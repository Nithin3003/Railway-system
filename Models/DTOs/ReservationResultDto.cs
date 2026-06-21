namespace RailwayReservationSystem.Models.DTOs
{
    public class ReservedTicketDto
    {
        public string PNR { get; set; } = string.Empty;
        public string PassengerName { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Sex { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string BookingStatus { get; set; } = "Confirmed";
        public string CheckInStatus { get; set; } = "Pending";
        public string? SeatNumber { get; set; }
    }

    public class ReservationResultDto
    {
        public string OrderReference { get; set; } = string.Empty;
        public string PaymentUrl { get; set; } = string.Empty;
        public string TrainId { get; set; } = string.Empty;
        public string SourceCode { get; set; } = string.Empty;
        public string DestinationCode { get; set; } = string.Empty;
        public int PassengerCount { get; set; }
        public decimal FarePerTicket { get; set; }
        public decimal TotalFare { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class BookingConfirmationDto
    {
        public string OrderReference { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string TrainId { get; set; } = string.Empty;
        public decimal TotalFare { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public List<string> Pnrs { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    public class BookingStatusDto
    {
        public string PNR { get; set; } = string.Empty;
        public string TrainId { get; set; } = string.Empty;
        public string BookingStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public bool CheckedIn { get; set; }
        public string? SeatNumber { get; set; }
        public string SeatAllocationStatus { get; set; } = string.Empty;
    }

    public class PaymentStatusDto
    {
        public string OrderReference { get; set; } = string.Empty;
        public string TrainId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int PassengerCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? FailureReason { get; set; }
        public List<string> Pnrs { get; set; } = new();
        public string? EmailSubject { get; set; }
        public string? EmailBody { get; set; }
    }
}
