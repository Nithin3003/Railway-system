using RailwayReservationSystem.Models.Entities;

namespace RailwayReservationSystem.Services
{
    /// <summary>
    /// Calculates fares based on route stations and passenger details
    /// </summary>
    public class FareCalculator
    {
        public decimal CalculateFare(TrainStation startStation, TrainStation endStation)
        {
            if (startStation == null || endStation == null)
            {
                throw new ArgumentNullException("Stations cannot be null");
            }

            var fare = endStation.FareFromStart - startStation.FareFromStart;
            
            if (fare < 0)
            {
                throw new ArgumentException("Invalid station selection. End station fare cannot be less than start station.");
            }

            return fare;
        }

        public decimal CalculateTotalFare(decimal farePerTicket, int passengerCount)
        {
            if (passengerCount <= 0)
            {
                throw new ArgumentException("Passenger count must be greater than 0");
            }

            return farePerTicket * passengerCount;
        }
    }
}
