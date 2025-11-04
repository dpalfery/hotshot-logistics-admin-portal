using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Core.Enums;

namespace HotshotLogistics.Contracts.Services;

/// <summary>
/// Service interface for job assignment operations.
/// </summary>
public interface IJobAssignmentService
{
    /// <summary>
    /// Gets a job assignment by ID.
    /// </summary>
    /// <param name="id">The assignment ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The job assignment if found; otherwise, null.</returns>
    Task<JobAssignmentDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all job assignments.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of job assignments.</returns>
    Task<IEnumerable<JobAssignmentDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets job assignments by driver ID.
    /// </summary>
    /// <param name="driverId">The driver ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of job assignments for the driver.</returns>
    Task<IEnumerable<JobAssignmentDto>> GetByDriverIdAsync(int driverId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets job assignments by job ID.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of job assignments for the job.</returns>
    Task<IEnumerable<JobAssignmentDto>> GetByJobIdAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active job assignments.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of active job assignments.</returns>
    Task<IEnumerable<JobAssignmentDto>> GetActiveAssignmentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a job to a driver.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="driverId">The driver ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created job assignment.</returns>
    Task<JobAssignmentDto> AssignJobAsync(string jobId, int driverId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a job assignment status.
    /// </summary>
    /// <param name="id">The assignment ID.</param>
    /// <param name="status">The new status.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated job assignment.</returns>
    Task<JobAssignmentDto> UpdateAssignmentStatusAsync(string id, JobAssignmentStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unassigns a job from a driver.
    /// </summary>
    /// <param name="id">The assignment ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the assignment was deleted; otherwise, false.</returns>
    Task<bool> UnassignJobAsync(string id, CancellationToken cancellationToken = default);
}
