using Microsoft.EntityFrameworkCore;
using RailwayReservationSystem.Data;
using RailwayReservationSystem.Interfaces;
using RailwayReservationSystem.Models.Entities;

namespace RailwayReservationSystem.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly ApplicationDbContext _context;

        public BookingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddBookingsAsync(IEnumerable<Booking> bookings)
        {
            await _context.Bookings.AddRangeAsync(bookings);
        }

        public async Task<Booking?> GetByPnrAsync(string pnr)
        {
            return await _context.Bookings.FirstOrDefaultAsync(b => b.PNR == pnr);
        }

        public async Task<List<Booking>> GetByTrainIdAsync(string trainId)
        {
            return await _context.Bookings.Where(b => b.TrainId == trainId).ToListAsync();
        }

        public async Task<List<Booking>> GetByUserIdAsync(string userId)
        {
            return await _context.Bookings
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task<List<Booking>> GetByPaymentIdAsync(int paymentId)
        {
            return await _context.Bookings
                .Where(b => b.PaymentId == paymentId)
                .OrderBy(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task<List<Booking>> GetAllAsync()
        {
            return await _context.Bookings
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task<List<string>> GetAssignedSeatsAsync(string trainId)
        {
            return await _context.Bookings
                .Where(b => b.TrainId == trainId && b.IsCheckedIn && !string.IsNullOrWhiteSpace(b.SeatNumber))
                .Select(b => b.SeatNumber)
                .ToListAsync();
        }

        public async Task<CheckIn?> GetCheckInByBookingIdAsync(int bookingId)
        {
            return await _context.CheckIns.FirstOrDefaultAsync(c => c.BookingId == bookingId);
        }

        public async Task AddCheckInAsync(CheckIn checkIn)
        {
            await _context.CheckIns.AddAsync(checkIn);
        }

        public void RemoveCheckIn(CheckIn checkIn)
        {
            _context.CheckIns.Remove(checkIn);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
