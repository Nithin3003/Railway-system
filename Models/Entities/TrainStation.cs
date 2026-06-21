namespace RailwayReservationSystem.Models.Entities
{
    public class TrainStation
    {
        public int Id { get; set; }
        public string TrainId { get; set; } = string.Empty;
        public string StationCode { get; set; } = string.Empty;
        public string StationName { get; set; } = string.Empty;
        public int StopOrder { get; set; }
        public decimal FareFromStart { get; set; }
    }
}
