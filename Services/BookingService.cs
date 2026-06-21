using System.Text.Json;
using RailwayReservationSystem.Interfaces;
using RailwayReservationSystem.Models.DTOs;
using RailwayReservationSystem.Models.Entities;
using RailwayReservationSystem.Validators;
using Stripe;
using Stripe.Checkout;

namespace RailwayReservationSystem.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ITrainRepository _trainRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IEmailService _emailService;
        private readonly BookingValidator _validator = new();
        private readonly IConfiguration _configuration;
        private readonly ILogger<BookingService> _logger;

        public BookingService(
            IBookingRepository bookingRepository,
            ITrainRepository trainRepository,
            IPaymentRepository paymentRepository,
            IAccountRepository accountRepository,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<BookingService> logger)
        {
            _bookingRepository = bookingRepository;
            _trainRepository = trainRepository;
            _paymentRepository = paymentRepository;
            _accountRepository = accountRepository;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;

            var secretKey = _configuration["Stripe:SecretKey"];
            if (!string.IsNullOrWhiteSpace(secretKey))
            {
                StripeConfiguration.ApiKey = secretKey;
            }
        }

        public async Task<ReservationResultDto> ReserveTicketAsync(BookingRequestDto dto, string userId)
        {
            var train = await _trainRepository.GetByIdAsync(dto.TrainId)
                ?? throw new ArgumentException("Train not found. Please choose a valid train.");

            var routeStops = await _trainRepository.GetRouteAsync(dto.TrainId);
            _validator.ValidateRoute(routeStops, dto.TrainId);
            _validator.ValidateStationCodes(dto.SourceCode, dto.DestinationCode);

            var sourceCode = dto.SourceCode.Trim().ToUpperInvariant();
            var destinationCode = dto.DestinationCode.Trim().ToUpperInvariant();
            var startStation = routeStops.FirstOrDefault(s => s.StationCode.Equals(sourceCode, StringComparison.OrdinalIgnoreCase));
            var endStation = routeStops.FirstOrDefault(s => s.StationCode.Equals(destinationCode, StringComparison.OrdinalIgnoreCase));
            _validator.ValidateStationSelection(startStation, endStation, sourceCode, destinationCode, routeStops);

            var passengers = ResolvePassengers(dto);
            _validator.ValidateSeatAvailability(train, passengers.Count);

            var farePerTicket = endStation!.FareFromStart - startStation!.FareFromStart;
            var totalFare = farePerTicket * passengers.Count;
            var userEmail = await _accountRepository.GetUserEmailByIdOrUserNameAsync(userId) ?? string.Empty;
            var orderReference = $"STRP-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant();

            var payment = new Payment
            {
                OrderReference = orderReference,
                UserId = userId,
                UserEmail = userEmail,
                TrainId = dto.TrainId,
                SourceCode = sourceCode,
                DestinationCode = destinationCode,
                PassengerCount = passengers.Count,
                TotalAmount = totalFare,
                Currency = "usd",
                Status = "Pending",
                RequestPayloadJson = JsonSerializer.Serialize(dto),
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            var session = await CreateCheckoutSessionAsync(payment, passengers.Count, farePerTicket, totalFare);

            payment.RazorpayOrderId = session.Id;
            await _paymentRepository.SaveChangesAsync();

            return new ReservationResultDto
            {
                OrderReference = payment.OrderReference,
                PaymentUrl = session.Url ?? string.Empty,
                TrainId = dto.TrainId,
                SourceCode = sourceCode,
                DestinationCode = destinationCode,
                PassengerCount = passengers.Count,
                FarePerTicket = farePerTicket,
                TotalFare = totalFare,
                PaymentStatus = payment.Status,
                UserEmail = userEmail,
                Message = "Stripe checkout session created. Complete the payment to confirm the booking."
            };
        }

        public async Task<BookingConfirmationDto?> FinalizePaymentAsync(string checkoutSessionId, string? orderReference)
        {
            var session = await new SessionService().GetAsync(checkoutSessionId);
            if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var payment = await _paymentRepository.GetByCheckoutSessionIdAsync(checkoutSessionId)
                ?? (!string.IsNullOrWhiteSpace(orderReference) ? await _paymentRepository.GetByOrderReferenceAsync(orderReference) : null);

            if (payment == null)
            {
                return null;
            }

            if (string.Equals(payment.Status, "Paid", StringComparison.OrdinalIgnoreCase))
            {
                return new BookingConfirmationDto
                {
                    OrderReference = payment.OrderReference,
                    PaymentStatus = payment.Status,
                    TrainId = payment.TrainId,
                    TotalFare = payment.TotalAmount,
                    UserEmail = payment.UserEmail,
                    Message = "This payment was already processed."
                };
            }

            var dto = JsonSerializer.Deserialize<BookingRequestDto>(payment.RequestPayloadJson ?? string.Empty)
                ?? throw new InvalidOperationException("Stored booking request could not be read.");

            var passengers = ResolvePassengers(dto);
            var train = await _trainRepository.GetByIdAsync(dto.TrainId)
                ?? throw new ArgumentException("Train not found. Please choose a valid train.");

            var routeStops = await _trainRepository.GetRouteAsync(dto.TrainId);
            _validator.ValidateRoute(routeStops, dto.TrainId);
            _validator.ValidateStationCodes(dto.SourceCode, dto.DestinationCode);

            var sourceCode = dto.SourceCode.Trim().ToUpperInvariant();
            var destinationCode = dto.DestinationCode.Trim().ToUpperInvariant();
            var startStation = routeStops.FirstOrDefault(s => s.StationCode.Equals(sourceCode, StringComparison.OrdinalIgnoreCase));
            var endStation = routeStops.FirstOrDefault(s => s.StationCode.Equals(destinationCode, StringComparison.OrdinalIgnoreCase));
            _validator.ValidateStationSelection(startStation, endStation, sourceCode, destinationCode, routeStops);

            if (train.AvailableSeats < passengers.Count)
            {
                payment.Status = "Failed";
                payment.FailureReason = "Not enough seats were available when the payment completed.";
                await _paymentRepository.SaveChangesAsync();
                return null;
            }

            var farePerTicket = endStation!.FareFromStart - startStation!.FareFromStart;
            var pnrs = new List<string>();

            await _trainRepository.ExecuteInTransactionAsync(async () =>
            {
                train.AvailableSeats -= passengers.Count;

                var bookings = passengers.Select(passenger => new Booking
                {
                    PaymentId = payment.Id,
                    PNR = GeneratePnr(),
                    TrainId = dto.TrainId,
                    Source = sourceCode,
                    Destination = destinationCode,
                    Fare = farePerTicket,
                    UserId = payment.UserId,
                    PassengerName = passenger.PassengerName,
                    PassengerAge = passenger.Age,
                    Sex = passenger.Sex,
                    Address = passenger.Address,
                    BankName = string.Empty,
                    Class = string.IsNullOrWhiteSpace(dto.Class) ? "Economy" : dto.Class,
                    SeatNumber = string.Empty,
                    BookingDate = DateTime.UtcNow,
                    Status = "Confirmed",
                    IsCheckedIn = false
                }).ToList();

                await _bookingRepository.AddBookingsAsync(bookings);

                payment.Status = "Paid";
                payment.PaidAt = DateTime.UtcNow;
                payment.RazorpayPaymentId = checkoutSessionId;
                payment.FailureReason = null;

                pnrs.AddRange(bookings.Select(b => b.PNR));
                await _bookingRepository.SaveChangesAsync();
            });

            await SendConfirmationEmailAsync(payment.UserEmail, payment.OrderReference, dto.TrainId, pnrs, payment.TotalAmount);

            return new BookingConfirmationDto
            {
                OrderReference = payment.OrderReference,
                PaymentStatus = payment.Status,
                TrainId = payment.TrainId,
                TotalFare = payment.TotalAmount,
                UserEmail = payment.UserEmail,
                Pnrs = pnrs,
                Message = "Payment confirmed and booking saved successfully."
            };
        }

        public async Task<bool> MarkPaymentCancelledAsync(string orderReference)
        {
            if (string.IsNullOrWhiteSpace(orderReference))
            {
                return false;
            }

            var payment = await _paymentRepository.GetByOrderReferenceAsync(orderReference);
            if (payment == null || string.Equals(payment.Status, "Paid", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            payment.Status = "Cancelled";
            payment.FailureReason = "Payment was cancelled by the user.";
            await _paymentRepository.SaveChangesAsync();
            return true;
        }

        public async Task<BookingStatusDto?> GetBookingStatusAsync(string pnr)
        {
            if (string.IsNullOrWhiteSpace(pnr))
            {
                return null;
            }

            var booking = await _bookingRepository.GetByPnrAsync(pnr.Trim().ToUpperInvariant());
            if (booking == null)
            {
                return null;
            }

            Payment? payment = null;
            if (booking.PaymentId.HasValue)
            {
                payment = await _paymentRepository.GetByIdAsync(booking.PaymentId.Value);
            }

            return new BookingStatusDto
            {
                PNR = booking.PNR,
                TrainId = booking.TrainId,
                BookingStatus = booking.Status,
                PaymentStatus = payment?.Status ?? "Unknown",
                CheckedIn = booking.IsCheckedIn,
                SeatNumber = booking.SeatNumber,
                SeatAllocationStatus = booking.Status == "Cancelled"
                    ? "Not applicable"
                    : booking.IsCheckedIn || !string.IsNullOrWhiteSpace(booking.SeatNumber)
                        ? "Allocated"
                        : "Pending",
            };
        }

        public async Task<bool> CancelTicketAsync(string pnr)
        {
            var booking = await _bookingRepository.GetByPnrAsync(pnr);

            if (booking == null)
            {
                return false;
            }

            var userEmail = await _accountRepository.GetUserEmailByIdOrUserNameAsync(booking.UserId);

            if (booking.Status == "Cancelled" && !booking.IsCheckedIn && string.IsNullOrEmpty(booking.SeatNumber))
            {
                return false;
            }

            var train = await _trainRepository.GetByIdAsync(booking.TrainId);
            if (train != null && !string.IsNullOrEmpty(booking.SeatNumber))
            {
                train.AvailableSeats = Math.Min(train.TotalSeats, train.AvailableSeats + 1);
            }

            booking.Status = "Cancelled";
            booking.IsCheckedIn = false;
            booking.SeatNumber = string.Empty;

            var checkIn = await _bookingRepository.GetCheckInByBookingIdAsync(booking.Id);
            if (checkIn != null)
            {
                _bookingRepository.RemoveCheckIn(checkIn);
            }

            await _bookingRepository.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                var cancellationSubject = $"Railway Reservation Cancelled - {pnr}";
                var cancellationBody = $@"Hello,

Your ticket has been cancelled successfully.

PNR: {pnr}
Train ID: {booking.TrainId}
From: {booking.Source}
To: {booking.Destination}
Status: Cancelled
Cancelled Time (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}

If this was not requested by you, please contact support immediately.";

                try
                {
                    await _emailService.SendAsync(userEmail, cancellationSubject, cancellationBody);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cancellation completed but email failed for PNR {Pnr}.", pnr);
                }
            }

            return true;
        }

        private async Task<Session> CreateCheckoutSessionAsync(Payment payment, int passengerCount, decimal farePerTicket, decimal totalFare)
        {
            var baseUrl = _configuration["Stripe:BaseUrl"] ?? "https://localhost:7119";
            var successUrl = $"{baseUrl.TrimEnd('/')}/api/payment/success?session_id={{CHECKOUT_SESSION_ID}}&order_reference={Uri.EscapeDataString(payment.OrderReference)}";
            var cancelUrl = $"{baseUrl.TrimEnd('/')}/api/payment/cancel?order_reference={Uri.EscapeDataString(payment.OrderReference)}";

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                PaymentMethodTypes = new List<string> { "card" },
                CustomerEmail = payment.UserEmail,
                ClientReferenceId = payment.OrderReference,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = payment.Currency,
                            UnitAmount = (long)Math.Round(totalFare * 100m),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Train Ticket - {payment.TrainId}",
                                Description = $"{payment.SourceCode} to {payment.DestinationCode} x {passengerCount} passenger(s) at {farePerTicket:C} each"
                            }
                        }
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    ["order_reference"] = payment.OrderReference,
                    ["train_id"] = payment.TrainId,
                    ["user_id"] = payment.UserId,
                    ["passenger_count"] = passengerCount.ToString()
                }
            };

            var service = new SessionService();
            return await service.CreateAsync(options);
        }

        private async Task SendConfirmationEmailAsync(string userEmail, string orderReference, string trainId, IEnumerable<string> pnrs, decimal totalFare)
        {
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return;
            }

            var subject = $"Booking Confirmed - {orderReference}";
            var body = $@"Hello,

Your payment was successful and your booking has been confirmed.

Order Reference: {orderReference}
Train ID: {trainId}
PNRs: {string.Join(", ", pnrs)}
Total Fare: {totalFare:C}
Confirmed Time (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}

Have a safe journey.";

            try
            {
                await _emailService.SendAsync(userEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Booking confirmation email failed for order {OrderReference}.", orderReference);
            }
        }

        private List<PassengerDetailDto> ResolvePassengers(BookingRequestDto dto)
        {
            var passengers = dto.Passengers
                .Where(passenger => !string.IsNullOrWhiteSpace(passenger.PassengerName))
                .ToList();

            _validator.ValidatePassengers(passengers);
            return passengers;
        }

        private static string GeneratePnr()
        {
            return $"PNR-{Guid.NewGuid():N}".Substring(0, 15).ToUpperInvariant();
        }

        public async Task<List<Models.Entities.Payment>> GetPaymentsForUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new List<Models.Entities.Payment>();
            }

            return await _paymentRepository.GetByUserIdAsync(userId);
        }
    }
}