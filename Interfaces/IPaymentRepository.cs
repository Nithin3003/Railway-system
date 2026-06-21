using RailwayReservationSystem.Models.Entities;

namespace RailwayReservationSystem.Interfaces
{
    public interface IPaymentRepository
    {
        Task AddAsync(Payment payment);
        Task<Payment?> GetByIdAsync(int id);
        Task<Payment?> GetByOrderReferenceAsync(string orderReference);
        Task<Payment?> GetByCheckoutSessionIdAsync(string checkoutSessionId);
        Task<List<Payment>> GetByTrainIdAsync(string trainId);
        Task<List<Payment>> GetByUserIdAsync(string userId);
        Task SaveChangesAsync();
    }
}