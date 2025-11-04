using HotshotLogistics.Application.Services;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Contracts.Repositories;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Http;
namespace HotshotLogistics.Tests.Utils.Infrastructure
{
    /// <summary>
public class DriverServiceTests
{
    [Fact]
    public async Task GetDriversAsync_ReturnsDriversFromRepository()
    {
var expected = new List<Driver> { new HotshotLogistics.Domain.Entities.Driver { Id = 1 }, new HotshotLogistics.Domain.Entities.Driver { Id = 2 } };
        var repoMock = new Mock<DriverRepository>();
        repoMock.Setup(r => r.GetDriversAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var service = new DriverService(repoMock.Object);
        var result = await service.GetDriversAsync();
        Assert.Equal(expected, result);
        repoMock.Verify(r => r.GetDriversAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDriverByIdAsync_UsesRepository()
    {
var driver = new HotshotLogistics.Domain.Entities.Driver { Id = 1 };
        var repoMock = new Mock<DriverRepository>();
        repoMock.Setup(r => r.GetDriverByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(driver);
        var service = new DriverService(repoMock.Object);

        var result = await service.GetDriverByIdAsync(1);

        Assert.Equal(driver, result);
        repoMock.Verify(r => r.GetDriverByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateDriverAsync_CallsRepository()
    {
var newDriver = new HotshotLogistics.Domain.Entities.Driver { Id = 1 };
        var repoMock = new Mock<DriverRepository>();
        repoMock.Setup(r => r.CreateDriverAsync(newDriver, It.IsAny<CancellationToken>())).ReturnsAsync(newDriver);
        var service = new DriverService(repoMock.Object);
        var result = await service.CreateDriverAsync(newDriver);

        Assert.Equal(newDriver, result);
        repoMock.Verify(r => r.CreateDriverAsync(newDriver, It.IsAny<CancellationToken>()), Times.Once);

    }
}

public class NotificationServiceTests
{
    [Fact]
    public async Task SendSmsAsync_CallsCommunicationService()
    {
        var cacheMock = new Mock<IDistributedCache>();
        var loggerMock = new Mock<ILogger<NotificationService>>();
        var factoryMock = new Mock<ICommunicationServiceFactory>();
        var commServiceMock = new Mock<ICommunicationService>();

        commServiceMock.Setup(s => s.SendAsync(It.IsAny<CommunicationMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        factoryMock.Setup(f => f.GetService("Sms")).Returns(commServiceMock.Object);

        var service = new NotificationService(cacheMock.Object, loggerMock.Object, factoryMock.Object);

        var result = await service.SendSmsAsync("+1234567890", "Test message");

        Assert.True(result);
        factoryMock.Verify(f => f.GetService("Sms"), Times.Once);
        commServiceMock.Verify(s => s.SendAsync(It.Is<CommunicationMessage>(m => m.To == "+1234567890" && m.Body == "Test message"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_CallsCommunicationService()
    {
        var cacheMock = new Mock<IDistributedCache>();
        var loggerMock = new Mock<ILogger<NotificationService>>();
        var factoryMock = new Mock<ICommunicationServiceFactory>();
        var commServiceMock = new Mock<ICommunicationService>();

        commServiceMock.Setup(s => s.SendAsync(It.IsAny<CommunicationMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        factoryMock.Setup(f => f.GetService("Email")).Returns(commServiceMock.Object);

        var service = new NotificationService(cacheMock.Object, loggerMock.Object, factoryMock.Object);

        var result = await service.SendEmailAsync("test@example.com", "Subject", "Test message");

        Assert.True(result);
        factoryMock.Verify(f => f.GetService("Email"), Times.Once);
        commServiceMock.Verify(s => s.SendAsync(It.Is<CommunicationMessage>(m => m.To == "test@example.com" && m.Subject == "Subject" && m.Body == "Test message"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendPushNotificationAsync_CallsCommunicationService()
    {
        var cacheMock = new Mock<IDistributedCache>();
        var loggerMock = new Mock<ILogger<NotificationService>>();
        var factoryMock = new Mock<ICommunicationServiceFactory>();
        var commServiceMock = new Mock<ICommunicationService>();

        commServiceMock.Setup(s => s.SendAsync(It.IsAny<CommunicationMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        factoryMock.Setup(f => f.GetService("Push")).Returns(commServiceMock.Object);

        var service = new NotificationService(cacheMock.Object, loggerMock.Object, factoryMock.Object);

        var result = await service.SendPushNotificationAsync("device123", "Title", "Test message");

        Assert.True(result);
        factoryMock.Verify(f => f.GetService("Push"), Times.Once);
        commServiceMock.Verify(s => s.SendAsync(It.Is<CommunicationMessage>(m => m.To == "device123" && m.Title == "Title" && m.Body == "Test message"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendSmsAsync_RetryOnFailure()
    {
        var cacheMock = new Mock<IDistributedCache>();
        var loggerMock = new Mock<ILogger<NotificationService>>();
        var factoryMock = new Mock<ICommunicationServiceFactory>();
        var commServiceMock = new Mock<ICommunicationService>();

        // Fail first two times, succeed on third
        commServiceMock.SetupSequence(s => s.SendAsync(It.IsAny<CommunicationMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false)
            .ReturnsAsync(false)
            .ReturnsAsync(true);
        factoryMock.Setup(f => f.GetService("Sms")).Returns(commServiceMock.Object);

        var service = new NotificationService(cacheMock.Object, loggerMock.Object, factoryMock.Object);

        var result = await service.SendSmsAsync("+1234567890", "Test message");

        Assert.True(result);
        commServiceMock.Verify(s => s.SendAsync(It.IsAny<CommunicationMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }
}

public class JobServiceTests
{
    [Fact]
    public async Task GetJobsAsync_ReturnsJobsFromRepository()
    {
var expected = new List<Job> { new HotshotLogistics.Domain.Entities.Job { Id = "1" }, new HotshotLogistics.Domain.Entities.Job { Id = "2" } };
        var repoMock = new Mock<JobRepository>();
        var customerRepoMock = new Mock<CustomerRepository>();
        var driverRepoMock = new Mock<DriverRepository>();
        var notificationServiceMock = new Mock<INotificationService>();
        var mappingServiceMock = new Mock<IMappingService>();
        var loggerMock = new Mock<ILogger<JobService>>();
        repoMock.Setup(r => r.GetJobsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var service = new JobService(repoMock.Object, customerRepoMock.Object, driverRepoMock.Object, notificationServiceMock.Object, mappingServiceMock.Object, loggerMock.Object);

        var result = await service.GetJobsAsync(CancellationToken.None);

        Assert.Equal(expected, result);
        repoMock.Verify(r => r.GetJobsAsync(It.IsAny<CancellationToken>()), Times.Once);

    }

    [Fact]
    public async Task GetJobByIdAsync_UsesRepository()
    {
var job = new HotshotLogistics.Domain.Entities.Job { Id = "1" };
        var repoMock = new Mock<JobRepository>();
        var customerRepoMock = new Mock<CustomerRepository>();
        var driverRepoMock = new Mock<DriverRepository>();
        var notificationServiceMock = new Mock<INotificationService>();
        var mappingServiceMock = new Mock<IMappingService>();
        var loggerMock = new Mock<ILogger<JobService>>();
        repoMock.Setup(r => r.GetJobByIdAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(job);
        var service = new JobService(repoMock.Object, customerRepoMock.Object, driverRepoMock.Object, notificationServiceMock.Object, mappingServiceMock.Object, loggerMock.Object);

        var result = await service.GetJobByIdAsync("1");

        Assert.Equal(job, result);
        repoMock.Verify(r => r.GetJobByIdAsync("1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateJobAsync_CallsRepository()
    {
var job = new HotshotLogistics.Domain.Entities.Job
        {
            Id = "1",
            CustomerId = "CUST001",
            Title = "Test Job",
            PickupLocation = new Location
            {
                Address = "123 Pickup St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Latitude = 40.7128m,
                Longitude = -74.0060m
            },
            DeliveryLocation = new Location
            {
                Address = "456 Delivery Ave",
                City = "New York",
                State = "NY",
                PostalCode = "10002",
                Latitude = 40.7589m,
                Longitude = -73.9851m
            },
            Cargo = new CargoDetails
            {
                Description = "Test cargo",
                Weight = 100,
                Value = 1000,
                Quantity = 1
            },
            Pricing = new PricingDetails
            {
                BaseRate = 100,
                MileageRate = 2.5m,
                FuelSurcharge = 10,
                TollCharges = 5,
                AdditionalCharges = 0,
                TotalAmount = 115,
                Discount = 0,
                Tax = 0,
                TaxRate = 0
            },
            ScheduledPickupTime = DateTime.UtcNow.AddHours(2),
            EstimatedDeliveryTime = DateTime.UtcNow.AddHours(8)
        };

        var customer = new HotshotLogistics.Domain.Entities.Customer { Id = "CUST001", IsActive = true };

        var repoMock = new Mock<JobRepository>();
        var customerRepoMock = new Mock<CustomerRepository>();
        var driverRepoMock = new Mock<DriverRepository>();
        var notificationServiceMock = new Mock<INotificationService>();
        var mappingServiceMock = new Mock<IMappingService>();
        var loggerMock = new Mock<ILogger<JobService>>();

        // Add this setup inside the test
        mappingServiceMock
            .Setup(m => m.ReverseGeocodeAsync(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReverseGeocodingResult { IsValid = true });

        repoMock.Setup(r => r.CreateJobAsync(job, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        customerRepoMock.Setup(r => r.GetByIdAsync("CUST001", It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var service = new JobService(repoMock.Object, customerRepoMock.Object, driverRepoMock.Object, notificationServiceMock.Object, mappingServiceMock.Object, loggerMock.Object);

        var result = await service.CreateJobAsync(job);

        Assert.Equal(job, result);
        repoMock.Verify(r => r.CreateJobAsync(job, It.IsAny<CancellationToken>()), Times.Once);

    }

    [Fact]
    public async Task UpdateJobAsync_CallsRepository()
    {
        var job = new HotshotLogistics.Domain.Entities.Job
        {
            Id = "1",
            CustomerId = "CUST001",
            Title = "Test Job",
            PickupLocation = new Location
            {
                Address = "123 Pickup St",
                City = "New York",
                State = "NY",
                PostalCode = "10001",
                Latitude = 40.7128m,
                Longitude = -74.0060m
            },
            DeliveryLocation = new Location
            {
                Address = "456 Delivery Ave",
                City = "New York",
                State = "NY",
                PostalCode = "10002",
                Latitude = 40.7589m,
                Longitude = -73.9851m
            },
            Cargo = new CargoDetails
            {
                Description = "Test cargo",
                Weight = 100,
                Value = 1000,
                Quantity = 1
            },
            Pricing = new PricingDetails
            {
                BaseRate = 100,
                MileageRate = 2.5m,
                FuelSurcharge = 10,
                TollCharges = 5,
                AdditionalCharges = 0,
                TotalAmount = 115,
                Discount = 0,
                Tax = 0,
                TaxRate = 0
            },
            ScheduledPickupTime = DateTime.UtcNow.AddHours(2),
            EstimatedDeliveryTime = DateTime.UtcNow.AddHours(8)
        };
        var repoMock = new Mock<JobRepository>();
        var customerRepoMock = new Mock<CustomerRepository>();
        var driverRepoMock = new Mock<DriverRepository>();
        var notificationServiceMock = new Mock<INotificationService>();
        var mappingServiceMock = new Mock<IMappingService>();
        var loggerMock = new Mock<ILogger<JobService>>();
        repoMock.Setup(r => r.GetJobByIdAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(job);
        repoMock.Setup(r => r.UpdateJobAsync("1", job, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        var service = new JobService(repoMock.Object, customerRepoMock.Object, driverRepoMock.Object, notificationServiceMock.Object, mappingServiceMock.Object, loggerMock.Object);

        var result = await service.UpdateJobAsync("1", job);

        Assert.Equal(job, result);
        repoMock.Verify(r => r.UpdateJobAsync("1", job, It.IsAny<CancellationToken>()), Times.Once);

    }

    [Fact]
    public async Task DeleteJobAsync_CallsRepository()
    {
        var job = new HotshotLogistics.Domain.Entities.Job { Id = "1", Status = JobStatus.Pending };
        var repoMock = new Mock<JobRepository>();
        var customerRepoMock = new Mock<CustomerRepository>();
        var driverRepoMock = new Mock<DriverRepository>();
        var notificationServiceMock = new Mock<INotificationService>();
        var mappingServiceMock = new Mock<IMappingService>();
        var loggerMock = new Mock<ILogger<JobService>>();
        repoMock.Setup(r => r.GetJobByIdAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(job);
        repoMock.Setup(r => r.DeleteJobAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = new JobService(repoMock.Object, customerRepoMock.Object, driverRepoMock.Object, notificationServiceMock.Object, mappingServiceMock.Object, loggerMock.Object);

        var result = await service.DeleteJobAsync("1", It.IsAny<CancellationToken>());

        Assert.True(result);
        repoMock.Verify(r => r.DeleteJobAsync("1", It.IsAny<CancellationToken>()), Times.Once);

    }
}
}
