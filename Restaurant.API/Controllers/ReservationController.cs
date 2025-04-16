using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.API.Models;
using Restaurant.Application.DTOs.Reservations;
using Restaurant.Application.DTOs.Tables;
using Restaurant.Application.Interfaces;

namespace Restaurant.API.Controllers;

[ApiController]
[Route("api/reservations")]
public class ReservationController(IReservationService reservationService) : ControllerBase
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
    /// <returns>The cancelled reservation</returns>
    /// <response code="200">Reservation cancelled successfully</response>
    /// <response code="404">If the reservation is not found</response>
    /// <response code="409">If the reservation cannot be cancelled due to its current status</response>
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
}