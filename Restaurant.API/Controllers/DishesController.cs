using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Interfaces;

namespace Restaurant.API.Controllers;

[ApiController]
[Route("api/dishes")]
public class DishesController(IDishService dishService) : ControllerBase
{
    
    /// <summary>
    /// Gets popular dishes.
    /// </summary>
    /// <returns>Retrieves dishes where 'isPopular' is set to true/returns>
    /// <response code="200">Returns a list of dishes where 'isPopular' is set to true</response>
    [HttpGet("popular")]
    public async Task<IActionResult> GetPopularDishes()
    {
        var dishes = await dishService.GetPopularDishesAsync();
        return Ok(dishes);
    }
}