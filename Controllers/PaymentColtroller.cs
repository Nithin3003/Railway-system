using Microsoft.AspNetCore.Mvc;
using RailwayReservationSystem.Interfaces;
using System.Net;
using System.Linq;
using System.Security.Claims;
using RailwayReservationSystem.Models.DTOs;

namespace RailwayReservationSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public PaymentController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpGet("success")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> PaymentSuccess([FromQuery] string session_id, [FromQuery] string? order_reference = null)
        {
            if (string.IsNullOrEmpty(session_id))
            {
                return Content(BuildHtmlPage("Payment Missing", "Missing session ID."), "text/html");
            }

            var result = await _bookingService.FinalizePaymentAsync(session_id, order_reference);
            if (result == null)
            {
                return Content(BuildHtmlPage("Payment Failed", "Payment verification failed or the booking could not be finalized."), "text/html");
            }

            // Build a minimal "Payment History / Booking Story" page. Do not include check-in details here.
            var pnrs = result.Pnrs.Count == 0 ? "<li>No PNR generated</li>" : string.Join(string.Empty, result.Pnrs.Select(pnr => $"<li>{WebUtility.HtmlEncode(pnr)}</li>"));
            var html = $@"
<html>
    <head>
        <meta charset='utf-8' />
        <title>Payment History</title>
        <style>
            body {{ font-family: Arial, sans-serif; margin: 40px; background: #f7f9fc; color: #1f2937; }}
            .card {{ max-width: 720px; margin: 0 auto; background: #fff; border-radius: 12px; padding: 24px; box-shadow: 0 8px 24px rgba(0,0,0,.06); }}
            h1 {{ color: #0b4a6f; margin-top: 0; }}
            .meta {{ line-height: 1.8; margin-bottom: 12px; }}
            ul {{ padding-left: 20px; }}
            .status {{ display: inline-block; padding: 6px 12px; border-radius: 999px; background: #dcfce7; color: #166534; font-size: 13px; font-weight: 700; }}
        </style>
    </head>
    <body>
        <div class='card'>
            <span class='status'>Paid</span>
            <h1>Payment History / Booking Story</h1>
            <p>Your payment has been processed. Below is the payment history entry for this order. Check-in is optional and can be completed later from your account.</p>
            <div class='meta'>
                <div><strong>Order Reference:</strong> {WebUtility.HtmlEncode(result.OrderReference)}</div>
                <div><strong>Total Fare:</strong> {result.TotalFare}</div>
                <div><strong>Email:</strong> {WebUtility.HtmlEncode(result.UserEmail)}</div>
                <div><strong>Payment Status:</strong> Paid</div>
            </div>
            <h3>Booking Story (PNR(s))</h3>
            <ul>{pnrs}</ul>
        </div>
    </body>
</html>";

            return Content(html, "text/html");
        }

        [HttpGet("cancel")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> PaymentCancel([FromQuery] string? order_reference = null)
        {
            if (!string.IsNullOrWhiteSpace(order_reference))
            {
                await _bookingService.MarkPaymentCancelledAsync(order_reference);
            }

            // Show a minimal cancellation entry in payment history style
            var message = "Your payment was cancelled and no booking was completed.";
            if (!string.IsNullOrWhiteSpace(order_reference))
            {
                message += $" Order Reference: {WebUtility.HtmlEncode(order_reference)}";
            }

            return Content(BuildHtmlPage("Payment Cancelled", message), "text/html");
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetPaymentHistory()
        {
            // For passengers, return their own payment history; admins can extend this later.
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            // resolve repository via booking service (booking service exposes payment repo access)
            if (!(_bookingService is RailwayReservationSystem.Services.BookingService bookingServiceImpl))
            {
                return StatusCode(500, "Payment history not available");
            }

            var payments = await bookingServiceImpl.GetPaymentsForUserAsync(userId);
            var dto = payments.Select(payment => {
                var pnrs = payment.Bookings?.Select(b => b.PNR).Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? new List<string>();
                var emailSubject = $"Booking Confirmed - {payment.OrderReference}";
                var emailBody = $@"Hello,\n\nYour payment was successful and your booking has been confirmed.\n\nOrder Reference: {payment.OrderReference}\nTrain ID: {payment.TrainId}\nPNRs: {string.Join(", ", pnrs)}\nTotal Fare: {payment.TotalAmount:C}\nConfirmed Time (UTC): {payment.PaidAt:yyyy-MM-dd HH:mm:ss}\n\nHave a safe journey.";

                return new PaymentStatusDto
                {
                    OrderReference = payment.OrderReference,
                    TrainId = payment.TrainId,
                    UserEmail = payment.UserEmail,
                    Status = payment.Status,
                    TotalAmount = payment.TotalAmount,
                    PassengerCount = payment.PassengerCount,
                    CreatedAt = payment.CreatedAt,
                    PaidAt = payment.PaidAt,
                    FailureReason = payment.FailureReason,
                    Pnrs = pnrs,
                    EmailSubject = emailSubject,
                    EmailBody = emailBody
                };
            }).ToList();

            return Ok(dto);
        }

                private static string BuildHtmlPage(string title, string message)
                {
                        return $@"
<html>
    <head>
        <meta charset='utf-8' />
        <title>{WebUtility.HtmlEncode(title)}</title>
        <style>
            body {{ font-family: Arial, sans-serif; margin: 40px; background: #f7f9fc; color: #1f2937; }}
            .card {{ max-width: 640px; margin: 0 auto; background: #fff; border-radius: 14px; padding: 28px; box-shadow: 0 10px 30px rgba(0,0,0,.08); }}
            h1 {{ margin-top: 0; }}
        </style>
    </head>
    <body>
        <div class='card'>
            <h1>{WebUtility.HtmlEncode(title)}</h1>
            <p>{WebUtility.HtmlEncode(message)}</p>
        </div>
    </body>
</html>";
                }
    }
}
