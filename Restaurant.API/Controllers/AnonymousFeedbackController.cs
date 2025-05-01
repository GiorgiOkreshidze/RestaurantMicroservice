using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs.Feedbacks;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities.Enums;

namespace Restaurant.API.Controllers;

[ApiController]
[Route("api/anonymous-feedback")]
public class AnonymousFeedbackController(IAnonymousFeedbackService anonymousFeedbackService) : ControllerBase
{
    /// <summary>
    /// Validates a feedback token and returns reservation details if valid.
    /// </summary>
    /// <param name="token">The anonymous feedback token from QR code.</param>
    /// <returns>Basic reservation details needed for feedback context.</returns>
    [HttpGet("validate-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateToken(string token)
    {
        var reservationId = await anonymousFeedbackService.ValidateTokenAndGetReservationId(token);
        return Ok(reservationId);
    }
    
    /// <summary>
    /// Submits anonymous feedback for a completed reservation.
    /// </summary>
    /// <param name="request">The feedback details.</param>
    /// <returns>Confirmation of feedback submission.</returns>
    [HttpPost("submit-feedback")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitFeedback([FromBody] CreateFeedbackRequest request)
    {
        await anonymousFeedbackService.SubmitAnonymousFeedback(request);
        return Ok(new {Message = "Anonymous feedback submitted successfully."});
    }
}