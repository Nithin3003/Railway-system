namespace RailwayReservationSystem.Models.DTOs
{
    public class TravelPlanFareRequest
    {
        public string TrainId { get; set; } = string.Empty;
        public string SourceCode { get; set; } = string.Empty;
        public string DestinationCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a train station in a route
    /// </summary>
    public class TrainStationDto
    {
        public string StationCode { get; set; } = string.Empty;
        public string StationName { get; set; } = string.Empty;
        public int Order { get; set; }
        public decimal FareFromStart { get; set; }
    }

    /// <summary>
    /// Complete train information with route details
    /// </summary>
    public class TrainViewDto
    {
        public string Id { get; set; } = string.Empty;
        public string TrainNumber { get; set; } = string.Empty;
        public string TrainName { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
        public decimal BaseFare { get; set; }
        public DateTime DepartureTime { get; set; }
        public List<TrainStationDto> Stations { get; set; } = new();
    }

    /// <summary>
    /// Fare calculation result between two stations
    /// </summary>
    public class FarePlanResultDto
    {
        public string TrainId { get; set; } = string.Empty;
        public string TrainName { get; set; } = string.Empty;
        public string SourceCode { get; set; } = string.Empty;
        public string DestinationCode { get; set; } = string.Empty;
        public decimal Fare { get; set; }
    }
}
