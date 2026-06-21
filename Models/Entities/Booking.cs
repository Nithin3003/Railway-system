namespace RailwayReservationSystem.Models.Entities
{
    public class Booking
    {
        public int Id { get; set; }
        public int? PaymentId { get; set; }
        public string PNR { get; set; } = string.Empty; // Unique Identifier
        public string TrainId { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public decimal Fare { get; set; }
        public string UserId { get; set; } = string.Empty; // Links to ApplicationUser
        public string PassengerName { get; set; } = string.Empty;
        public int PassengerAge { get; set; }
        public string Sex { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string Class { get; set; } = "Economy";
        public string SeatNumber { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Confirmed"; // Confirmed/Cancelled
        public bool IsCheckedIn { get; set; } = false;
    }
}