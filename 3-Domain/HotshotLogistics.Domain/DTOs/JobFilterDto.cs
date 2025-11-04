using HotshotLogistics.Core.Enums;
namespace HotshotLogistics.Domain.DTOs;

/// <summary>
/// Filter criteria for job queries.
/// </summary>
public class JobFilterDto
{
    /// <summary>
    /// Gets or sets the job status filter.
    /// </summary>
    public JobStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets the customer ID filter.
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the assigned driver ID filter.
    /// </summary>
    public int? DriverId { get; set; }

    /// <summary>
    /// Gets or sets the assigned driver ID filter (alternative name for compatibility).
    /// </summary>
    public int? AssignedDriverId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to filter by jobs with assigned drivers.
    /// </summary>
    public bool? HasAssignedDriver { get; set; }

    /// <summary>
    /// Gets or sets the job priority filter.
    /// </summary>
    public JobPriority? Priority { get; set; }

    /// <summary>
    /// Gets or sets the minimum created date filter.
    /// </summary>
    public DateTime? CreatedAfter { get; set; }

    /// <summary>
    /// Gets or sets the maximum created date filter.
    /// </summary>
    public DateTime? CreatedBefore { get; set; }

    /// <summary>
    /// Gets or sets the minimum scheduled pickup time filter.
    /// </summary>
    public DateTime? ScheduledPickupAfter { get; set; }

    /// <summary>
    /// Gets or sets the maximum scheduled pickup time filter.
    /// </summary>
    public DateTime? ScheduledPickupBefore { get; set; }

    /// <summary>
    /// Gets or sets the minimum scheduled pickup time filter (alternative name for compatibility).
    /// </summary>
    public DateTime? ScheduledAfter { get; set; }

    /// <summary>
    /// Gets or sets the maximum scheduled pickup time filter (alternative name for compatibility).
    /// </summary>
    public DateTime? ScheduledBefore { get; set; }

    /// <summary>
    /// Gets or sets the list of statuses to filter by.
    /// </summary>
    public List<JobStatus>? StatusList { get; set; }

    /// <summary>
    /// Gets or sets the minimum estimated delivery time filter.
    /// </summary>
    public DateTime? EstimatedDeliveryAfter { get; set; }

    /// <summary>
    /// Gets or sets the maximum estimated delivery time filter.
    /// </summary>
    public DateTime? EstimatedDeliveryBefore { get; set; }

    /// <summary>
    /// Gets or sets the search term for job title or description.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Gets or sets the minimum job amount filter.
    /// </summary>
    public decimal? MinAmount { get; set; }

    /// <summary>
    /// Gets or sets the maximum job amount filter.
    /// </summary>
    public decimal? MaxAmount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include only active jobs.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include only overdue jobs.
    /// </summary>
    public bool? IsOverdue { get; set; }
}
