using Microsoft.AspNetCore.Mvc;
using Restaurant.API.Models;
using Restaurant.Application.DTOs.Locations;
using Restaurant.Application.Interfaces;
using Restaurant.Application.DTOs.Dishes;

namespace Restaurant.API.Controllers
{
    [ApiController]
    [Route("api/locations")]
    public class LocationsController(ILocationService locationService, IDishService dishService) : ControllerBase
    {
        /// <summary>
        /// Gets all locations.
        /// </summary>
        /// <returns> Location List or empty array</returns>
        /// <response code="200">Returns the list of locations</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<LocationDto>), StatusCodes.Status200OK)]
        [Produces("application/json")]
        public async Task<IActionResult> GetAllLocations()
        {
            var locations = await locationService.GetAllLocationsAsync();
            return Ok(locations);
        }

        /// <summary>
        /// Gets a specific location by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the location.</param>
        /// <returns>The location details if found, or 404 Not Found.</returns>
        /// <response code="200">Returns the location details</response>
        /// <response code="404">If the location is not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LocationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        public async Task<IActionResult> GetLocationById(string id)
        {
            var location = await locationService.GetLocationByIdAsync(id);
            return Ok(location);
        }

        /// <summary>
        /// Gets all locations for a dropdown list.
        /// </summary>
        /// <returns>A collection of simplified location models for dropdowns</returns>
        /// <response code="200">Returns the list of location select options</response>
        [HttpGet("select-options")]
        [ProducesResponseType(typeof(IEnumerable<LocationSelectOptionDto>), 200)]
        [Produces("application/json")]
        public async Task<IActionResult> GetLocationSelectOptions()
        {
            var options = await locationService.GetAllLocationsForDropDownAsync();
            return Ok(options);
        }

        /// <summary>
        /// Gets all specialty dishes for a specific location.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Retrieves a collection of the specialty dishes for a specific restaurant location</returns>
        /// <response code="200">Returns the list of specialty dishes for a selected location</response>
        [HttpGet("{id}/speciality-dishes")]
        public async Task<ActionResult<IEnumerable<DishDto>>> GetSpecialtyDishes(string id)
        {
            var dishes = await dishService.GetSpecialtyDishesByLocationAsync(id);
            return Ok(dishes);
        }
    }
}