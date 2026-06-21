using Microsoft.AspNetCore.Identity;

namespace RailwayReservationSystem.Models.Entities
{
public class Admin 
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty; // Up to 100 chars
    public string Password { get; set; } = string.Empty; 
}

}