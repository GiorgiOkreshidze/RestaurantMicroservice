using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs.Users;
using Restaurant.Application.Interfaces;

namespace Restaurant.API.Controllers;

[ApiController]
[Route("api/users")]
public class UserController(IUserService userService) : ControllerBase
{
    /// <summary>
    /// Retrieves the profile of the currently authenticated user.
    /// </summary>
    /// <returns>The user's profile information.</returns>
    /// <response code="200">Returns the user's profile information successfully.</response>
    /// <response code="401">If the user is not authenticated or the token is invalid.</response>
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetUsersProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized("User ID not found in token.");

        var userProfile = await userService.GetUserByIdAsync(userId);

        return Ok(userProfile);
    }
    
    /// <summary>
    /// Retrieves a list of all users.
    /// </summary>
    /// <returns>A list of user profiles.</returns>
    /// <response code="200">Returns the list of users successfully.</response>
    /// <response code="401">If the user is not authenticated or the token is invalid.</response>
    [HttpGet]
    [Authorize(Roles = "Waiter")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Updates the password for the currently authenticated user.
    /// </summary>
    /// <param name="request">The request containing the old and new passwords.</param>
    /// <returns>A success message indicating the password was updated.</returns>
    /// <response code="200">Password was updated successfully.</response>
    /// <response code="400">Validation failed or current password is incorrect.</response>
    /// <response code="401">If the user is not authenticated or the token is invalid.</response>
    /// <response code="404">If the user is not found.</response>
    [HttpPut("profile/password")]
    [Authorize]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized("User ID not found in token.");

        await userService.UpdatePasswordAsync(userId, request);

        return Ok(new { Message = "Password updated successfully." });
    }
}
