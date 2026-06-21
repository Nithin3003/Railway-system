namespace RailwayReservationSystem.Models.DTOs
{
    /// <summary>
    /// DEPRECATED: Use TrainViewDto from SearchDtos.cs instead.
    /// This class is kept for backward compatibility only.
    /// </summary>
    public class TrainWithRouteDto
    {
        public string TrainId { get; set; } = string.Empty;
        public string TrainNumber { get; set; } = string.Empty;
        public string TrainName { get; set; } = string.Empty;
        public int TotalSeats { get; set; }
        public decimal BaseFare { get; set; }
        public DateTime DepartureTime { get; set; }
        public List<TrainRouteStationDto> Stations { get; set; } = new();
    }

    /// <summary>
    /// DEPRECATED: Use TrainStationDto from SearchDtos.cs instead.
    /// </summary>
    public class TrainRouteStationDto
    {
        public string StationCode { get; set; } = string.Empty;
        public string StationName { get; set; } = string.Empty;
        public int Order { get; set; }
        public decimal FareFromStart { get; set; }
    }
}
