using RailwayReservationSystem.Models.Entities;

namespace RailwayReservationSystem.Interfaces
{
    public interface IBookingRepository
    {
        Task AddBookingsAsync(IEnumerable<Booking> bookings);
        Task<Booking?> GetByPnrAsync(string pnr);
        Task<List<Booking>> GetByTrainIdAsync(string trainId);
        Task<List<Booking>> GetByUserIdAsync(string userId);
        Task<List<Booking>> GetByPaymentIdAsync(int paymentId);
        Task<List<Booking>> GetAllAsync();
        Task<List<string>> GetAssignedSeatsAsync(string trainId);
        Task<CheckIn?> GetCheckInByBookingIdAsync(int bookingId);
        Task AddCheckInAsync(CheckIn checkIn);
        void RemoveCheckIn(CheckIn checkIn);
        Task SaveChangesAsync();
    }
}
