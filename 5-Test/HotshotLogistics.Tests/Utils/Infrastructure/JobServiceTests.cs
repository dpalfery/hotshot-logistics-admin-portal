using HotshotLogistics.Application.Services;
using HotshotLogistics.Contracts.Repositories;
using HotshotLogistics.Contracts.Services;
using Moq;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Collections.Generic;
using Xunit;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;

namespace HotshotLogistics.Tests.Utils.Infrastructure
{
    public class JobServiceTests
    {
        [Fact]
        public async System.Threading.Tasks.Task GetJobsAsync_ReturnsJobsFromRepository()
        {
            var expected = new List<HotshotLogistics.Domain.Entities.Job> { new HotshotLogistics.Domain.Entities.Job { Id = "1" }, new HotshotLogistics.Domain.Entities.Job { Id = "2" } };
            var repoMock = new Mock<IJobRepository>();
            var customerRepoMock = new Mock<ICustomerRepository>();
            var driverRepoMock = new Mock<IDriverRepository>();
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
        public async System.Threading.Tasks.Task GetJobByIdAsync_UsesRepository()
        {
            var job = new HotshotLogistics.Domain.Entities.Job { Id = "1" };
            var repoMock = new Mock<IJobRepository>();
            var customerRepoMock = new Mock<ICustomerRepository>();
            var driverRepoMock = new Mock<IDriverRepository>();
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
        public async System.Threading.Tasks.Task CreateJobAsync_CallsRepository()
        {
            var job = new HotshotLogistics.Domain.Entities.Job
            {
                Id = "1",
                CustomerId = "CUST001",
                Title = "Test Job",
                PickupLocation = new HotshotLogistics.Domain.Entities.Location
                {
                    Address = "123 Pickup St",
                    City = "New York",
                    State = "NY",
                    PostalCode = "10001",
                    Latitude = 40.7128m,
                    Longitude = -74.0060m
                },
                DeliveryLocation = new HotshotLogistics.Domain.Entities.Location
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
                ScheduledPickupTime = System.DateTime.UtcNow.AddHours(2),
                EstimatedDeliveryTime = System.DateTime.UtcNow.AddHours(8)
            };

            var customer = new HotshotLogistics.Domain.Entities.Customer { Id = "CUST001", IsActive = true };

            var repoMock = new Mock<IJobRepository>();
            var customerRepoMock = new Mock<ICustomerRepository>();
            var driverRepoMock = new Mock<IDriverRepository>();
            var notificationServiceMock = new Mock<INotificationService>();
            var mappingServiceMock = new Mock<IMappingService>();
            var loggerMock = new Mock<ILogger<JobService>>();

            // Add this setup inside the test
            mappingServiceMock
                .Setup(m => m.ReverseGeocodeAsync(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HotshotLogistics.Domain.Entities.ReverseGeocodingResult { IsValid = true });

            repoMock.Setup(r => r.CreateJobAsync(job, It.IsAny<CancellationToken>())).ReturnsAsync(job);
            customerRepoMock.Setup(r => r.GetByIdAsync("CUST001", It.IsAny<CancellationToken>())).ReturnsAsync(customer);

            var service = new JobService(repoMock.Object, customerRepoMock.Object, driverRepoMock.Object, notificationServiceMock.Object, mappingServiceMock.Object, loggerMock.Object);

            var result = await service.CreateJobAsync(job);

            Assert.Equal(job, result);
            repoMock.Verify(r => r.CreateJobAsync(job, It.IsAny<CancellationToken>()), Times.Once);

        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateJobAsync_CallsRepository()
        {
            var job = new HotshotLogistics.Domain.Entities.Job
            {
                Id = "1",
                CustomerId = "CUST001",
                Title = "Test Job",
                PickupLocation = new HotshotLogistics.Domain.Entities.Location
                {
                    Address = "123 Pickup St",
                    City = "New York",
                    State = "NY",
                    PostalCode = "10001",
                    Latitude = 40.7128m,
                    Longitude = -74.0060m
                },
                DeliveryLocation = new HotshotLogistics.Domain.Entities.Location
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
                ScheduledPickupTime = System.DateTime.UtcNow.AddHours(2),
                EstimatedDeliveryTime = System.DateTime.UtcNow.AddHours(8)
            };
            var repoMock = new Mock<IJobRepository>();
            var customerRepoMock = new Mock<ICustomerRepository>();
            var driverRepoMock = new Mock<IDriverRepository>();
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
        public async System.Threading.Tasks.Task DeleteJobAsync_CallsRepository()
        {
            var job = new HotshotLogistics.Domain.Entities.Job { Id = "1", Status = HotshotLogistics.Core.Enums.JobStatus.Pending };
            var repoMock = new Mock<IJobRepository>();
            var customerRepoMock = new Mock<ICustomerRepository>();
            var driverRepoMock = new Mock<IDriverRepository>();
            var notificationServiceMock = new Mock<INotificationService>();
            var mappingServiceMock = new Mock<IMappingService>();
            var loggerMock = new Mock<ILogger<JobService>>();
            repoMock.Setup(r => r.GetJobByIdAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(job);
            repoMock.Setup(r => r.DeleteJobAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(true);
            var service = new JobService(repoMock.Object, customerRepoMock.Object, driverRepoMock.Object, notificationServiceMock.Object, mappingServiceMock.Object, loggerMock.Object);

            var result = await service.DeleteJobAsync("1", CancellationToken.None);

            Assert.True(result);
            repoMock.Verify(r => r.DeleteJobAsync("1", It.IsAny<CancellationToken>()), Times.Once);

        }
    }
}
