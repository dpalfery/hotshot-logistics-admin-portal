using System;
using System.Collections.Generic;

namespace HotshotLogistics.Domain.ValueObjects;

/// <summary>
/// Represents the availability schedule for a driver.
/// </summary>
public class AvailabilitySchedule
{
    /// <summary>
    /// Gets or sets the regular working hours.
    /// </summary>
    public List<WorkingHours> RegularHours { get; set; } = new List<WorkingHours>();

    /// <summary>
    /// Gets or sets the vacation days.
    /// </summary>
    public List<DateTime> VacationDays { get; set; } = new List<DateTime>();

    /// <summary>
    /// Gets or sets a value indicating whether the driver is available for emergency jobs.
    /// </summary>
    public bool AvailableForEmergency { get; set; }

    /// <summary>
    /// Gets or sets the preferred service areas.
    /// </summary>
    public List<string> PreferredServiceAreas { get; set; } = new List<string>();
}
