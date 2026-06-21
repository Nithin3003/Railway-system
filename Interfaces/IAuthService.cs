using RailwayReservationSystem.Models.DTOs;

namespace RailwayReservationSystem.Interfaces
{
    public interface IAuthService
    {
        Task<string?> LoginAsync(LoginDto model);
        Task<(bool Succeeded, string Message)> RegisterAsync(RegisterDto model);
    }
}
