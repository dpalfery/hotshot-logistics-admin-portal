// <copyright file="TrackingService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Application.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using HotshotLogistics.Core.Enums;
    using HotshotLogistics.Domain.Entities;
    using HotshotLogistics.Contracts.Repositories;
    using HotshotLogistics.Contracts.Services;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Logging;
    using System.Text.Json;

    /// <summary>
    /// Service for location tracking operations.
    /// </summary>
    public class TrackingService : ITrackingService
    {
        private readonly ILocationTrackingRepository locationTrackingRepository;
        private readonly IJobRepository jobRepository;
        private readonly IDriverRepository driverRepository;
        private readonly INotificationService notificationService;
        private readonly IDistributedCache cache;
        private readonly ILogger<TrackingService> logger;

        // Route deviation threshold in miles
        private const double RouteDeviationThresholdMiles = 5.0;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackingService"/> class.
        /// </summary>
        /// <param name="locationTrackingRepository">The location tracking repository.</param>
        /// <param name="jobRepository">The job repository.</param>
        /// <param name="driverRepository">The driver repository.</param>
        /// <param name="notificationService">The notification service.</param>
        /// <param name="cache">The distributed cache.</param>
        /// <param name="logger">The logger.</param>
        public TrackingService(
            ILocationTrackingRepository locationTrackingRepository,
            IJobRepository jobRepository,
            IDriverRepository driverRepository,
            INotificationService notificationService,
            IDistributedCache cache,
            ILogger<TrackingService> logger)
        {
            this.locationTrackingRepository = locationTrackingRepository ?? throw new ArgumentNullException(nameof(locationTrackingRepository));
            this.jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            this.driverRepository = driverRepository ?? throw new ArgumentNullException(nameof(driverRepository));
            this.notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<bool> StartTrackingAsync(string jobId, int driverId, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Starting tracking for job {JobId} with driver {DriverId}", jobId, driverId);

            // Validate job exists and is assigned to the driver
            var job = await jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                logger.LogWarning("Job not found: {JobId}", jobId);
                return false;
            }

            if (job.AssignedDriverId != driverId)
            {
                logger.LogWarning("Driver {DriverId} is not assigned to job {JobId}", driverId, jobId);
                return false;
            }

            // Validate driver exists and is active
            var driver = await driverRepository.GetDriverByIdAsync(driverId);
            if (driver == null || !driver.IsActive)
            {
                logger.LogWarning("Driver not found or inactive: {DriverId}", driverId);
                return false;
            }

            // Set tracking status in cache
            var trackingKey = $"tracking:{jobId}";
            var trackingInfo = new
            {
                JobId = jobId,
                DriverId = driverId,
                StartTime = DateTime.UtcNow,
                IsActive = true
            };

            var trackingJson = JsonSerializer.Serialize(trackingInfo);
            await cache.SetStringAsync(trackingKey, trackingJson, new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(24) // Keep tracking active for 24 hours
            }, cancellationToken);

            logger.LogInformation("Tracking started successfully for job {JobId} with driver {DriverId}", jobId, driverId);
            return true;
        }

        /// <inheritdoc/>
        public async Task<LocationTracking> UpdateLocationAsync(string jobId, int driverId, LocationUpdate locationUpdate, CancellationToken cancellationToken = default)
        {
            logger.LogDebug("Updating location for job {JobId} with driver {DriverId}", jobId, driverId);

            // Validate location update
            if (!locationUpdate.IsValid())
            {
                throw new ArgumentException("Invalid location update data", nameof(locationUpdate));
            }

            // Check if tracking is active
            var trackingKey = $"tracking:{jobId}";
            var trackingJson = await cache.GetStringAsync(trackingKey, cancellationToken);
            if (string.IsNullOrEmpty(trackingJson))
            {
                throw new InvalidOperationException($"Tracking is not active for job {jobId}");
            }

            // Create location tracking record
            var locationTracking = LocationTracking.FromLocationUpdate(jobId, driverId, locationUpdate);
            var savedLocation = await locationTrackingRepository.AddAsync(locationTracking);

            // Update cache with latest location
            var cacheKey = $"location:current:{jobId}";
            var locationJson = JsonSerializer.Serialize(locationUpdate);
            await cache.SetStringAsync(cacheKey, locationJson, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) // Cache for 30 minutes
            }, cancellationToken);

            // Update job tracking information
            try
            {
                await UpdateJobTrackingInfoAsync(jobId, locationUpdate, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to update job tracking info for job {JobId}", jobId);
            }

            // Check for route deviation
            try
            {
                var hasDeviated = await CheckRouteDeviationAsync(jobId, locationUpdate, cancellationToken);
                if (hasDeviated)
                {
                    await HandleRouteDeviationAsync(jobId, driverId, locationUpdate, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to check route deviation for job {JobId}", jobId);
            }

            logger.LogDebug("Location updated successfully for job {JobId}", jobId);
            return savedLocation;
        }

        /// <inheritdoc/>
        public async Task<bool> StopTrackingAsync(string jobId, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Stopping tracking for job {JobId}", jobId);

            // Remove tracking status from cache
            var trackingKey = $"tracking:{jobId}";
            await cache.RemoveAsync(trackingKey, cancellationToken);

            // Remove current location from cache
            var locationKey = $"location:current:{jobId}";
            await cache.RemoveAsync(locationKey, cancellationToken);

            logger.LogInformation("Tracking stopped successfully for job {JobId}", jobId);
            return true;
        }

        /// <inheritdoc/>
        public async Task<LocationTracking?> GetCurrentLocationAsync(string jobId, CancellationToken cancellationToken = default)
        {
            // Try to get from cache first
            var cacheKey = $"location:current:{jobId}";
            var cachedLocationJson = await cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cachedLocationJson))
            {
                try
                {
                    var cachedLocation = JsonSerializer.Deserialize<LocationUpdate>(cachedLocationJson);
                    if (cachedLocation != null)
                    {
                        // Get driver ID from job
                        var job = await jobRepository.GetByIdAsync(jobId);
                        if (job?.AssignedDriverId.HasValue == true)
                        {
                            return LocationTracking.FromLocationUpdate(jobId, job.AssignedDriverId.Value, cachedLocation);
                        }
                    }
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex, "Failed to deserialize cached location for job {JobId}", jobId);
                }
            }

            // Fall back to database
            return await locationTrackingRepository.GetLatestByJobIdAsync(jobId);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<LocationTracking>> GetLocationHistoryAsync(string jobId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
        {
            return locationTrackingRepository.GetByJobIdAndTimeRangeAsync(jobId, startTime, endTime);
        }

        /// <inheritdoc/>
        public async Task<bool> CheckRouteDeviationAsync(string jobId, LocationUpdate currentLocation, CancellationToken cancellationToken = default)
        {
            try
            {
                var job = await jobRepository.GetByIdAsync(jobId);
                if (job == null)
                {
                    return false;
                }

                // Get expected route points (simplified - in real implementation, use mapping service)
                var expectedRoute = await GetExpectedRouteAsync(job.PickupLocation.FullAddress, job.DeliveryLocation.FullAddress);
                if (!expectedRoute.Any())
                {
                    return false;
                }

                // Find the closest point on the expected route
                var closestDistance = expectedRoute.Min(point => currentLocation.DistanceTo(point));

                // Check if current location is too far from the expected route
                return closestDistance > RouteDeviationThresholdMiles;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking route deviation for job {JobId}", jobId);
                return false;
            }
        }

        /// <summary>
        /// Gets the expected route between two addresses.
        /// </summary>
        /// <param name="fromAddress">The origin address.</param>
        /// <param name="toAddress">The destination address.</param>
        /// <returns>A list of location points representing the expected route.</returns>
        private async Task<List<LocationUpdate>> GetExpectedRouteAsync(string fromAddress, string toAddress)
        {
            // Simplified route calculation - in real implementation, use mapping service like Google Maps or Azure Maps
            // For demo purposes, return a straight line with a few waypoints

            await Task.Delay(100); // Simulate API call

            var route = new List<LocationUpdate>();

            // Generate some sample waypoints (in real implementation, get from mapping service)
            var random = new Random();
            var startLat = 40.7128m + (decimal)(random.NextDouble() - 0.5) * 0.1m; // Around NYC
            var startLon = -74.0060m + (decimal)(random.NextDouble() - 0.5) * 0.1m;
            var endLat = startLat + (decimal)(random.NextDouble() - 0.5) * 0.5m;
            var endLon = startLon + (decimal)(random.NextDouble() - 0.5) * 0.5m;

            // Create waypoints along the route
            for (int i = 0; i <= 10; i++)
            {
                var progress = i / 10.0m;
                var lat = startLat + (endLat - startLat) * progress;
                var lon = startLon + (endLon - startLon) * progress;

                route.Add(new LocationUpdate(lat, lon));
            }

            return route;
        }

        /// <summary>
        /// Generates a public tracking link for a job.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The tracking link URL.</returns>
        public async Task<string> GenerateTrackingLinkAsync(string jobId, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Generating tracking link for job {JobId}", jobId);

            var job = await jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                throw new ArgumentException($"Job {jobId} not found", nameof(jobId));
            }

            // Generate a secure tracking token (in real implementation, use proper token generation)
            var trackingToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{jobId}:{DateTime.UtcNow.Ticks}"));

            // Store tracking token in cache with expiration
            var tokenKey = $"tracking:token:{trackingToken}";
            await cache.SetStringAsync(tokenKey, jobId, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7) // Token valid for 7 days
            }, cancellationToken);

            // Generate tracking URL (in real implementation, use proper base URL from configuration)
            var trackingUrl = $"https://tracking.hotshotlogistics.com/track/{trackingToken}";

            logger.LogInformation("Tracking link generated for job {JobId}: {TrackingUrl}", jobId, trackingUrl);
            return trackingUrl;
        }

        /// <summary>
        /// Updates the job's tracking information with the latest location data.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="locationUpdate">The location update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpdateJobTrackingInfoAsync(string jobId, LocationUpdate locationUpdate, CancellationToken cancellationToken = default)
        {
            logger.LogDebug("Updating job tracking info for job {JobId}", jobId);

            var job = await jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                logger.LogWarning("Job not found for tracking update: {JobId}", jobId);
                return;
            }

            // Update job's tracking information
            if (job.Tracking == null)
            {
                job.Tracking = new TrackingInfo();
            }

            job.Tracking.AddUpdate(locationUpdate);
            job.Tracking.LastUpdateTime = locationUpdate.Timestamp;
            job.Tracking.IsActive = true;

            // Update estimated arrival time based on current location and speed
            if (locationUpdate.Speed.HasValue && locationUpdate.Speed > 0)
            {
                var targetLocation = job.Status == JobStatus.Assigned || job.Status == JobStatus.EnRoute
                    ? job.PickupLocation
                    : job.DeliveryLocation;

                var remainingDistance = locationUpdate.DistanceTo(targetLocation);
                if (remainingDistance.HasValue)
                {
                    var remainingTimeHours = (double)remainingDistance.Value / (double)locationUpdate.Speed.Value;
                    job.Tracking.EstimatedArrival = DateTime.UtcNow.AddHours(remainingTimeHours);
                }
            }

            // Update job status based on location proximity
            await UpdateJobStatusBasedOnLocationAsync(job, locationUpdate, cancellationToken);

            // Save updated job
            job.UpdatedAt = DateTime.UtcNow;
            await jobRepository.UpdateAsync(job);

            logger.LogDebug("Job tracking info updated for job {JobId}", jobId);
        }

        /// <summary>
        /// Updates job status based on current location proximity to pickup/delivery locations.
        /// </summary>
        /// <param name="job">The job to update.</param>
        /// <param name="currentLocation">The current location.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task UpdateJobStatusBasedOnLocationAsync(Job job, LocationUpdate currentLocation, CancellationToken cancellationToken)
        {
            const double proximityThresholdMiles = 0.5; // Within 0.5 miles

            var distanceToPickup = currentLocation.DistanceTo(job.PickupLocation);
            var distanceToDelivery = currentLocation.DistanceTo(job.DeliveryLocation);

            var statusChanged = false;

            // Check if driver has arrived at pickup or delivery location
            if (job.Status == JobStatus.EnRoute &&
                distanceToPickup.HasValue &&
                distanceToPickup.Value <= proximityThresholdMiles)
            {
                job.Tracking.CurrentStatus = "Arrived at pickup location";
                statusChanged = true;
            }
            // Check if driver has arrived at delivery location
            else if (job.Status == JobStatus.EnRoute &&
                     distanceToDelivery.HasValue &&
                     distanceToDelivery.Value <= proximityThresholdMiles)
            {
                // Don't automatically mark as received - wait for driver confirmation
                job.Tracking.CurrentStatus = "Arrived at delivery location";
                statusChanged = true;
            }

            // Send notifications if status changed
            if (statusChanged)
            {
                try
                {
                    await notificationService.SendNotificationAsync(
                        job.CustomerId,
                        NotificationType.JobStatusUpdate,
                        "Job Status Update",
                        $"Your job '{job.Title}' status has been updated to {job.Status}",
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send status update notification for job {JobId}", job.Id);
                }
            }
        }

        /// <summary>
        /// Gets tracking statistics for a job.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The tracking statistics.</returns>
        public async Task<TrackingStatistics> GetTrackingStatisticsAsync(string jobId, CancellationToken cancellationToken = default)
        {
            logger.LogDebug("Getting tracking statistics for job {JobId}", jobId);

            var job = await jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                throw new ArgumentException($"Job {jobId} not found", nameof(jobId));
            }

            var locationHistory = await locationTrackingRepository.GetByJobIdAsync(jobId);
            var locations = locationHistory.ToList();

            var statistics = new TrackingStatistics
            {
                JobId = jobId,
                TotalUpdates = locations.Count,
                FirstUpdate = locations.OrderBy(l => l.Timestamp).FirstOrDefault()?.Timestamp,
                LastUpdate = locations.OrderByDescending(l => l.Timestamp).FirstOrDefault()?.Timestamp,
                TotalDistance = CalculateTotalDistance(locations),
                AverageSpeed = CalculateAverageSpeed(locations),
                MaxSpeed = locations.Where(l => l.Speed.HasValue).Max(l => l.Speed),
                IsActive = job.Tracking?.IsActive ?? false
            };

            if (statistics.FirstUpdate.HasValue && statistics.LastUpdate.HasValue)
            {
                statistics.TotalDuration = statistics.LastUpdate.Value - statistics.FirstUpdate.Value;
            }

            logger.LogDebug("Tracking statistics calculated for job {JobId}: {TotalUpdates} updates, {TotalDistance:F2} miles",
                jobId, statistics.TotalUpdates, statistics.TotalDistance);

            return statistics;
        }

        /// <summary>
        /// Calculates the total distance traveled based on location history.
        /// </summary>
        /// <param name="locations">The location tracking records.</param>
        /// <returns>The total distance in miles.</returns>
        private static decimal CalculateTotalDistance(List<LocationTracking> locations)
        {
            if (locations.Count < 2)
                return 0;

            var orderedLocations = locations.OrderBy(l => l.Timestamp).ToList();
            decimal totalDistance = 0;

            for (int i = 1; i < orderedLocations.Count; i++)
            {
                var prev = orderedLocations[i - 1];
                var current = orderedLocations[i];

                if (prev is LocationTracking prevTracking && current is LocationTracking currentTracking)
                {
                    totalDistance += (decimal)prevTracking.DistanceTo(currentTracking);
                }
            }

            return totalDistance;
        }

        /// <summary>
        /// Calculates the average speed based on location history.
        /// </summary>
        /// <param name="locations">The location tracking records.</param>
        /// <returns>The average speed in mph.</returns>
        private static decimal? CalculateAverageSpeed(List<LocationTracking> locations)
        {
            var locationsWithSpeed = locations.Where(l => l.Speed.HasValue).ToList();

            if (!locationsWithSpeed.Any())
                return null;

            return locationsWithSpeed.Average(l => l.Speed!.Value);
        }

        /// <summary>
        /// Handles route deviation by sending notifications.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="driverId">The driver identifier.</param>
        /// <param name="currentLocation">The current location.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleRouteDeviationAsync(string jobId, int driverId, LocationUpdate currentLocation, CancellationToken cancellationToken)
        {
            logger.LogWarning("Route deviation detected for job {JobId} at location {Lat}, {Lon}",
                jobId, currentLocation.Latitude, currentLocation.Longitude);

            var job = await jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                return;
            }

            // Send notification to customer
            try
            {
                await notificationService.SendNotificationAsync(
                    job.CustomerId,
                    NotificationType.RouteDeviation,
                    "Route Deviation Alert",
                    $"Driver for job {job.Title} has deviated from the expected route",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send route deviation notification to customer for job {JobId}", jobId);
            }

            // Send notification to driver
            try
            {
                await notificationService.SendNotificationAsync(
                    driverId.ToString(),
                    NotificationType.RouteDeviation,
                    "Route Deviation Alert",
                    "You have deviated from the expected route. Please check your navigation.",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send route deviation notification to driver {DriverId}", driverId);
            }
        }
    }
}
