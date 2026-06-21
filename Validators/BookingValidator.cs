using RailwayReservationSystem.Models.DTOs;
using RailwayReservationSystem.Models.Entities;
using RailwayReservationSystem.Common;

namespace RailwayReservationSystem.Validators
{
    /// <summary>
    /// Centralized validation logic for booking operations
    /// </summary>
    public class BookingValidator
    {
        public void ValidatePassengers(List<PassengerDetailDto> passengers)
        {
            if (passengers == null || passengers.Count == 0)
            {
                throw new ArgumentException("At least one passenger is required.");
            }

            if (passengers.Count > 6)
            {
                throw new ArgumentException("One passenger can book for 6 tickets at a time only.");
            }

            if (passengers.Any(p => string.IsNullOrWhiteSpace(p.PassengerName)))
            {
                throw new ArgumentException("Passenger name is required for each passenger.");
            }

            if (passengers.Any(p => p.Age <= 0))
            {
                throw new ArgumentException("Passenger age must be greater than 0 for all passengers.");
            }

            if (passengers.Any(p => string.IsNullOrWhiteSpace(p.Sex)))
            {
                throw new ArgumentException("Passenger sex is required for all passengers.");
            }

            if (passengers.Any(p => string.IsNullOrWhiteSpace(p.Address)))
            {
                throw new ArgumentException("Passenger address is required for all passengers.");
            }
        }

        public void ValidateSeatAvailability(Train train, int requiredSeats)
        {
            if (train == null)
            {
                throw new KeyNotFoundException("Train not found. Please select a valid train.");
            }

            if (train.AvailableSeats < requiredSeats)
            {
                throw new ArgumentException(
                    $"Only {train.AvailableSeats} seats are available on train {train.TrainName}. Requested {requiredSeats} tickets.");
            }
        }

        public void ValidateStationCodes(string sourceCode, string destinationCode)
        {
            if (sourceCode.IsEmpty() || destinationCode.IsEmpty())
            {
                throw new ArgumentException("SourceCode and DestinationCode are required. Please enter station codes only.");
            }
        }

        public void ValidateRoute(List<TrainStation> routeStops, string trainId)
        {
            if (routeStops.Count == 0)
            {
                throw new ArgumentException($"Route is not configured for train {trainId}. Please contact admin.");
            }
        }

        public void ValidateStationSelection(TrainStation? startStation, TrainStation? endStation, string sourceCode, string destinationCode, List<TrainStation> routeStops)
        {
            if (startStation == null || endStation == null)
            {
                var validCodes = string.Join(", ", routeStops.Select(s => s.StationCode));
                throw new ArgumentException($"Invalid station code(s): {sourceCode}, {destinationCode}. Valid codes: {validCodes}");
            }

            if (startStation.StopOrder >= endStation.StopOrder)
            {
                throw new ArgumentException(
                    $"Invalid route direction. Train does not travel from {sourceCode} to {destinationCode} in this order.");
            }
        }

        public void ValidateCancelPNR(string pnr)
        {
            if (pnr.IsEmpty())
            {
                throw new ArgumentException("PNR is required.");
            }

            if (!pnr.StartsWith("PNR"))
            {
                throw new ArgumentException("Invalid PNR format. PNR should start with 'PNR'.");
            }
        }
    }
}
