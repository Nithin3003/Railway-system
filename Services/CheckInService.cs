using RailwayReservationSystem.Interfaces;
using RailwayReservationSystem.Models.DTOs;
using RailwayReservationSystem.Models.Entities;

namespace RailwayReservationSystem.Services
{
    public class CheckInService : ICheckInService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ITrainRepository _trainRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<CheckInService> _logger;

        public CheckInService(
            IBookingRepository bookingRepository,
            ITrainRepository trainRepository,
            IAccountRepository accountRepository,
            IEmailService emailService,
            ILogger<CheckInService> logger)
        {
            _bookingRepository = bookingRepository;
            _trainRepository = trainRepository;
            _accountRepository = accountRepository;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<CheckInResultDto?> PerformCheckInAsync(string pnr)
        {
            var booking = await _bookingRepository.GetByPnrAsync(pnr);
            if (booking == null)
            {
                return null;
            }

            if (booking.Status == "Cancelled")
            {
                throw new ArgumentException("Cannot check-in. This booking has been cancelled.");
            }

            if (booking.IsCheckedIn)
            {
                throw new InvalidOperationException($"Passenger already checked in. Seat: {booking.SeatNumber}");
            }

            var train = await _trainRepository.GetByIdAsync(booking.TrainId);
            if (train == null)
            {
                throw new ArgumentException("Train not found for this booking.");
            }

            var nextSeat = await GetNextAvailableSeatAsync(booking.TrainId, train.TotalSeats);
            if (nextSeat == null)
            {
                throw new InvalidOperationException("No seat available for check-in. Please contact support.");
            }

            var checkIn = new CheckIn
            {
                BookingId = booking.Id,
                SeatNumber = nextSeat,
                CheckInReference = "CHK-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper()
            };

            booking.IsCheckedIn = true;
            booking.SeatNumber = nextSeat;

            await _bookingRepository.AddCheckInAsync(checkIn);
            await _bookingRepository.SaveChangesAsync();

            var userEmail = await _accountRepository.GetUserEmailByIdOrUserNameAsync(booking.UserId);
            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                var subject = $"Check-In Successful - {booking.PNR}";
                var body = $@"Hello {booking.PassengerName},

Your check-in has been completed successfully.

PNR: {booking.PNR}
Train ID: {booking.TrainId}
Seat Number: {checkIn.SeatNumber}
Check-In Reference: {checkIn.CheckInReference}
Status: Checked-In

Have a safe journey.";

                try
                {
                    await _emailService.SendAsync(userEmail, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Check-in email failed for PNR {Pnr}.", booking.PNR);
                }
            }

            return new CheckInResultDto
            {
                PNR = booking.PNR,
                PassengerName = booking.PassengerName,
                Seat = checkIn.SeatNumber,
                CheckInRef = checkIn.CheckInReference,
                TrainId = booking.TrainId
            };
        }

        public async Task<CheckInStatusDto?> GetStatusAsync(string pnr)
        {
            var booking = await _bookingRepository.GetByPnrAsync(pnr);
            if (booking == null)
            {
                return null;
            }

            var isCancelled = booking.Status == "Cancelled";

            await TrySendStatusMailAsync(booking, isCancelled);

            return new CheckInStatusDto
            {
                PNR = booking.PNR,
                PassengerName = booking.PassengerName,
                CheckedIn = isCancelled ? false : booking.IsCheckedIn,
                Seat = isCancelled ? null : booking.SeatNumber,
                Status = booking.Status,
                Message = isCancelled
                    ? "This ticket has been cancelled and is no longer valid for travel."
                    : "Booking status retrieved successfully."
            };
        }

        private async Task TrySendStatusMailAsync(Booking booking, bool isCancelled)
        {
            var userEmail = await _accountRepository.GetUserEmailByIdOrUserNameAsync(booking.UserId);
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return;
            }

            var subject = $"Booking Status Checked - {booking.PNR}";
            var seatText = isCancelled ? "Not Applicable" : (string.IsNullOrWhiteSpace(booking.SeatNumber) ? "To be assigned during check-in" : booking.SeatNumber);
            var body = $@"Hello {booking.PassengerName},

Your booking status was requested.

PNR: {booking.PNR}
Train ID: {booking.TrainId}
Status: {booking.Status}
Checked-In: {(isCancelled ? "No" : booking.IsCheckedIn ? "Yes" : "No")}
Seat: {seatText}
Checked Time (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}

This is an automated status notification.";

            try
            {
                await _emailService.SendAsync(userEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Status check email failed for PNR {Pnr}.", booking.PNR);
            }
        }

        private async Task<string?> GetNextAvailableSeatAsync(string trainId, int totalSeats)
        {
            if (totalSeats <= 0)
            {
                return null;
            }

            var assignedSeats = await _bookingRepository.GetAssignedSeatsAsync(trainId);
            var assignedSet = new HashSet<string>(assignedSeats, StringComparer.OrdinalIgnoreCase);

            for (var i = 1; i <= totalSeats; i++)
            {
                var seat = $"S-{i:D3}";
                if (!assignedSet.Contains(seat))
                {
                    return seat;
                }
            }

            return null;
        }
    }
}
