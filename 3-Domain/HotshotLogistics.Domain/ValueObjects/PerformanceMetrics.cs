namespace HotshotLogistics.Domain.ValueObjects;

/// <summary>
/// Represents performance metrics for a driver.
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// Gets or sets the on-time delivery rate (percentage).
    /// </summary>
    public decimal OnTimeDeliveryRate { get; set; }

    /// <summary>
    /// Gets or sets the average customer rating (1-5 scale).
    /// </summary>
    public decimal CustomerRating { get; set; }

    /// <summary>
    /// Gets or sets the number of completed jobs.
    /// </summary>
    public int CompletedJobs { get; set; }

    /// <summary>
    /// Gets or sets the number of cancelled jobs.
    /// </summary>
    public int CancelledJobs { get; set; }

    /// <summary>
    /// Gets or sets the total miles driven.
    /// </summary>
    public decimal TotalMilesDriven { get; set; }

    /// <summary>
    /// Gets or sets the average delivery time in hours.
    /// </summary>
    public decimal AverageDeliveryTime { get; set; }

    /// <summary>
    /// Gets or sets the safety incident count.
    /// </summary>
    public int SafetyIncidents { get; set; }
}
