using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Restaurant.API.Models;
using Restaurant.Application.DTOs.Feedbacks;
using Restaurant.Application.Interfaces;

namespace Restaurant.API.Controllers;

[ApiController]
[Route("api/feedbacks")]
public class FeedbacksController(IFeedbackService feedbackService) : ControllerBase
{
    /// <summary>
    /// Creates a new feedback for a reservation.
    /// </summary>
    /// <param name="request">The feedback details to be submitted.</param>
    /// <returns>A success message upon successful creation.</returns>
    /// <response code="200">Feedback added successfully</response>
    /// <response code="400">If the request is invalid</response>
    [HttpPost]
    public async Task<IActionResult> CreateFeedback([FromBody] CreateFeedbackRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized("User ID not found in token.");
        
        await feedbackService.AddFeedbackAsync(request, userId);
        return Ok(new  { Message = "Feedback added successfully" });
    }
}