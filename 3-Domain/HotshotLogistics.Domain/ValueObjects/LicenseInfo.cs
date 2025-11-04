using System;
using System.Collections.Generic;

namespace HotshotLogistics.Domain.ValueObjects;

/// <summary>
/// Represents license information for a driver.
/// </summary>
public class LicenseInfo
{
    /// <summary>
    /// Gets or sets the license number.
    /// </summary>
    public string LicenseNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state that issued the license.
    /// </summary>
    public string LicenseState { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the license expiry date.
    /// </summary>
    public DateTime LicenseExpiryDate { get; set; }

    /// <summary>
    /// Gets or sets the license class.
    /// </summary>
    public string LicenseClass { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the license has endorsements.
    /// </summary>
    public List<string> Endorsements { get; set; } = new List<string>();
}
