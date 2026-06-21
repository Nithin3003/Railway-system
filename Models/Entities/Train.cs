namespace RailwayReservationSystem.Models.Entities
{
public class Train
{
    public string Id { get; set; } = string.Empty; // Simple numeric format like "02603"
    public required string TrainNumber { get; set; } = string.Empty;
    public string TrainName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public decimal Fare { get; set; }
    public DateTime DepartureTime { get; set; }
}
}