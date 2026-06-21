using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RailwayReservationSystem.Interfaces;
using RailwayReservationSystem.Models.DTOs;

namespace RailwayReservationSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpPut("updatetrain/{id}")]
        public async Task<IActionResult> UpdateTrain(string id, [FromBody] UpdateTrainDto updatedTrain)
        {
            if (!string.IsNullOrWhiteSpace(updatedTrain.Id) && !string.Equals(updatedTrain.Id, id, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Route id and body id must match." });
            }

            var sourceCode = updatedTrain.SourceCode?.Trim().ToUpper();
            var destinationCode = updatedTrain.DestinationCode?.Trim().ToUpper();

            try
            {
                var train = await _adminService.UpdateTrainAsync(id, updatedTrain);
                if (train == null)
                {
                    return NotFound(new { message = "Train not found" });
                }

                return Ok(new
                {
                    message = "Train details updated by Administrator.",
                    trainId = train.Id,
                    source = train.Source,
                    destination = train.Destination,
                    sourceCode = sourceCode,
                    destinationCode = destinationCode,
                    intermediateStationCodes = updatedTrain.IntermediateStationCodes
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("delete-train/{id}")]
        public async Task<IActionResult> DeleteTrain(string id)
        {
            var deleted = await _adminService.DeleteTrainAsync(id);
            if (!deleted) return NotFound(new { message = "Train not found" });

            return Ok(new { message = "Train deleted successfully." });
        }

        [HttpPost("newtrain")]
        public async Task<IActionResult> CreateTrainWithRoute([FromBody] TrainWithRouteDto dto)
        {
            try
            {
                var result = await _adminService.CreateTrainWithRouteAsync(dto);

                return Ok(new
                {
                    message = "Train and route created successfully.",
                    trainId = result.TrainId,
                    stations = result.Stations
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("dashboard/{trainId}")]
        public async Task<IActionResult> GetTrainDashboard(string trainId)
        {
            var dashboard = await _adminService.GetTrainDashboardAsync(trainId);
            if (dashboard == null)
            {
                return NotFound(new { message = "Train not found." });
            }

            return Ok(dashboard);
        }

        [HttpGet("train-payments/{trainId}")]
        public async Task<IActionResult> GetTrainPayments(string trainId)
        {
            var payments = await _adminService.GetTrainPaymentHistoryAsync(trainId);
            return Ok(payments);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _adminService.GetAllUsersAsync();
            return Ok(users);
        }
    }
}
