namespace RailwayReservationSystem.Models.DTOs
{
    public class CheckInStatusDto
    {
        public string PNR { get; set; } = string.Empty;
        public string PassengerName { get; set; } = string.Empty;
        public bool CheckedIn { get; set; }
        public string? Seat { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
