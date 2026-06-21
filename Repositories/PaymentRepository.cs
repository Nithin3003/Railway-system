using Microsoft.EntityFrameworkCore;
using RailwayReservationSystem.Data;
using RailwayReservationSystem.Interfaces;
using RailwayReservationSystem.Models.Entities;

namespace RailwayReservationSystem.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly ApplicationDbContext _context;

        public PaymentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
        }

        public async Task<Payment?> GetByIdAsync(int id)
        {
            return await _context.Payments.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Payment?> GetByOrderReferenceAsync(string orderReference)
        {
            return await _context.Payments.FirstOrDefaultAsync(p => p.OrderReference == orderReference);
        }

        public async Task<Payment?> GetByCheckoutSessionIdAsync(string checkoutSessionId)
        {
            return await _context.Payments.FirstOrDefaultAsync(p => p.RazorpayOrderId == checkoutSessionId);
        }

        public async Task<List<Payment>> GetByTrainIdAsync(string trainId)
        {
            return await _context.Payments
                .Where(p => p.TrainId == trainId)
                .Include(p => p.Bookings)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Payment>> GetByUserIdAsync(string userId)
        {
            return await _context.Payments
                .Where(p => p.UserId == userId)
                .Include(p => p.Bookings)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}