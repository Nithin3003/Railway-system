using Microsoft.AspNetCore.Identity;

namespace RailwayReservationSystem.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
    }
}