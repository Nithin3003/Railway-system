using RailwayReservationSystem.Interfaces;
using RailwayReservationSystem.Models.DTOs;

namespace RailwayReservationSystem.Services
{
    public class SearchService : ISearchService
    {
        private readonly ITrainRepository _trainRepository;

        public SearchService(ITrainRepository trainRepository)
        {
            _trainRepository = trainRepository;
        }

        public async Task<List<TrainViewDto>> GetAllTrainsAsync()
        {
            var trains = await _trainRepository.GetAllAsync();
            var trainIds = trains.Select(t => t.Id).ToList();
            var allStops = await _trainRepository.GetStationsForTrainIdsAsync(trainIds);

            return trains.Select(train =>
            {
                var stops = allStops
                    .Where(s => s.TrainId == train.Id)
                    .OrderBy(s => s.StopOrder)
                    .Select(s => new TrainStationDto
                    {
                        StationCode = s.StationCode,
                        StationName = s.StationName,
                        Order = s.StopOrder,
                        FareFromStart = s.FareFromStart
                    })
                    .ToList();

                return new TrainViewDto
                {
                    Id = train.Id,
                    TrainNumber = train.TrainNumber,
                    TrainName = train.TrainName,
                    Source = train.Source,
                    Destination = train.Destination,
                    TotalSeats = train.TotalSeats,
                    AvailableSeats = train.AvailableSeats,
                    BaseFare = train.Fare,
                    DepartureTime = train.DepartureTime,
                    Stations = stops
                };
            }).ToList();
        }

        public async Task<FarePlanResultDto?> CheckFarePlanAsync(TravelPlanFareRequest request)
        {
            var train = await _trainRepository.GetByIdAsync(request.TrainId);
            if (train == null)
            {
                return null;
            }

            var routeStops = await _trainRepository.GetRouteAsync(request.TrainId);
            if (!routeStops.Any())
            {
                throw new ArgumentException("Route stations are not defined for this train.");
            }

            var src = request.SourceCode.Trim().ToUpper();
            var dest = request.DestinationCode.Trim().ToUpper();

            var startStation = routeStops.FirstOrDefault(s => s.StationCode.Equals(src, StringComparison.OrdinalIgnoreCase));
            var endStation = routeStops.FirstOrDefault(s => s.StationCode.Equals(dest, StringComparison.OrdinalIgnoreCase));

            if (startStation == null || endStation == null)
            {
                var validCodes = string.Join(", ", routeStops.Select(s => s.StationCode));
                throw new ArgumentException($"Invalid station code(s). Valid station codes: {validCodes}.");
            }

            if (startStation.StopOrder >= endStation.StopOrder)
            {
                throw new ArgumentException($"Invalid route direction. Train does not travel from {src} to {dest} in this order.");
            }

            return new FarePlanResultDto
            {
                TrainId = train.Id,
                TrainName = train.TrainName,
                SourceCode = src,
                DestinationCode = dest,
                Fare = endStation.FareFromStart - startStation.FareFromStart
            };
        }
    }
}
