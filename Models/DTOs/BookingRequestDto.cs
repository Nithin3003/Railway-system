namespace RailwayReservationSystem.Models.DTOs
{
    public class PassengerDetailDto
    {
        public string PassengerName { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Sex { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class BookingRequestDto
    {
        public string TrainId { get; set; } = string.Empty;
        public string SourceCode { get; set; } = string.Empty;
        public string DestinationCode { get; set; } = string.Empty;

        // Preferred input for booking (max 6 passengers).
        public List<PassengerDetailDto> Passengers { get; set; } = new();

        public string BankName { get; set; } = string.Empty;
        public string Class { get; set; } = "Economy"; // Economy, Business, First
    }
}
