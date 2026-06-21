namespace RailwayReservationSystem.Models.DTOs
{
    public class UpdateTrainDto
    {
        public string Id { get; set; } = string.Empty;
        public string TrainNumber { get; set; } = string.Empty;
        public string TrainName { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string SourceCode { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string DestinationCode { get; set; } = string.Empty;
        public List<string> IntermediateStationCodes { get; set; } = new();
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
        public decimal Fare { get; set; }
        public DateTime DepartureTime { get; set; }
    }
}
