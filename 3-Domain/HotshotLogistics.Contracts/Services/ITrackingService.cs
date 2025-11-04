using HotshotLogistics.Domain.Entities;

namespace HotshotLogistics.Contracts.Services;

/// <summary>
/// Service interface for location tracking operations.
/// </summary>
public interface ITrackingService
{
    /// <summary>
    /// Starts tracking for a job and driver.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="driverId">The driver identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if tracking started successfully.</returns>
    Task<bool> StartTrackingAsync(string jobId, int driverId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the location for a job and driver.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="driverId">The driver identifier.</param>
    /// <param name="locationUpdate">The location update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created location tracking record.</returns>
    Task<LocationTracking> UpdateLocationAsync(string jobId, int driverId, LocationUpdate locationUpdate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops tracking for a job.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if tracking stopped successfully.</returns>
    Task<bool> StopTrackingAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current location for a job.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The latest location tracking record.</returns>
    Task<LocationTracking?> GetCurrentLocationAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the location history for a job.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="startTime">The start time for the history.</param>
    /// <param name="endTime">The end time for the history.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The location tracking history.</returns>
    Task<IEnumerable<LocationTracking>> GetLocationHistoryAsync(string jobId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a driver has deviated from the expected route.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="currentLocation">The current location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the driver has deviated from the route.</returns>
    Task<bool> CheckRouteDeviationAsync(string jobId, LocationUpdate currentLocation, CancellationToken cancellationToken = default);
}
