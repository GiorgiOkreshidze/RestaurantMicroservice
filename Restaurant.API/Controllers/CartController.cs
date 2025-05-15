using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs.PerOrders;
using Restaurant.Application.DTOs.PerOrders.Request;
using Restaurant.Application.Interfaces;
using System.Security.Claims;
using Restaurant.Domain.Entities.Enums;

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

    /// <summary>
    /// Creates, updates, or cancels a pre-order in the user's cart.
    /// </summary>
    /// <param name="request">Pre-order details</param>
    /// <returns>Updated cart</returns>
    /// <response code="200">Returns the updated cart after the operation</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpPut]
    [Authorize]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpsertPreOrder([FromBody] UpsertPreOrderRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("User ID not found in token.");

        var result = await preOrderService.UpsertPreOrder(userId, request);

        return Ok(result);
    }
    
    /// <summary>
    /// Gets dishes from a specific pre-order for authorized waiters.
    /// </summary>
    /// <param name="reservationId">The ID of the reservation to retrieve preorder</param>
    /// <returns>Pre-order with existing dishes</returns>
    /// <response code="200">Returns the Preorder with Dishes</response>
    /// <response code="401">If user is not authenticated or not a waiter</response>
    [HttpGet("preorder/{reservationId}/dishes")]
    [Authorize]
    [ProducesResponseType(typeof(PreOrderDishConfirmDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPreOrderDishes(string reservationId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
            return Unauthorized("User ID or role not found in token.");
        
        if (role != Role.Waiter.ToString())
            return Unauthorized("You don't have permission to access this resource.");
        
        var preOrderDishes = await preOrderService.GetPreOrderDishes(reservationId);
        
        return Ok(preOrderDishes);
    }
    
    /// <summary>
    /// Updates the status of dishes in a pre-order for authorized waiters.
    /// </summary>
    /// <param name="request">Request containing pre-order ID, dish ID and new status</param>
    /// <returns>Success message</returns>
    /// <response code="200">Returns success message after updating dish status</response>
    /// <response code="401">If user is not authenticated or not a waiter</response>
    [HttpPut("preorder/dishes/status")]
    [Authorize]
    [ProducesResponseType(typeof(PreOrderDishesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePreOrderDishesStatus([FromBody]UpdatePreOrderDishesStatusRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
            return Unauthorized(new {Message = "User ID or role not found in token."});
        
        if (role != Role.Waiter.ToString())
            return Unauthorized(new {Message = "You don't have permission to access this resource."});
        
        await preOrderService.UpdatePreOrderDishesStatus(request);
        
        return Ok(new {Message = "Dish status updated successfully."});
    }
}
