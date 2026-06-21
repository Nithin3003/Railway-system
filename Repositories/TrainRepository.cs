using Microsoft.EntityFrameworkCore;
using RailwayReservationSystem.Data;
using RailwayReservationSystem.Interfaces;
using RailwayReservationSystem.Models.Entities;

namespace RailwayReservationSystem.Repositories
{
    public class TrainRepository : ITrainRepository
    {
        private readonly ApplicationDbContext _context;

        public TrainRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Train?> GetByIdAsync(string trainId)
        {
            return await _context.Trains.FirstOrDefaultAsync(t => t.Id == trainId);
        }

        public async Task<bool> ExistsByIdAsync(string trainId)
        {
            return await _context.Trains.AnyAsync(t => t.Id == trainId);
        }

        public async Task<bool> ExistsByNumberAsync(string trainNumber)
        {
            return await _context.Trains.AnyAsync(t => t.TrainNumber == trainNumber);
        }

        public async Task<List<Train>> GetAllAsync()
        {
            return await _context.Trains.OrderBy(t => t.TrainNumber).ToListAsync();
        }

        public async Task<List<TrainStation>> GetRouteAsync(string trainId)
        {
            return await _context.TrainStations
                .Where(s => s.TrainId == trainId)
                .OrderBy(s => s.StopOrder)
                .ToListAsync();
        }

        public async Task<List<TrainStation>> GetStationsForTrainIdsAsync(List<string> trainIds)
        {
            return await _context.TrainStations
                .Where(s => trainIds.Contains(s.TrainId))
                .OrderBy(s => s.StopOrder)
                .ToListAsync();
        }

        public async Task AddAsync(Train train)
        {
            await _context.Trains.AddAsync(train);
        }

        public async Task AddStationsAsync(IEnumerable<TrainStation> stations)
        {
            await _context.TrainStations.AddRangeAsync(stations);
        }

        public void Remove(Train train)
        {
            _context.Trains.Remove(train);
        }

        public async Task ExecuteInTransactionAsync(Func<Task> action)
        {
            using var tx = await _context.Database.BeginTransactionAsync();
            await action();
            await tx.CommitAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
