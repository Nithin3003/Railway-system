using RailwayReservationSystem.Models.DTOs;
using RailwayReservationSystem.Models.Entities;

namespace RailwayReservationSystem.Interfaces
{
    public interface IAdminService
    {
        Task<Train?> UpdateTrainAsync(string id, UpdateTrainDto dto);
        Task<bool> DeleteTrainAsync(string id);
        Task<(string TrainId, int Stations)> CreateTrainWithRouteAsync(TrainWithRouteDto dto);
        Task<List<PaymentStatusDto>> GetTrainPaymentHistoryAsync(string trainId);
        Task<TrainDashboardDto?> GetTrainDashboardAsync(string trainId);
        Task<List<RailwayReservationSystem.Models.DTOs.UserDetailsDto>> GetAllUsersAsync();
    }
}
