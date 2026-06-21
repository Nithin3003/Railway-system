namespace RailwayReservationSystem.Models.DTOs
{
    public class CheckInResultDto
    {
        public string PNR { get; set; } = string.Empty;
        public string PassengerName { get; set; } = string.Empty;
        public string Seat { get; set; } = string.Empty;
        public string CheckInRef { get; set; } = string.Empty;
        public string TrainId { get; set; } = string.Empty;
    }
}
