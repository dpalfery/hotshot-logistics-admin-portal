// <copyright file="TrackingControllerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Tests.Tracking
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using HotshotLogistics.Api.Controllers;
    using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.Entities;
    using HotshotLogistics.Contracts.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;

    /// <summary>
    /// Integration tests for the TrackingController.
    /// </summary>
    public class TrackingControllerTests
    {
        private readonly Mock<ITrackingService> mockTrackingService;
        private readonly Mock<ILogger<TrackingController>> mockLogger;
        private readonly TrackingController controller;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackingControllerTests"/> class.
        /// </summary>
        public TrackingControllerTests()
        {
            mockTrackingService = new Mock<ITrackingService>();
            mockLogger = new Mock<ILogger<TrackingController>>();
            controller = new TrackingController(mockTrackingService.Object, mockLogger.Object);
        }

        /// <summary>
        /// Tests that StartTracking starts tracking successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task StartTracking_WithValidRequest_StartsTrackingSuccessfully()
        {
            // Arrange
            var request = new StartTrackingRequest
            {
                JobId = "job-123",
                DriverId = 456
            };

            mockTrackingService.Setup(s => s.StartTrackingAsync(request.JobId, request.DriverId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await controller.StartTracking(request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var trackingResult = okResult.Value.Should().BeOfType<TrackingResult>().Subject;
            trackingResult.Success.Should().BeTrue();
            trackingResult.JobId.Should().Be(request.JobId);
            trackingResult.DriverId.Should().Be(request.DriverId);
        }

        /// <summary>
        /// Tests that StartTracking returns BadRequest for null request.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task StartTracking_WithNullRequest_ReturnsBadRequest()
        {
            // Act
            var result = await controller.StartTracking(null!);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Tests that StartTracking returns BadRequest for empty job ID.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task StartTracking_WithEmptyJobId_ReturnsBadRequest()
        {
            // Arrange
            var request = new StartTrackingRequest
            {
                JobId = "",
                DriverId = 456
            };

            // Act
            var result = await controller.StartTracking(request);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Tests that StartTracking returns BadRequest for invalid driver ID.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task StartTracking_WithInvalidDriverId_ReturnsBadRequest()
        {
            // Arrange
            var request = new StartTrackingRequest
            {
                JobId = "job-123",
                DriverId = 0
            };

            // Act
            var result = await controller.StartTracking(request);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Tests that StopTracking stops tracking successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task StopTracking_WithValidJobId_StopsTrackingSuccessfully()
        {
            // Arrange
            var jobId = "job-123";

            mockTrackingService.Setup(s => s.StopTrackingAsync(jobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await controller.StopTracking(jobId);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var trackingResult = okResult.Value.Should().BeOfType<TrackingResult>().Subject;
            trackingResult.Success.Should().BeTrue();
            trackingResult.JobId.Should().Be(jobId);
        }

        /// <summary>
        /// Tests that StopTracking returns BadRequest for empty job ID.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task StopTracking_WithEmptyJobId_ReturnsBadRequest()
        {
            // Act
            var result = await controller.StopTracking("");

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Tests that UpdateLocation updates location successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task UpdateLocation_WithValidRequest_UpdatesLocationSuccessfully()
        {
            // Arrange
            var request = new UpdateLocationRequest
            {
                JobId = "job-123",
                DriverId = 456,
                LocationUpdate = new LocationUpdate
                {
                    Latitude = 40.7128m,
                    Longitude = -74.0060m,
                    Timestamp = DateTime.UtcNow
                }
            };

            var expectedLocationTracking = CreateTestLocationTracking(request.JobId, request.DriverId);

            mockTrackingService.Setup(s => s.UpdateLocationAsync(
                request.JobId,
                request.DriverId,
                request.LocationUpdate,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedLocationTracking);

            // Act
            var result = await controller.UpdateLocation(request);

            // Assert
            result.Should().NotBeNull();
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var locationTracking = createdResult.Value.Should().BeAssignableTo<LocationTracking>().Subject;
            locationTracking.JobId.Should().Be(request.JobId);
        }

        /// <summary>
        /// Tests that UpdateLocation returns BadRequest for null request.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task UpdateLocation_WithNullRequest_ReturnsBadRequest()
        {
            // Act
            var result = await controller.UpdateLocation(null!);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Tests that GetCurrentLocation returns current location.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetCurrentLocation_WhenLocationExists_ReturnsCurrentLocation()
        {
            // Arrange
            var jobId = "job-123";
            var expectedLocation = CreateTestLocationTracking(jobId, 456);

            mockTrackingService.Setup(s => s.GetCurrentLocationAsync(jobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedLocation);

            // Act
            var result = await controller.GetCurrentLocation(jobId);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var locationTracking = okResult.Value.Should().BeAssignableTo<LocationTracking>().Subject;
            locationTracking.JobId.Should().Be(jobId);
        }

        /// <summary>
        /// Tests that GetCurrentLocation returns NotFound when no location exists.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetCurrentLocation_WhenLocationNotFound_ReturnsNotFound()
        {
            // Arrange
            var jobId = "job-123";

            mockTrackingService.Setup(s => s.GetCurrentLocationAsync(jobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((LocationTracking?)null);

            // Act
            var result = await controller.GetCurrentLocation(jobId);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        /// <summary>
        /// Tests that GetLocationHistory returns location history.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetLocationHistory_WithValidParameters_ReturnsLocationHistory()
        {
            // Arrange
            var jobId = "job-123";
            var startTime = DateTime.UtcNow.AddHours(-2);
            var endTime = DateTime.UtcNow;
            var expectedHistory = new List<LocationTracking>
            {
                CreateTestLocationTracking(jobId, 456),
                CreateTestLocationTracking(jobId, 456)
            };

            mockTrackingService.Setup(s => s.GetLocationHistoryAsync(jobId, startTime, endTime, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedHistory);

            // Act
            var result = await controller.GetLocationHistory(jobId, startTime, endTime);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var history = okResult.Value.Should().BeAssignableTo<IEnumerable<LocationTracking>>().Subject;
            history.Should().HaveCount(2);
        }

        /// <summary>
        /// Tests that GetLocationHistory returns BadRequest for invalid time range.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetLocationHistory_WithInvalidTimeRange_ReturnsBadRequest()
        {
            // Arrange
            var jobId = "job-123";
            var startTime = DateTime.UtcNow;
            var endTime = DateTime.UtcNow.AddHours(-1); // End time before start time

            // Act
            var result = await controller.GetLocationHistory(jobId, startTime, endTime);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Tests that CheckRouteDeviation checks deviation successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task CheckRouteDeviation_WithValidRequest_ChecksDeviationSuccessfully()
        {
            // Arrange
            var request = new RouteDeviationRequest
            {
                JobId = "job-123",
                CurrentLocation = new LocationUpdate
                {
                    Latitude = 40.7128m,
                    Longitude = -74.0060m,
                    Timestamp = DateTime.UtcNow
                }
            };

            mockTrackingService.Setup(s => s.CheckRouteDeviationAsync(
                request.JobId,
                request.CurrentLocation,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); // No deviation

            // Act
            var result = await controller.CheckRouteDeviation(request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var deviationResult = okResult.Value.Should().BeOfType<RouteDeviationResult>().Subject;
            deviationResult.JobId.Should().Be(request.JobId);
            deviationResult.HasDeviated.Should().BeFalse();
        }

        /// <summary>
        /// Tests that CheckRouteDeviation returns BadRequest for null request.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task CheckRouteDeviation_WithNullRequest_ReturnsBadRequest()
        {
            // Act
            var result = await controller.CheckRouteDeviation(null!);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Tests that GetPublicTrackingInfo returns public tracking information.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetPublicTrackingInfo_WhenLocationExists_ReturnsPublicTrackingInfo()
        {
            // Arrange
            var jobId = "job-123";
            var expectedLocation = CreateTestLocationTracking(jobId, 456);

            mockTrackingService.Setup(s => s.GetCurrentLocationAsync(jobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedLocation);

            // Act
            var result = await controller.GetPublicTrackingInfo(jobId);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var publicInfo = okResult.Value.Should().BeOfType<PublicTrackingInfo>().Subject;
            publicInfo.JobId.Should().Be(jobId);
            publicInfo.CurrentLatitude.Should().Be((double)expectedLocation.Latitude);
            publicInfo.CurrentLongitude.Should().Be((double)expectedLocation.Longitude);
        }

        /// <summary>
        /// Tests that GetPublicTrackingInfo returns NotFound when no location exists.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetPublicTrackingInfo_WhenLocationNotFound_ReturnsNotFound()
        {
            // Arrange
            var jobId = "job-123";

            mockTrackingService.Setup(s => s.GetCurrentLocationAsync(jobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((LocationTracking?)null);

            // Act
            var result = await controller.GetPublicTrackingInfo(jobId);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        /// <summary>
        /// Creates a test location tracking record for testing purposes.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <param name="driverId">The driver ID.</param>
        /// <returns>A test location tracking instance.</returns>
        private static LocationTracking CreateTestLocationTracking(string jobId, int driverId)
        {
            var mockLocationTracking = new Mock<LocationTracking>();
            mockLocationTracking.Setup(l => l.Id).Returns(1L);
            mockLocationTracking.Setup(l => l.JobId).Returns(jobId);
            mockLocationTracking.Setup(l => l.DriverId).Returns(driverId);
            mockLocationTracking.Setup(l => l.Latitude).Returns(40.7128m);
            mockLocationTracking.Setup(l => l.Longitude).Returns(-74.0060m);
            mockLocationTracking.Setup(l => l.Timestamp).Returns(DateTime.UtcNow);
            mockLocationTracking.Setup(l => l.Speed).Returns(55.0m);
            mockLocationTracking.Setup(l => l.Heading).Returns(90);
            return mockLocationTracking.Object;
        }
    }
}
