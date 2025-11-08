// <copyright file="TrackingController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Api.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using HotshotLogistics.Contracts.Services;
    using HotshotLogistics.Domain.Entities;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// API controller for location tracking services.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TrackingController : ControllerBase
    {
        private readonly ITrackingService trackingService;
        private readonly ILogger<TrackingController> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackingController"/> class.
        /// </summary>
        /// <param name="trackingService">The tracking service.</param>
        /// <param name="logger">The logger.</param>
        public TrackingController(
            ITrackingService trackingService,
            ILogger<TrackingController> logger)
        {
            this.trackingService = trackingService ?? throw new ArgumentNullException(nameof(trackingService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Starts tracking for a job and driver.
        /// </summary>
        /// <param name="request">The start tracking request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Success status of starting tracking.</returns>
        [HttpPost("start")]
        [ProducesResponseType(typeof(TrackingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TrackingResult>> StartTracking(
            [FromBody] StartTrackingRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Start tracking request is required");
                }

                if (string.IsNullOrWhiteSpace(request.JobId))
                {
                    return BadRequest("Job ID is required");
                }

                if (request.DriverId <= 0)
                {
                    return BadRequest("Valid driver ID is required");
                }

                var success = await trackingService.StartTrackingAsync(request.JobId, request.DriverId, cancellationToken);

                var result = new TrackingResult
                {
                    Success = success,
                    JobId = request.JobId,
                    DriverId = request.DriverId,
                    Message = success ? "Tracking started successfully" : "Failed to start tracking",
                    Timestamp = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid start tracking request: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Job or driver not found: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while starting tracking for job");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Stops tracking for a job.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Success status of stopping tracking.</returns>
        [HttpPost("stop/{jobId}")]
        [ProducesResponseType(typeof(TrackingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TrackingResult>> StopTracking(string jobId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jobId))
                {
                    return BadRequest("Job ID is required");
                }

                var success = await trackingService.StopTrackingAsync(jobId, cancellationToken);

                var result = new TrackingResult
                {
                    Success = success,
                    JobId = jobId,
                    Message = success ? "Tracking stopped successfully" : "Failed to stop tracking",
                    Timestamp = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid stop tracking request for job: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Job not found for stop tracking");
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while stopping tracking for job");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Updates the location for a job and driver.
        /// </summary>
        /// <param name="request">The location update request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created location tracking record.</returns>
        [HttpPost("location")]
        [ProducesResponseType(typeof(LocationTracking), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LocationTracking>> UpdateLocation(
            [FromBody] UpdateLocationRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Location update request is required");
                }

                if (string.IsNullOrWhiteSpace(request.JobId))
                {
                    return BadRequest("Job ID is required");
                }

                if (request.DriverId <= 0)
                {
                    return BadRequest("Valid driver ID is required");
                }

                if (request.LocationUpdate == null)
                {
                    return BadRequest("Location update data is required");
                }

                var locationTracking = await trackingService.UpdateLocationAsync(
                    request.JobId,
                    request.DriverId,
                    request.LocationUpdate,
                    cancellationToken);

                return CreatedAtAction(
                    nameof(GetCurrentLocation),
                    new { jobId = request.JobId },
                    locationTracking);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid location update request: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Job or driver not found for location update: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while updating location for job");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Gets the current location for a job.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The latest location tracking record.</returns>
        [HttpGet("location/{jobId}")]
        [ProducesResponseType(typeof(LocationTracking), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LocationTracking>> GetCurrentLocation(string jobId, CancellationToken cancellationToken = default)
        {
            try
            {
                var locationTracking = await trackingService.GetCurrentLocationAsync(jobId, cancellationToken);
                if (locationTracking == null)
                {
                    return NotFound($"No location tracking found for job {jobId}");
                }

                return Ok(locationTracking);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving current location for job");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Gets the location history for a job.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <param name="startTime">The start time for the history.</param>
        /// <param name="endTime">The end time for the history.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The location tracking history.</returns>
        [HttpGet("history/{jobId}")]
        [ProducesResponseType(typeof(IEnumerable<LocationTracking>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<LocationTracking>>> GetLocationHistory(
            string jobId,
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jobId))
                {
                    return BadRequest("Job ID is required");
                }

                // Default to last 24 hours if no time range specified
                var start = startTime ?? DateTime.UtcNow.AddDays(-1);
                var end = endTime ?? DateTime.UtcNow;

                if (start >= end)
                {
                    return BadRequest("Start time must be before end time");
                }

                var locationHistory = await trackingService.GetLocationHistoryAsync(jobId, start, end, cancellationToken);
                return Ok(locationHistory);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving location history for job");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Checks if a driver has deviated from the expected route.
        /// </summary>
        /// <param name="request">The route deviation check request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Route deviation status.</returns>
        [HttpPost("check-deviation")]
        [ProducesResponseType(typeof(RouteDeviationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RouteDeviationResult>> CheckRouteDeviation(
            [FromBody] RouteDeviationRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Route deviation request is required");
                }

                if (string.IsNullOrWhiteSpace(request.JobId))
                {
                    return BadRequest("Job ID is required");
                }

                if (request.CurrentLocation == null)
                {
                    return BadRequest("Current location is required");
                }

                var hasDeviated = await trackingService.CheckRouteDeviationAsync(
                    request.JobId,
                    request.CurrentLocation,
                    cancellationToken);

                var result = new RouteDeviationResult
                {
                    JobId = request.JobId,
                    HasDeviated = hasDeviated,
                    CurrentLocation = request.CurrentLocation,
                    CheckedAt = DateTime.UtcNow,
                    Message = hasDeviated ? "Driver has deviated from expected route" : "Driver is on expected route"
                };

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid route deviation request: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Job not found for route deviation check: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while checking route deviation for job");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Gets a public tracking link for a job (for customer access).
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Public tracking information.</returns>
        [HttpGet("public/{jobId}")]
        [ProducesResponseType(typeof(PublicTrackingInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PublicTrackingInfo>> GetPublicTrackingInfo(string jobId, CancellationToken cancellationToken = default)
        {
            try
            {
                var currentLocation = await trackingService.GetCurrentLocationAsync(jobId, cancellationToken);
                if (currentLocation == null)
                {
                    return NotFound($"No tracking information available for job {jobId}");
                }

                var publicInfo = new PublicTrackingInfo
                {
                    JobId = jobId,
                    CurrentLatitude = (double)currentLocation.Latitude,
                    CurrentLongitude = (double)currentLocation.Longitude,
                    LastUpdated = currentLocation.Timestamp,
                    Status = "In Transit", // This could be derived from job status
                    EstimatedArrival = currentLocation.Timestamp.AddHours(2) // Placeholder calculation
                };

                return Ok(publicInfo);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving public tracking info for job");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }
    }

    /// <summary>
    /// Request model for starting tracking.
    /// </summary>
    public class StartTrackingRequest
    {
        /// <summary>
        /// Gets or sets the job ID.
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the driver ID.
        /// </summary>
        public int DriverId { get; set; }
    }

    /// <summary>
    /// Request model for updating location.
    /// </summary>
    public class UpdateLocationRequest
    {
        /// <summary>
        /// Gets or sets the job ID.
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the driver ID.
        /// </summary>
        public int DriverId { get; set; }

        /// <summary>
        /// Gets or sets the location update data.
        /// </summary>
        public LocationUpdate LocationUpdate { get; set; } = new LocationUpdate();
    }

    /// <summary>
    /// Request model for checking route deviation.
    /// </summary>
    public class RouteDeviationRequest
    {
        /// <summary>
        /// Gets or sets the job ID.
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current location.
        /// </summary>
        public LocationUpdate CurrentLocation { get; set; } = new LocationUpdate();
    }

    /// <summary>
    /// Result model for tracking operations.
    /// </summary>
    public class TrackingResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the job ID.
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the driver ID.
        /// </summary>
        public int? DriverId { get; set; }

        /// <summary>
        /// Gets or sets the result message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the operation timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Result model for route deviation checks.
    /// </summary>
    public class RouteDeviationResult
    {
        /// <summary>
        /// Gets or sets the job ID.
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the driver has deviated from route.
        /// </summary>
        public bool HasDeviated { get; set; }

        /// <summary>
        /// Gets or sets the current location.
        /// </summary>
        public LocationUpdate CurrentLocation { get; set; } = new LocationUpdate();

        /// <summary>
        /// Gets or sets the check timestamp.
        /// </summary>
        public DateTime CheckedAt { get; set; }

        /// <summary>
        /// Gets or sets the result message.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Public tracking information for customer access.
    /// </summary>
    public class PublicTrackingInfo
    {
        /// <summary>
        /// Gets or sets the job ID.
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current latitude.
        /// </summary>
        public double CurrentLatitude { get; set; }

        /// <summary>
        /// Gets or sets the current longitude.
        /// </summary>
        public double CurrentLongitude { get; set; }

        /// <summary>
        /// Gets or sets the last updated timestamp.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Gets or sets the current status.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the estimated arrival time.
        /// </summary>
        public DateTime? EstimatedArrival { get; set; }
    }
}
