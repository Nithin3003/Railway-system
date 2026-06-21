using RailwayReservationSystem.Interfaces;
using RailwayReservationSystem.Models.DTOs;
using RailwayReservationSystem.Models.Entities;

namespace RailwayReservationSystem.Services
{
    using Microsoft.AspNetCore.Identity;
    using RailwayReservationSystem.Models.Entities;

    public class AdminService : IAdminService
    {
        private readonly ITrainRepository _trainRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminService(
            ITrainRepository trainRepository,
            IBookingRepository bookingRepository,
            IPaymentRepository paymentRepository,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _trainRepository = trainRepository;
            _bookingRepository = bookingRepository;
            _paymentRepository = paymentRepository;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<Train?> UpdateTrainAsync(string id, UpdateTrainDto dto)
        {
            var train = await _trainRepository.GetByIdAsync(id);
            if (train == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(dto.TrainNumber))
            {
                train.TrainNumber = dto.TrainNumber;
            }

            train.DepartureTime = dto.DepartureTime;
            train.Fare = dto.Fare;
            train.TrainName = dto.TrainName;
            train.TotalSeats = dto.TotalSeats;
            train.AvailableSeats = dto.AvailableSeats;

            var sourceCode = dto.SourceCode?.Trim().ToUpper();
            var destinationCode = dto.DestinationCode?.Trim().ToUpper();

            var route = await _trainRepository.GetRouteAsync(id);

            if (!string.IsNullOrWhiteSpace(sourceCode))
            {
                var src = route.FirstOrDefault(r => r.StationCode == sourceCode);
                if (src == null)
                {
                    throw new ArgumentException($"Invalid SourceCode '{sourceCode}' for train {id}.");
                }
                train.Source = src.StationName;
            }
            else
            {
                train.Source = dto.Source;
            }

            if (!string.IsNullOrWhiteSpace(destinationCode))
            {
                var dest = route.FirstOrDefault(r => r.StationCode == destinationCode);
                if (dest == null)
                {
                    throw new ArgumentException($"Invalid DestinationCode '{destinationCode}' for train {id}.");
                }
                train.Destination = dest.StationName;
            }
            else
            {
                train.Destination = dto.Destination;
            }

            var intermediateCodes = dto.IntermediateStationCodes
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim().ToUpper())
                .Distinct()
                .ToList();

            if (intermediateCodes.Count > 0)
            {
                var valid = route.Select(r => r.StationCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (intermediateCodes.Any(c => !valid.Contains(c)))
                {
                    throw new ArgumentException("One or more IntermediateStationCodes are invalid for this train.");
                }
            }

            await _trainRepository.SaveChangesAsync();
            return train;
        }

        public async Task<bool> DeleteTrainAsync(string id)
        {
            var train = await _trainRepository.GetByIdAsync(id);
            if (train == null)
            {
                return false;
            }

            _trainRepository.Remove(train);
            await _trainRepository.SaveChangesAsync();
            return true;
        }

        public async Task<(string TrainId, int Stations)> CreateTrainWithRouteAsync(TrainWithRouteDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TrainId))
            {
                throw new ArgumentException("TrainId is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.TrainNumber))
            {
                throw new ArgumentException("TrainNumber is required.");
            }

            if (dto.TotalSeats <= 0)
            {
                throw new ArgumentException("TotalSeats must be greater than zero.");
            }

            if (dto.Stations == null || dto.Stations.Count < 2)
            {
                throw new ArgumentException("At least two stations are required to define a route.");
            }

            if (await _trainRepository.ExistsByIdAsync(dto.TrainId))
            {
                throw new InvalidOperationException($"Train with Id '{dto.TrainId}' already exists.");
            }

            if (await _trainRepository.ExistsByNumberAsync(dto.TrainNumber))
            {
                throw new InvalidOperationException($"Train number '{dto.TrainNumber}' already exists.");
            }

            var orderedStations = dto.Stations.OrderBy(s => s.Order).ToList();
            if (orderedStations.Any(s => string.IsNullOrWhiteSpace(s.StationName) || string.IsNullOrWhiteSpace(s.StationCode)))
            {
                throw new ArgumentException("Each station needs StationName and StationCode.");
            }

            await _trainRepository.ExecuteInTransactionAsync(async () =>
            {
                var train = new Train
                {
                    Id = dto.TrainId,
                    TrainNumber = dto.TrainNumber,
                    TrainName = dto.TrainName,
                    Source = orderedStations.First().StationName,
                    Destination = orderedStations.Last().StationName,
                    TotalSeats = dto.TotalSeats,
                    AvailableSeats = dto.TotalSeats,
                    Fare = orderedStations.Last().FareFromStart,
                    DepartureTime = dto.DepartureTime
                };

                await _trainRepository.AddAsync(train);

                var stations = orderedStations.Select(station => new TrainStation
                {
                    TrainId = train.Id,
                    StationCode = station.StationCode.Trim().ToUpper(),
                    StationName = station.StationName,
                    StopOrder = station.Order,
                    FareFromStart = station.FareFromStart
                });

                await _trainRepository.AddStationsAsync(stations);
                await _trainRepository.SaveChangesAsync();
            });

            return (dto.TrainId, orderedStations.Count);
        }

        public async Task<TrainDashboardDto?> GetTrainDashboardAsync(string trainId)
        {
            var train = await _trainRepository.GetByIdAsync(trainId);
            if (train == null)
            {
                return null;
            }

            var bookings = await _bookingRepository.GetByTrainIdAsync(trainId);

            return new TrainDashboardDto
            {
                TrainId = train.Id,
                TrainName = train.TrainName,
                TotalCapacity = train.TotalSeats,
                SeatsLeft = train.AvailableSeats,
                TotalBookings = bookings.Count,
                ConfirmedCount = bookings.Count(b => b.Status == "Confirmed"),
                CancelledCount = bookings.Count(b => b.Status == "Cancelled"),
                CheckedInCount = bookings.Count(b => b.IsCheckedIn),
                TotalRevenue = bookings.Where(b => b.Status != "Cancelled").Sum(b => b.Fare)
            };
        }

        public async Task<List<PaymentStatusDto>> GetTrainPaymentHistoryAsync(string trainId)
        {
            var payments = await _paymentRepository.GetByTrainIdAsync(trainId);
            return payments.Select(payment => {
                var pnrs = payment.Bookings?.Select(b => b.PNR).Where(p => !string.IsNullOrWhiteSpace(p)).ToList() ?? new List<string>();
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
        }

        public async Task<List<RailwayReservationSystem.Models.DTOs.UserDetailsDto>> GetAllUsersAsync()
        {
            var users = _userManager.Users.ToList();
            var result = new List<RailwayReservationSystem.Models.DTOs.UserDetailsDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new RailwayReservationSystem.Models.DTOs.UserDetailsDto
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    FullName = (user as ApplicationUser)?.FullName ?? string.Empty,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumber = user.PhoneNumber,
                    Roles = roles.ToList()
                });
            }

            return result;
        }
    }
}
