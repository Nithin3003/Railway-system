using Microsoft.AspNetCore.Mvc;
using RailwayReservationSystem.Interfaces;
using RailwayReservationSystem.Models.DTOs;

namespace RailwayReservationSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;
        public SearchController(ISearchService searchService) => _searchService = searchService;

        [HttpGet("view-trains")]
        public async Task<IActionResult> GetAllTrains()
        {
            var response = await _searchService.GetAllTrainsAsync();
            return Ok(response);
        }

        [HttpPost("check-fare-plan")]
        public async Task<IActionResult> CheckFarePlan([FromBody] TravelPlanFareRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TrainId)
                || string.IsNullOrWhiteSpace(request.SourceCode)
                || string.IsNullOrWhiteSpace(request.DestinationCode))
            {
                return BadRequest(new { message = "TrainId, SourceCode and DestinationCode are required." });
            }

            var result = await _searchService.CheckFarePlanAsync(request);
            if (result == null)
                return NotFound(new { message = "Train not found." });

            return Ok(result);
        }
    }
}
