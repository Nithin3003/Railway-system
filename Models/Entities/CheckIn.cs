using Microsoft.AspNetCore.Identity;

namespace RailwayReservationSystem.Models.Entities
{
    public class CheckIn 
{
    public int Id { get; set; }
    public int BookingId { get; set; } // Foreign Key
    public string SeatNumber { get; set; } = string.Empty;
    public string CheckInReference { get; set; } = string.Empty; // Unique
}
}
