using HotshotLogistics.Core.Enums;

namespace HotshotLogistics.Domain.Entities;
/// <summary>
/// Represents a job assignment to a driver in the system.
/// </summary>
public class JobAssignment 
{
    /// <inheritdoc/>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <inheritdoc/>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property for the assigned job.
    /// </summary>
    public virtual Job? Job { get; set; }

    /// <inheritdoc/>
    public int DriverId { get; set; }

    /// <summary>
    /// Navigation property for the assigned driver.
    /// </summary>
    public virtual Driver? Driver { get; set; }

    /// <inheritdoc/>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public JobAssignmentStatus Status { get; set; } = JobAssignmentStatus.Active;

    /// <summary>
    /// Gets or sets when the assignment was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JobAssignment"/> class.
    /// </summary>
    public JobAssignment()
    {
        if (AssignedAt == default)
        {
            AssignedAt = DateTime.UtcNow;
        }
    }
}
