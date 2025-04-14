using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Interfaces;
using Restaurant.Application.DTOs.Auth;
using Restaurant.API.Models;

namespace Restaurant.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        /// <summary>
        /// Registers a new user with the provided registration details.
        /// </summary>
        /// <param name="request">The registration details including email, password, and personal info.</param>
        /// <returns>A success message containing the registered email address.</returns>
        /// <response code="200">User was registered successfully.</response>
        /// <response code="400">Validation failed or user with the same email already exists.</response>
        [HttpPost("signup")]
        [ProducesResponseType(typeof(RegisterDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [Produces("application/json")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            var response = await authService.RegisterUserAsync(request);
            return Ok(new { Message = response });
        }
     }
 }
