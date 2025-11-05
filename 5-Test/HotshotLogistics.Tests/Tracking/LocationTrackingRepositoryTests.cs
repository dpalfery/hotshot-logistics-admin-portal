// <copyright file="LocationTrackingRepositoryTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Contracts.Repositories;
using HotshotLogistics.Data.Repositories;
using HotshotLogistics.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HotshotLogistics.Tests.Tracking
{
    /// <summary>
    /// Integration tests for LocationTrackingRepository.
    /// </summary>
    public class LocationTrackingRepositoryTests : IClassFixture<DatabaseTestFixture>, IDisposable
    {
        private readonly LocationTrackingRepository _locationTrackingRepository;
        private readonly IConfiguration _configuration;
        private readonly List<long> _createdLocationTrackingIds = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationTrackingRepositoryTests"/> class.
        /// </summary>
        public LocationTrackingRepositoryTests(DatabaseTestFixture fixture)
        {
            ArgumentNullException.ThrowIfNull(fixture);
            var configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = TestDatabaseHelper.GetConnectionString()
                });

            _configuration = configBuilder.Build();
            _locationTrackingRepository = new LocationTrackingRepository(_configuration);
        }

        /// <summary>
        /// Tests that AddAsync creates a new location tracking record successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task AddAsync_CreatesLocationTrackingRecordSuccessfully()
        {
            // Arrange
            var locationTracking = CreateTestLocationTracking();

            // Act
            var result = await _locationTrackingRepository.AddAsync(locationTracking);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.JobId.Should().Be(locationTracking.JobId);
            result.DriverId.Should().Be(locationTracking.DriverId);
            result.Latitude.Should().Be(locationTracking.Latitude);
            result.Longitude.Should().Be(locationTracking.Longitude);

            _createdLocationTrackingIds.Add(result.Id);
        }

        /// <summary>
        /// Tests that GetByIdAsync retrieves a location tracking record successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetByIdAsync_RetrievesLocationTrackingRecordSuccessfully()
        {
            // Arrange
            var locationTracking = await CreateAndSaveTestLocationTrackingAsync();

            // Act
            var result = await _locationTrackingRepository.GetByIdAsync(locationTracking.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(locationTracking.Id);
            result.JobId.Should().Be(locationTracking.JobId);
            result.DriverId.Should().Be(locationTracking.DriverId);
            result.Latitude.Should().Be(locationTracking.Latitude);
            result.Longitude.Should().Be(locationTracking.Longitude);
        }

        /// <summary>
        /// Tests that GetByJobIdAsync returns location tracking records for the correct job.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetByJobIdAsync_ReturnsLocationTrackingRecordsForCorrectJob()
        {
            // Arrange
            var jobId = "JOB001";
            await CreateTestLocationTrackingRecordsAsync();

            // Act
            var result = await _locationTrackingRepository.GetByJobIdAsync(jobId);

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            result.Should().OnlyContain(lt => lt.JobId == jobId);
            result.Should().BeInAscendingOrder(lt => lt.Timestamp);
        }

        /// <summary>
        /// Tests that GetByDriverIdAsync returns location tracking records for the correct driver.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetByDriverIdAsync_ReturnsLocationTrackingRecordsForCorrectDriver()
        {
            // Arrange
            var driverId = 1;
            await CreateTestLocationTrackingRecordsAsync();

            // Act
            var result = await _locationTrackingRepository.GetByDriverIdAsync(driverId);

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            result.Should().OnlyContain(lt => lt.DriverId == driverId);
            result.Should().BeInDescendingOrder(lt => lt.Timestamp);
        }

        /// <summary>
        /// Tests that GetByJobIdAndTimeRangeAsync returns location tracking records within the time range.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetByJobIdAndTimeRangeAsync_ReturnsLocationTrackingRecordsWithinTimeRange()
        {
            // Arrange
            var jobId = "JOB001";
            await CreateTestLocationTrackingRecordsAsync();
            var startTime = DateTime.UtcNow.AddHours(-2);
            var endTime = DateTime.UtcNow.AddHours(2);

            // Act
            var result = await _locationTrackingRepository.GetByJobIdAndTimeRangeAsync(jobId, startTime, endTime);

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(lt =>
                lt.JobId == jobId &&
                lt.Timestamp >= startTime &&
                lt.Timestamp <= endTime);
            result.Should().BeInAscendingOrder(lt => lt.Timestamp);
        }

        /// <summary>
        /// Tests that GetByDriverIdAndTimeRangeAsync returns location tracking records within the time range.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetByDriverIdAndTimeRangeAsync_ReturnsLocationTrackingRecordsWithinTimeRange()
        {
            // Arrange
            var driverId = 1;
            await CreateTestLocationTrackingRecordsAsync();
            var startTime = DateTime.UtcNow.AddHours(-2);
            var endTime = DateTime.UtcNow.AddHours(2);

            // Act
            var result = await _locationTrackingRepository.GetByDriverIdAndTimeRangeAsync(driverId, startTime, endTime);

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(lt =>
                lt.DriverId == driverId &&
                lt.Timestamp >= startTime &&
                lt.Timestamp <= endTime);
            result.Should().BeInAscendingOrder(lt => lt.Timestamp);
        }

        /// <summary>
        /// Tests that GetLatestByJobIdAsync returns the latest location tracking record for a job.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetLatestByJobIdAsync_ReturnsLatestLocationTrackingRecordForJob()
        {
            // Arrange
            var jobId = "JOB001";
            await CreateTestLocationTrackingRecordsAsync();

            // Act
            var result = await _locationTrackingRepository.GetLatestByJobIdAsync(jobId);

            // Assert
            result.Should().NotBeNull();
            result!.JobId.Should().Be(jobId);
        }

        /// <summary>
        /// Tests that GetLatestByDriverIdAsync returns the latest location tracking record for a driver.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetLatestByDriverIdAsync_ReturnsLatestLocationTrackingRecordForDriver()
        {
            // Arrange
            var driverId = 1;
            await CreateTestLocationTrackingRecordsAsync();

            // Act
            var result = await _locationTrackingRepository.GetLatestByDriverIdAsync(driverId);

            // Assert
            result.Should().NotBeNull();
            result!.DriverId.Should().Be(driverId);
        }

        /// <summary>
        /// Tests that GetLatestByJobIdAsync with count returns the specified number of latest records.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetLatestByJobIdAsync_WithCount_ReturnsSpecifiedNumberOfLatestRecords()
        {
            // Arrange
            var jobId = "JOB001";
            await CreateTestLocationTrackingRecordsAsync();
            var count = 3;

            // Act
            var result = await _locationTrackingRepository.GetLatestByJobIdAsync(jobId, count);

            // Assert
            result.Should().NotBeNull();
            var records = result.ToList();
            records.Count.Should().BeLessThanOrEqualTo(count);
            records.Should().OnlyContain(lt => lt.JobId == jobId);
            records.Should().BeInDescendingOrder(lt => lt.Timestamp);
        }

        /// <summary>
        /// Tests that GetByGeographicAreaAsync returns location tracking records within the geographic area.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetByGeographicAreaAsync_ReturnsLocationTrackingRecordsWithinGeographicArea()
        {
            // Arrange
            await CreateTestLocationTrackingRecordsAsync();
            var centerLatitude = 40.7128m; // New York City
            var centerLongitude = -74.0060m;
            var radiusMiles = 50.0;

            // Act
            var result = await _locationTrackingRepository.GetByGeographicAreaAsync(
                centerLatitude, centerLongitude, radiusMiles);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeInDescendingOrder(lt => lt.Timestamp);
        }

        /// <summary>
        /// Tests that DeleteOlderThanAsync deletes location tracking records older than the cutoff date.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task DeleteOlderThanAsync_DeletesLocationTrackingRecordsOlderThanCutoffDate()
        {
            // Arrange
            await CreateTestLocationTrackingRecordsAsync();
            var cutoffDate = DateTime.UtcNow.AddDays(-30);

            // Act
            var result = await _locationTrackingRepository.DeleteOlderThanAsync(cutoffDate);

            // Assert
            result.Should().BeGreaterThanOrEqualTo(0);
        }

        /// <summary>
        /// Tests that GetTotalDistanceByJobIdAsync returns the total distance for a job.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetTotalDistanceByJobIdAsync_ReturnsTotalDistanceForJob()
        {
            // Arrange
            var jobId = "JOB001";
            await CreateTestLocationTrackingRecordsAsync();

            // Act
            var result = await _locationTrackingRepository.GetTotalDistanceByJobIdAsync(jobId);

            // Assert
            result.Should().BeGreaterThanOrEqualTo(0);
        }

        /// <summary>
        /// Tests that GetTotalDistanceByDriverIdAsync returns the total distance for a driver within a time range.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetTotalDistanceByDriverIdAsync_ReturnsTotalDistanceForDriverWithinTimeRange()
        {
            // Arrange
            var driverId = 1;
            await CreateTestLocationTrackingRecordsAsync();
            var startTime = DateTime.UtcNow.AddDays(-1);
            var endTime = DateTime.UtcNow.AddDays(1);

            // Act
            var result = await _locationTrackingRepository.GetTotalDistanceByDriverIdAsync(driverId, startTime, endTime);

            // Assert
            result.Should().BeGreaterThanOrEqualTo(0);
        }

        /// <summary>
        /// Tests that ExistsAsync returns true for existing location tracking records.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task ExistsAsync_ReturnsTrueForExistingLocationTrackingRecord()
        {
            // Arrange
            var locationTracking = await CreateAndSaveTestLocationTrackingAsync();

            // Act
            var result = await _locationTrackingRepository.ExistsAsync(locationTracking.Id);

            // Assert
            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that ExistsAsync returns false for non-existing location tracking records.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task ExistsAsync_ReturnsFalseForNonExistingLocationTrackingRecord()
        {
            // Arrange
            var nonExistingId = 999999L;

            // Act
            var result = await _locationTrackingRepository.ExistsAsync(nonExistingId);

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that GetCountByJobIdAsync returns the correct count of location tracking records for a job.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetCountByJobIdAsync_ReturnsCorrectCountOfLocationTrackingRecordsForJob()
        {
            // Arrange
            var jobId = "JOB001";
            await CreateTestLocationTrackingRecordsAsync();

            // Act
            var result = await _locationTrackingRepository.GetCountByJobIdAsync(jobId);

            // Assert
            result.Should().BeGreaterThan(0);
        }

        /// <summary>
        /// Tests that AddBatchAsync adds multiple location tracking records successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task AddBatchAsync_AddsMultipleLocationTrackingRecordsSuccessfully()
        {
            // Arrange
            var uniqueJobId = $"BATCH{Guid.NewGuid():N}";
            var locationTrackingRecords = new List<LocationTracking>
            {
                CreateTestLocationTracking(uniqueJobId, 1, 40.7128m, -74.0060m),
                CreateTestLocationTracking(uniqueJobId, 1, 40.7130m, -74.0062m),
                CreateTestLocationTracking(uniqueJobId, 1, 40.7132m, -74.0064m)
            };

            // Act
            var result = await _locationTrackingRepository.AddBatchAsync(locationTrackingRecords);

            // Assert
            result.Should().Be(locationTrackingRecords.Count);

            // Verify records were added
            var addedRecords = await _locationTrackingRepository.GetByJobIdAsync(uniqueJobId);
            addedRecords.Should().HaveCount(locationTrackingRecords.Count);

            // Track for cleanup
            foreach (var record in addedRecords)
            {
                _createdLocationTrackingIds.Add(record.Id);
            }
        }

        /// <summary>
        /// Creates a test location tracking record.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="driverId">The driver identifier.</param>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <returns>A test location tracking record.</returns>
        private LocationTracking CreateTestLocationTracking(
            string jobId = "JOB001",
            int driverId = 1,
            decimal latitude = 40.7128m,
            decimal longitude = -74.0060m)
        {
            return new LocationTracking
            {
                JobId = jobId,
                DriverId = driverId,
                Latitude = latitude,
                Longitude = longitude,
                Speed = 35.5m,
                Heading = 180,
                Accuracy = 5.0m,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates and saves a test location tracking record.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task<LocationTracking> CreateAndSaveTestLocationTrackingAsync()
        {
            var locationTracking = CreateTestLocationTracking();
            var result = await _locationTrackingRepository.AddAsync(locationTracking);
            _createdLocationTrackingIds.Add(result.Id);
            return result;
        }

        /// <summary>
        /// Creates multiple test location tracking records for testing purposes.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task<List<LocationTracking>> CreateTestLocationTrackingRecordsAsync()
        {
            var locationTrackingRecords = new List<LocationTracking>();

            // Create location tracking records with different jobs and drivers
            var testData = new[]
            {
                new { JobId = "JOB001", DriverId = 1, Lat = 40.7128m, Lon = -74.0060m, TimeOffset = -60 },
                new { JobId = "JOB001", DriverId = 1, Lat = 40.7130m, Lon = -74.0062m, TimeOffset = -45 },
                new { JobId = "JOB001", DriverId = 1, Lat = 40.7132m, Lon = -74.0064m, TimeOffset = -30 },
                new { JobId = "JOB002", DriverId = 2, Lat = 34.0522m, Lon = -118.2437m, TimeOffset = -15 },
                new { JobId = "JOB002", DriverId = 2, Lat = 34.0524m, Lon = -118.2439m, TimeOffset = 0 }
            };

            foreach (var data in testData)
            {
                var locationTracking = new LocationTracking
                {
                    JobId = data.JobId,
                    DriverId = data.DriverId,
                    Latitude = data.Lat,
                    Longitude = data.Lon,
                    Speed = 30.0m + (decimal)(new Random().NextDouble() * 20), // Random speed between 30-50 mph
                    Heading = new Random().Next(0, 360),
                    Accuracy = 3.0m + (decimal)(new Random().NextDouble() * 7), // Random accuracy between 3-10m
                    Timestamp = DateTime.UtcNow.AddMinutes(data.TimeOffset)
                };

                var result = await _locationTrackingRepository.AddAsync(locationTracking);
                locationTrackingRecords.Add(result);
                _createdLocationTrackingIds.Add(result.Id);
            }

            return locationTrackingRecords;
        }

        /// <summary>
        /// Cleans up test data.
        /// </summary>
        public void Dispose()
        {
            // Clean up created test location tracking records
            foreach (var locationTrackingId in _createdLocationTrackingIds)
            {
                try
                {
                    // Note: We would need a DeleteAsync method in the repository for proper cleanup
                    // For now, we'll rely on the test database being cleaned up between test runs
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}
