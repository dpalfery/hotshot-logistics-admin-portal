using System;

namespace HotshotLogistics.Domain.ValueObjects;

/// <summary>
/// Represents vehicle information for a driver.
/// </summary>
public class VehicleInfo
{
    /// <summary>
    /// Gets or sets the vehicle type.
    /// </summary>
    public string VehicleType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the vehicle make.
    /// </summary>
    public string Make { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the vehicle model.
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the vehicle year.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the license plate number.
    /// </summary>
    public string LicensePlateNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the VIN number.
    /// </summary>
    public string VIN { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the insurance policy number.
    /// </summary>
    public string InsurancePolicyNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the insurance expiry date.
    /// </summary>
    public DateTime InsuranceExpiryDate { get; set; }
}
