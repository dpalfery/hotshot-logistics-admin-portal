using System;

namespace HotshotLogistics.Domain.ValueObjects;

/// <summary>
/// Represents working hours for a day.
/// </summary>
public class WorkingHours
{
    /// <summary>
    /// Gets or sets the day of the week.
    /// </summary>
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>
    /// Gets or sets the start time.
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time.
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the driver is available on this day.
    /// </summary>
    public bool IsAvailable { get; set; } = true;
}
