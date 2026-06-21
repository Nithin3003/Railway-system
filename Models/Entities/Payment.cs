using System.Text.Json.Serialization;

namespace RailwayReservationSystem.Models.Entities
{
    public class Payment
    {
        public int Id { get; set; }
        public string OrderReference { get; set; } = string.Empty;
        public string RazorpayOrderId { get; set; } = string.Empty;
        public string? RazorpayPaymentId { get; set; }
        public string? RazorpaySignature { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string TrainId { get; set; } = string.Empty;
        public string SourceCode { get; set; } = string.Empty;
        public string DestinationCode { get; set; } = string.Empty;
        public int PassengerCount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "usd";
        public string Status { get; set; } = "Pending";
        public string? FailureReason { get; set; }
        public string? RequestPayloadJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }

        [JsonIgnore]
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}