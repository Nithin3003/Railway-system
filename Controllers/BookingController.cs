using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RailwayReservationSystem.Interfaces;
using RailwayReservationSystem.Models.DTOs;
using System.Security.Claims;

namespace RailwayReservationSystem.Controllers
{
    [Authorize(Roles = "Passenger")]
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        public BookingController(IBookingService bookingService) => _bookingService = bookingService;

        [HttpPost("reserve-ticket")]
        public async Task<IActionResult> Reserve([FromBody] BookingRequestDto model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated. Please login first." });
            }

            var reservation = await _bookingService.ReserveTicketAsync(model, userId);
            return Ok(new
            {
                reservation.Message,
                reservation.OrderReference,
                reservation.PaymentUrl,
                TrainId = reservation.TrainId,
                SourceCode = reservation.SourceCode,
                DestinationCode = reservation.DestinationCode,
                PassengerCount = reservation.PassengerCount,
                FarePerTicket = reservation.FarePerTicket,
                TotalFare = reservation.TotalFare,
                UserEmail = reservation.UserEmail,
                PaymentStatus = reservation.PaymentStatus,
                ConfirmationMessage = $"Checkout session created for {reservation.UserEmail}."
            });
        }

        [HttpPost("cancel-ticket")]
        public async Task<IActionResult> Cancel([FromBody] CancelRequestDto model)
        {
            if (string.IsNullOrWhiteSpace(model.PNR))
            {
                return BadRequest(new { message = "PNR is required." });
            }

            if (!model.PNR.StartsWith("PNR"))
            {
                return BadRequest(new { message = "Invalid PNR format. PNR should start with 'PNR'." });
            }

            var success = await _bookingService.CancelTicketAsync(model.PNR);

            if (!success)
            {
                return BadRequest(new { message = "Invalid PNR or already cancelled.", PNR = model.PNR });
            }

            return Ok(new { message = "Cancellation successful. Confirmation sent.", PNR = model.PNR });
        }

        [HttpGet("pnr-details/{pnr}")]
        public async Task<IActionResult> GetPnrDetails(string pnr)
        {
            var status = await _bookingService.GetBookingStatusAsync(pnr);
            if (status == null)
            {
                return NotFound(new { message = "PNR not found.", PNR = pnr });
            }

            return Ok(status);
        }
    }
}
