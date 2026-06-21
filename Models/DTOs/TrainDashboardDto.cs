namespace RailwayReservationSystem.Models.DTOs
{
    public class TrainDashboardDto
    {
        public string TrainId { get; set; } = string.Empty;
        public string TrainName { get; set; } = string.Empty;
        public int TotalCapacity { get; set; }
        public int SeatsLeft { get; set; }
        public int TotalBookings { get; set; }
        public int ConfirmedCount { get; set; }
        public int CancelledCount { get; set; }
        public int CheckedInCount { get; set; }
        public int PaidPayments { get; set; }
        public int PendingPayments { get; set; }
        public int FailedPayments { get; set; }
        public decimal TotalRevenue { get; set; }
        public DateTime? LastPaymentAt { get; set; }
    }
}
