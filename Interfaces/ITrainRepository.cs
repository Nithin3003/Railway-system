using RailwayReservationSystem.Models.Entities;

namespace RailwayReservationSystem.Interfaces
{
    public interface ITrainRepository
    {
        Task<Train?> GetByIdAsync(string trainId);
        Task<bool> ExistsByIdAsync(string trainId);
        Task<bool> ExistsByNumberAsync(string trainNumber);
        Task<List<Train>> GetAllAsync();
        Task<List<TrainStation>> GetRouteAsync(string trainId);
        Task<List<TrainStation>> GetStationsForTrainIdsAsync(List<string> trainIds);
        Task AddAsync(Train train);
        Task AddStationsAsync(IEnumerable<TrainStation> stations);
        void Remove(Train train);
        Task ExecuteInTransactionAsync(Func<Task> action);
        Task SaveChangesAsync();
    }
}
