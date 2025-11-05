using System;
using System.Collections.Generic;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.ValueObjects;
namespace HotshotLogistics.Domain.DTOs;
/// <summary>
/// Data Transfer Object for Driver information.
/// </summary>
public class DriverDto 
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public DateTime LicenseExpiryDate { get; set; }
    public PersonalInfo PersonalInfo { get; set; } = new PersonalInfo();
    public LicenseInfo License { get; set; } = new LicenseInfo();
    public VehicleInfo Vehicle { get; set; } = new VehicleInfo();
    public List<Certification> Certifications { get; set; } = new List<Certification>();
    public AvailabilitySchedule Availability { get; set; } = new AvailabilitySchedule();
    public PerformanceMetrics Performance { get; set; } = new PerformanceMetrics();
    public PaymentInfo PaymentDetails { get; set; } = new PaymentInfo();
    public bool IsActive { get; set; }
    public DriverStatus CurrentStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

