using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}