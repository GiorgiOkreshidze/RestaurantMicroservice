using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Interfaces;
using Restaurant.Application.DTOs.Auth;
using Restaurant.API.Models;
using Microsoft.AspNetCore.Authorization;
using Restaurant.Application.Exceptions;

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

        /// <summary>
        /// Authenticates a user with their email and password.
        /// </summary>
        /// <param name="request">The login credentials containing email and password.</param>
        /// <returns>A TokenResponseDto containing access token, refresh token, and expiration time.</returns>
        /// <exception cref="BadRequestException">Thrown when validation fails for the login request.</exception>
        /// <exception cref="UnauthorizedException">Thrown when credentials are invalid or user not found.</exception>
        [HttpPost("signin")]
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            var response = await authService.LoginAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Refreshes an authentication session using a valid refresh token.
        /// </summary>
        /// <param name="request">The refresh token string.</param>
        /// <returns>A TokenResponseDto containing new access token, refresh token, and expiration time.</returns>
        /// <exception cref="BadRequestException">Thrown when refresh token is missing.</exception>
        /// <exception cref="UnauthorizedException">Thrown when refresh token is invalid, expired, revoked, or the user no longer exists.</exception>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
        [Produces("application/json")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var response = await authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(response);
        }
    }
 }
