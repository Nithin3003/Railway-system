using RailwayReservationSystem.Models.DTOs;

namespace RailwayReservationSystem.Interfaces
{
    public interface ISearchService
    {
        Task<List<TrainViewDto>> GetAllTrainsAsync();
        Task<FarePlanResultDto?> CheckFarePlanAsync(TravelPlanFareRequest request);
    }
}
