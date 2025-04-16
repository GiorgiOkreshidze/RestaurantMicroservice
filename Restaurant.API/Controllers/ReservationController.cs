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
}