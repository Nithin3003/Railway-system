using RailwayReservationSystem.Models.DTOs;

namespace RailwayReservationSystem.Interfaces
{
    public interface IBookingService
    {
        Task<ReservationResultDto> ReserveTicketAsync(BookingRequestDto dto, string userId);
        Task<BookingConfirmationDto?> FinalizePaymentAsync(string checkoutSessionId, string? orderReference);
        Task<bool> MarkPaymentCancelledAsync(string orderReference);
        Task<BookingStatusDto?> GetBookingStatusAsync(string pnr);
        Task<bool> CancelTicketAsync(string pnr);
        Task<List<RailwayReservationSystem.Models.Entities.Payment>> GetPaymentsForUserAsync(string userId);
    }
}
