using System;
using System.Collections.Generic;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.ValueObjects;
namespace HotshotLogistics.Domain.DTOs;
/// <summary>
/// Data Transfer Object that carries driver information between layers.
/// </summary>
public class DriverDto
{
    /// <summary>
    /// Primary key identifier for the driver.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Driver's first name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Driver's last name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Contact email address for the driver.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Contact phone number for the driver.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Driver's license number.
    /// </summary>
    public string? LicenseNumber { get; set; }

    /// <summary>
    /// Expiration date for the driver's license.
    /// </summary>
    public DateTime LicenseExpiryDate { get; set; }

    /// <summary>
    /// Personal information value object for the driver.
    /// </summary>
    public PersonalInfo PersonalInfo { get; set; } = new PersonalInfo();

    /// <summary>
    /// License information value object for the driver.
    /// </summary>
    public LicenseInfo License { get; set; } = new LicenseInfo();

    /// <summary>
    /// Information about the vehicle assigned to the driver.
    /// </summary>
    public VehicleInfo Vehicle { get; set; } = new VehicleInfo();

    /// <summary>
    /// Collection of certifications the driver holds.
    /// </summary>
    public List<Certification> Certifications { get; set; } = new List<Certification>();

    /// <summary>
    /// Driver availability schedule.
    /// </summary>
    public AvailabilitySchedule Availability { get; set; } = new AvailabilitySchedule();

    /// <summary>
    /// Performance metrics for the driver.
    /// </summary>
    public PerformanceMetrics Performance { get; set; } = new PerformanceMetrics();

    /// <summary>
    /// Payment-related information for the driver.
    /// </summary>
    public PaymentInfo PaymentDetails { get; set; } = new PaymentInfo();

    /// <summary>
    /// Flag indicating whether the driver is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Current operational status of the driver.
    /// </summary>
    public DriverStatus CurrentStatus { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Optional record update timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

