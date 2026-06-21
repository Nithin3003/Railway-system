using RailwayReservationSystem.Models.DTOs;

namespace RailwayReservationSystem.Interfaces
{
    public interface ICheckInService
    {
        Task<CheckInResultDto?> PerformCheckInAsync(string pnr);
        Task<CheckInStatusDto?> GetStatusAsync(string pnr);
    }
}
