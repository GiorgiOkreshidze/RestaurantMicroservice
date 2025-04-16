using Microsoft.AspNetCore.Mvc;
using Restaurant.API.Models;
using Restaurant.Application.DTOs.Locations;
using Restaurant.Application.Interfaces;
using Restaurant.Application.DTOs.Dishes;
using Microsoft.AspNetCore.Authorization;
using Restaurant.Application.DTOs.Feedbacks;
using Restaurant.Domain.Entities.Enums;
using Restaurant.Domain.DTOs;

namespace Restaurant.API.Controllers
{
    [ApiController]
    [Route("api/locations")]
    public class LocationsController(
        ILocationService locationService,
        IDishService dishService, 
        IFeedbackService feedbackService) : ControllerBase
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

        /// <summary>
        /// Gets, filters, sorts feedbacks for a specific location.
        /// </summary>
        /// <param name="id">The unique identifier of the location</param>
        /// <param name="queryDto">Query parameters for filtering, sorting and pagination</param>
        /// <returns>Returns a list of feedbacks for a specific location</returns>
        /// <response code="200">Returns the list of feedbacks for a specific location</response>
        [HttpGet("{id}/feedbacks")]
        [ProducesResponseType(typeof(FeedbacksWithMetaData), StatusCodes.Status200OK)]
        [Produces("application/json")]
        public async Task<ActionResult<FeedbacksWithMetaData>> GetFeedbacks(
            string id,
            [FromQuery] FeedbackQueryDto queryDto)
        { 
            var queryParams = new FeedbackQueryParameters
            {
                Page = queryDto.Page,
                PageSize = queryDto.PageSize,
                NextPageToken = queryDto.NextPageToken,
                Sort = queryDto.Sort,
                Type = queryDto.Type
            };

            // Process the sort parameter if provided
            if (!string.IsNullOrEmpty(queryDto.Sort))
            {
                var sortParams = queryDto.Sort.Split(',');
                if (sortParams.Length > 0 && !string.IsNullOrEmpty(sortParams[0]))
                {
                    queryParams.SortProperty = sortParams[0].ToLower();
                }

                if (sortParams.Length > 1 && !string.IsNullOrEmpty(sortParams[1]))
                {
                    queryParams.SortDirection = sortParams[1].ToLower();
                }
            }

            var result = await feedbackService.GetFeedbacksByLocationIdAsync(id, queryParams);
            return Ok(result);
        }
    }
}