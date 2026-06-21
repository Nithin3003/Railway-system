using Microsoft.Extensions.Logging;
using Moq;
using RailwayReservationSystem.Interfaces;
using RailwayReservationSystem.Models.DTOs;
using RailwayReservationSystem.Models.Entities;
using RailwayReservationSystem.Services;

namespace RailwayReservationSystem.Tests;

[TestFixture]
public class RailwayRoutesTests
{
    [Test]
    public async Task SearchService_CheckFarePlan_ShouldReturnFare_WhenRouteIsValid()
    {
        var trainRepository = new Mock<ITrainRepository>();

        trainRepository.Setup(x => x.GetByIdAsync("12321")).ReturnsAsync(new Train
        {
            Id = "12321",
            TrainNumber = "12321",
            TrainName = "YNK Express",
            Source = "YNK",
            Destination = "RNK",
            TotalSeats = 100,
            AvailableSeats = 90,
            Fare = 30m,
            DepartureTime = DateTime.UtcNow.AddHours(2)
        });

        trainRepository.Setup(x => x.GetRouteAsync("12321")).ReturnsAsync(new List<TrainStation>
        {
            new() { TrainId = "12321", StationCode = "YNK", StationName = "Yelahanka", StopOrder = 1, FareFromStart = 10m },
            new() { TrainId = "12321", StationCode = "RNK", StationName = "Rajankunte", StopOrder = 2, FareFromStart = 30m }
        });

        var service = new SearchService(trainRepository.Object);

        var result = await service.CheckFarePlanAsync(new TravelPlanFareRequest
        {
            TrainId = "12321",
            SourceCode = "YNK",
            DestinationCode = "RNK"
        });

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Fare, Is.EqualTo(20m));
        Assert.That(result.SourceCode, Is.EqualTo("YNK"));
        Assert.That(result.DestinationCode, Is.EqualTo("RNK"));
    }

    [Test]
    public void SearchService_CheckFarePlan_ShouldThrow_WhenRouteDirectionIsInvalid()
    {
        var trainRepository = new Mock<ITrainRepository>();

        trainRepository.Setup(x => x.GetByIdAsync("12321")).ReturnsAsync(new Train
        {
            Id = "12321",
            TrainNumber = "12321",
            TrainName = "YNK Express"
        });

        trainRepository.Setup(x => x.GetRouteAsync("12321")).ReturnsAsync(new List<TrainStation>
        {
            new() { TrainId = "12321", StationCode = "YNK", StationName = "Yelahanka", StopOrder = 1, FareFromStart = 10m },
            new() { TrainId = "12321", StationCode = "RNK", StationName = "Rajankunte", StopOrder = 2, FareFromStart = 30m }
        });

        var service = new SearchService(trainRepository.Object);

        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await service.CheckFarePlanAsync(new TravelPlanFareRequest
        {
            TrainId = "12321",
            SourceCode = "RNK",
            DestinationCode = "YNK"
        }));

        Assert.That(ex!.Message, Does.Contain("Invalid route direction"));
    }

    [Test]
    public void BookingService_ReserveTicket_ShouldThrow_WhenStationCodeIsInvalid()
    {
        var bookingRepository = new Mock<IBookingRepository>();
        var trainRepository = new Mock<ITrainRepository>();
        var accountRepository = new Mock<IAccountRepository>();
        var emailService = new Mock<IEmailService>();
        var logger = new Mock<ILogger<BookingService>>();

        trainRepository.Setup(x => x.GetByIdAsync("12321")).ReturnsAsync(new Train
        {
            Id = "12321",
            TrainNumber = "12321",
            TrainName = "YNK Express",
            AvailableSeats = 100,
            TotalSeats = 100,
            Fare = 30m
        });

        trainRepository.Setup(x => x.GetRouteAsync("12321")).ReturnsAsync(new List<TrainStation>
        {
            new() { TrainId = "12321", StationCode = "YNK", StationName = "Yelahanka", StopOrder = 1, FareFromStart = 10m },
            new() { TrainId = "12321", StationCode = "RNK", StationName = "Rajankunte", StopOrder = 2, FareFromStart = 30m }
        });

        var service = new BookingService(
            bookingRepository.Object,
            trainRepository.Object,
            accountRepository.Object,
            emailService.Object,
            logger.Object);

        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await service.ReserveTicketAsync(new BookingRequestDto
        {
            TrainId = "12321",
            SourceCode = "AAA",
            DestinationCode = "RNK",
            BankName = "Indian Bank",
            Class = "General"
        }, "passenger1"));

        Assert.That(ex!.Message, Does.Contain("Invalid station code"));
    }

    [Test]
    public void BookingService_ReserveTicket_ShouldThrow_WhenRouteDirectionIsInvalid()
    {
        var bookingRepository = new Mock<IBookingRepository>();
        var trainRepository = new Mock<ITrainRepository>();
        var accountRepository = new Mock<IAccountRepository>();
        var emailService = new Mock<IEmailService>();
        var logger = new Mock<ILogger<BookingService>>();

        trainRepository.Setup(x => x.GetByIdAsync("12321")).ReturnsAsync(new Train
        {
            Id = "12321",
            TrainNumber = "12321",
            TrainName = "YNK Express",
            AvailableSeats = 100,
            TotalSeats = 100,
            Fare = 30m
        });

        trainRepository.Setup(x => x.GetRouteAsync("12321")).ReturnsAsync(new List<TrainStation>
        {
            new() { TrainId = "12321", StationCode = "YNK", StationName = "Yelahanka", StopOrder = 1, FareFromStart = 10m },
            new() { TrainId = "12321", StationCode = "RNK", StationName = "Rajankunte", StopOrder = 2, FareFromStart = 30m }
        });

        var service = new BookingService(
            bookingRepository.Object,
            trainRepository.Object,
            accountRepository.Object,
            emailService.Object,
            logger.Object);

        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await service.ReserveTicketAsync(new BookingRequestDto
        {
            TrainId = "12321",
            SourceCode = "RNK",
            DestinationCode = "YNK",
            BankName = "Indian Bank",
            Class = "General"
        }, "passenger1"));

        Assert.That(ex!.Message, Does.Contain("Invalid route direction"));
    }
}
