using System;

namespace HotshotLogistics.Domain.ValueObjects;

/// <summary>
/// Represents a certification for a driver.
/// </summary>
public class Certification
{
    /// <summary>
    /// Gets or sets the certification name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issuing authority.
    /// </summary>
    public string IssuingAuthority { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the certification number.
    /// </summary>
    public string CertificationNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the issue date.
    /// </summary>
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Gets or sets the expiry date.
    /// </summary>
    public DateTime? ExpiryDate { get; set; }
}
