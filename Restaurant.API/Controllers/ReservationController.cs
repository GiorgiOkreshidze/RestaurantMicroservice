using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs.Reservations;
using Restaurant.Application.DTOs.Tables;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities.Enums;

namespace Restaurant.API.Controllers;

[ApiController]
[Route("api/reservations")]
public class ReservationController(IReservationService reservationService, IOrderService orderService) : ControllerBase
{
    /// <summary>
    /// Gets available tables for a specific date, time, and number of guests.
    /// </summary>
    /// <param name="query">Query parameters for available tables</param>
    /// <returns>List of available tables with time slots</returns>
    /// <response code="200">Available tables retrieved successfully</response>
    /// <response code="400">If parameters are invalid</response>
    [HttpGet("tables")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<AvailableTableDto>>> GetAvailableTables(
        [FromQuery] FilterParameters query)
    {
        var result = await reservationService.GetAvailableTablesAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Creates or updates a client reservation.
    /// </summary>
    /// <param name="request">The reservation details.</param>
    /// <returns>The created or updated reservation information.</returns>
    /// <response code="200">Returns the successfully created or updated reservation.</response>
    /// <response code="400">If the reservation request is invalid (outside working hours, table too small, or conflicting reservation).</response>
    /// <response code="404">If required resources (location, table, waiter) are not found.</response>
    [HttpPost("client")]
    [Authorize]
    public async Task<IActionResult> CreateClientReservations([FromBody] ClientReservationRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized("User ID not found in token.");
        var locations = await reservationService.UpsertReservationAsync(request, userId);
        return Ok(locations);
    }
    
    /// <summary>
    /// Creates or updates a reservation on behalf of a waiter.
    /// </summary>
    /// <param name="request">The reservation details provided by the waiter.</param>
    /// <returns>The created or updated reservation information.</returns>
    /// <response code="200">Returns the successfully created or updated reservation.</response>
    /// <response code="400">If the reservation request is invalid (e.g., conflicting reservation, invalid table).</response>
    /// <response code="404">If required resources (e.g., table, waiter) are not found.</response>
    [HttpPost("waiter")]
    [Authorize]
    public async Task<IActionResult> CreateWaiterReservations([FromBody] WaiterReservationRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized("User ID not found in token.");
        var locations = await reservationService.UpsertReservationAsync(request, userId);
        return Ok(locations);
    }

    /// <summary>
    /// Gets reservations based on user identity and query parameters
    /// </summary>
    /// <param name="queryParams">Optional query parameters for filtering reservations</param>
    /// <returns>List of reservations</returns>
    /// <response code="200">Reservations retrieved successfully</response>
    /// <response code="401">Unauthorized access</response>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<ReservationResponseDto>>> GetReservations([FromQuery] ReservationsQueryParameters queryParams)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
            return Unauthorized("Invalid Request");

        var reservations = await reservationService.GetReservationsAsync(queryParams, userId, email, role!);
        return Ok(reservations);
    }

    /// <summary>
    /// Cancels an existing reservation.
    /// </summary>
    /// <param name="id">ID of the reservation to cancel</param>
    /// <returns>The canceled reservation</returns>
    /// <response code="200">Reservation canceled successfully</response>
    /// <response code="404">If the reservation is not found</response>
    /// <response code="409">If the reservation cannot be canceled due to its current status</response>
    /// <response code="401">If the user is not authorized to cancel this reservation</response>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ReservationResponseDto>> CancelReservation(string id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
            return Unauthorized("User ID or role not found in token.");

        var result = await reservationService.CancelReservationAsync(id, userId, role);
        return Ok(result);
    }
    
    /// <summary>
    /// Marks a reservation as completed.
    /// </summary>
    /// <param name="id">The ID of the reservation to complete.</param>
    /// <returns>A success message indicating the reservation was completed.</returns>
    /// <response code="200">Reservation completed successfully.</response>
    /// <response code="401">Unauthorized access or insufficient permissions.</response>
    /// <response code="404">Reservation not found.</response>
    [HttpPost("{id}/complete")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> CompleteReservation(string id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
            return Unauthorized("User ID or role not found in token.");
        
        if (role != Role.Waiter.ToString())
            return Unauthorized("You don't have permission to access this resource.");

        var qrCodeResponse = await reservationService.CompleteReservationAsync(id);
        
        if (!string.IsNullOrEmpty(qrCodeResponse.QrCodeImageBase64))
        {
            return Ok(new
            {
                message = "Reservation completed successfully",
                qrCodeData = qrCodeResponse
            });
        } 
        
        // For non-VISITOR clients
        return Ok(new { message = "Reservation completed successfully" });
    }
    
    /// <summary>
    /// Adds a dish to an existing order associated with a reservation.
    /// </summary>
    /// <param name="reservationId">The ID of the reservation to which the dish will be added.</param>
    /// <param name="dishId">The ID of the dish to add to the order.</param>
    /// <returns>A success message indicating the dish was added to the reservation.</returns>
    /// <response code="200">Dish was added to the reservation successfully.</response>
    /// <response code="401">Unauthorized access or insufficient permissions.</response>
    [HttpPost("{reservationId}/order/{dishId}")]
    [Authorize]
    public async Task<IActionResult> AddDishToOrder(string reservationId, string dishId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
            return Unauthorized("User ID or role not found in token.");

        if (role != Role.Waiter.ToString())
            return Unauthorized("You don't have permission to access this resource.");

        await orderService.AddDishToOrderAsync(reservationId, dishId);
        return Ok(new { message = "Dish was added to reservation successfully" });
    }
    
    /// <summary>
    /// Removes a dish from an existing order associated with a reservation.
    /// </summary>
    /// <param name="reservationId">The ID of the reservation from which the dish will be removed.</param>
    /// <param name="dishId">The ID of the dish to remove from the order.</param>
    /// <returns>A success message indicating the dish was removed from the reservation.</returns>
    /// <response code="200">Dish was removed successfully from the reservation.</response>
    /// <response code="401">Unauthorized access or insufficient permissions.</response>
    [HttpDelete("{reservationId}/order/{dishId}")]
    [Authorize]
    public async Task<IActionResult> DeleteDishFromOrder(string reservationId, string dishId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
            return Unauthorized("User ID or role not found in token.");

        if (role != Role.Waiter.ToString())
            return Unauthorized("You don't have permission to access this resource.");

        await orderService.DeleteDishFromOrderAsync(reservationId, dishId);
        return Ok(new { message = "Dish was removed successfully from order" });
    }
    
    // <summary>
    /// Retrieves the list of dishes in an order associated with a specific reservation.
    /// </summary>
    /// <param name="reservationId">The ID of the reservation whose order dishes are to be retrieved.</param>
    /// <returns>A list of dishes in the order.</returns>
    /// <response code="200">Dishes retrieved successfully.</response>
    /// <response code="401">Unauthorized access or insufficient permissions.</response>
    [HttpGet("{reservationId}/order/available-dishes")]
    [Authorize]
    public async Task<IActionResult> GetDishFromOrder(string reservationId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
            return Unauthorized("User ID or role not found in token.");

        if (role != Role.Waiter.ToString())
            return Unauthorized("You don't have permission to access this resource.");
        
        var dishes = await orderService.GetDishesInOrderAsync(reservationId);
        return Ok(dishes);
    }
}