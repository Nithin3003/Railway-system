using RailwayReservationSystem.Models.Entities;

namespace RailwayReservationSystem.Interfaces
{
    public interface IAccountRepository
    {
        Task<ApplicationUser?> FindByUsernameAsync(string username);
        Task<ApplicationUser?> FindByEmailAsync(string email);
        Task<bool> CheckPasswordAsync(ApplicationUser user, string password);
        Task<IList<string>> GetRolesAsync(ApplicationUser user);
        Task<(bool Succeeded, string? Error)> CreateUserAsync(ApplicationUser user, string password);
        Task EnsureRoleExistsAsync(string role);
        Task AddUserToRoleAsync(ApplicationUser user, string role);
        Task<string?> GetUserEmailByIdOrUserNameAsync(string value);
    }
}
