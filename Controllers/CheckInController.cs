using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RailwayReservationSystem.Interfaces;
using RailwayReservationSystem.Models.DTOs;

namespace RailwayReservationSystem.Controllers
{
    [Authorize(Roles = "Passenger")]
    [Route("api/[controller]")]
    [ApiController]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class CheckInController : ControllerBase
    {
        private readonly ICheckInService _checkInService;

        public CheckInController(ICheckInService checkInService)
        {
            _checkInService = checkInService;
        }

        [HttpPost("perform")]
        public async Task<IActionResult> PerformCheckIn([FromBody] CheckInRequestDto request)
        {
            try
            {
                var result = await _checkInService.PerformCheckInAsync(request.PNR);
                if (result == null)
                    return NotFound(new { message = "Booking not found. Please verify your PNR." });

                return Ok(new
                {
                    message = "Check-in successful!",
                    pnr = result.PNR,
                    passengerName = result.PassengerName,
                    seat = result.Seat,
                    checkInRef = result.CheckInRef,
                    trainId = result.TrainId
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

    }
}
