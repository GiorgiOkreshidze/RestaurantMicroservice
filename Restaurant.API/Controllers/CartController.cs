using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs.PerOrders;
using Restaurant.Application.Interfaces;
using System.Security.Claims;

namespace Restaurant.API.Controllers;

[ApiController]
[Route("api/cart")]
public class CartController(IPreOrderService preOrderService) : ControllerBase
{
    /// <summary>
    /// Returns all pre-orders for the current user.
    /// </summary>
    /// <returns> Cart details.</returns>
    /// <response code="200">Returns cart details of the current user.</response>
    /// <response code="401">User is not authenticated or token does not contain required claims.</response>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCart()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("User ID not found in token.");

        var result = await preOrderService.GetUserCart(userId);

        // Logic to retrieve the cart
        return Ok(result);
    }
}
