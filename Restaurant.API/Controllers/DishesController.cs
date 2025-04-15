using Microsoft.AspNetCore.Mvc;
using Restaurant.API.Models;
using Restaurant.Application.DTOs.Dishes;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.DTOs;

namespace Restaurant.API.Controllers;

[ApiController]
[Route("api/dishes")]
public class DishesController(IDishService dishService) : ControllerBase
{
    
    /// <summary>
    /// Gets popular dishes.
    /// </summary>
    /// <returns>Retrieves dishes where 'isPopular' is set to true</returns>
    /// <response code="200">Returns a list of dishes where 'isPopular' is set to true</response>
    [HttpGet("popular")]
    public async Task<IActionResult> GetPopularDishes()
    {
        var dishes = await dishService.GetPopularDishesAsync();
        return Ok(dishes);
    }
    
    /// <summary>
    /// Gets a specific dish by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the dish.</param>
    /// <returns>The dish details if found, or 404 Not Found.</returns>
    /// <response code="200">Returns the dish details</response>
    /// <response code="404">If the dish is not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DishDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetDishById(string id)
    {
        var dish = await dishService.GetDishByIdAsync(id);
        return Ok(dish);
    }
    
    /// <summary>
    /// Retrieves all dishes based on the provided filter criteria.
    /// </summary>
    /// <param name="filter">The filter criteria for retrieving dishes.</param>
    /// <returns>A list of dishes matching the filter criteria.</returns>
    /// <response code="200">Returns a list of dishes matching the filter criteria.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DishDto>), StatusCodes.Status200OK)]
    [Produces("application/json")]
    public async Task<IActionResult> GetAllDishes([FromQuery] DishFilterDto filter)
    {
        var dishes = await dishService.GetAllDishesAsync(filter);
        return Ok(dishes);
    }
}