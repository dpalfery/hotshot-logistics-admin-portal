using System;
using System.Collections.Generic;
using System.Linq;
using HotshotLogistics.Domain.ValueObjects;
using HotshotLogistics.Core.Enums;

namespace HotshotLogistics.Domain.Entities
{
/// <summary>
/// Represents a driver in the system.
/// </summary>
public class Driver 
{
    /// <inheritdoc/>
    public int Id { get; set; }

    /// <inheritdoc/>
    public PersonalInfo PersonalInfo { get; set; } = new PersonalInfo();

    /// <inheritdoc/>
    public LicenseInfo License { get; set; } = new LicenseInfo();

    /// <inheritdoc/>
    public VehicleInfo Vehicle { get; set; } = new VehicleInfo();

    /// <inheritdoc/>
    public List<Certification> Certifications { get; set; } = new List<Certification>();

    /// <inheritdoc/>
    public AvailabilitySchedule Availability { get; set; } = new AvailabilitySchedule();

    /// <inheritdoc/>
    public PerformanceMetrics Performance { get; set; } = new PerformanceMetrics();

    /// <inheritdoc/>
    public PaymentInfo PaymentDetails { get; set; } = new PaymentInfo();

    /// <inheritdoc/>
    public bool IsActive { get; set; } = true;

    /// <inheritdoc/>
    public DriverStatus CurrentStatus { get; set; } = DriverStatus.Offline;

    /// <inheritdoc/>
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc/>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Driver"/> class.
    /// </summary>
    public Driver()
    {
        if (CreatedAt == default)
            CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the full name of the driver.
    /// </summary>
    /// <returns>The full name.</returns>
    public string GetFullName()
    {
        return $"{PersonalInfo.FirstName} {PersonalInfo.LastName}".Trim();
    }

    /// <summary>
    /// Updates the driver's status.
    /// </summary>
    /// <param name="newStatus">The new status.</param>
    public void UpdateStatus(DriverStatus newStatus)
    {
        CurrentStatus = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a certification to the driver.
    /// </summary>
    /// <param name="certification">The certification to add.</param>
    public void AddCertification(Certification certification)
    {
        Certifications.Add(certification);
    }

    /// <summary>
    /// Removes a certification from the driver.
    /// </summary>
    /// <param name="certificationName">The name of the certification to remove.</param>
    public void RemoveCertification(string certificationName)
    {
        var certification = Certifications.FirstOrDefault(c => c.Name.Equals(certificationName, StringComparison.OrdinalIgnoreCase));
        if (certification != null)
        {
            Certifications.Remove(certification);
        }
    }

    /// <summary>
    /// Updates the performance metrics.
    /// </summary>
    /// <param name="metrics">The new performance metrics.</param>
    public void UpdatePerformance(PerformanceMetrics metrics)
    {
        Performance = metrics;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the driver is available for a job at the specified time.
    /// </summary>
    /// <param name="dateTime">The date and time to check availability for.</param>
    /// <returns>True if the driver is available, false otherwise.</returns>
    public bool IsAvailableAt(DateTime dateTime)
    {
        if (CurrentStatus != DriverStatus.Available)
            return false;

        if (Availability.VacationDays.Any(v => v.Date == dateTime.Date))
            return false;

        var dayOfWeek = dateTime.DayOfWeek;
        var workingHours = Availability.RegularHours.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);

        if (workingHours == null || !workingHours.IsAvailable)
            return false;

        var timeOfDay = dateTime.TimeOfDay;
        return timeOfDay >= workingHours.StartTime && timeOfDay <= workingHours.EndTime;
    }

    /// <summary>
    /// Checks if the driver's license is valid.
    /// </summary>
    /// <returns>True if the license is valid, false otherwise.</returns>
    public bool IsLicenseValid()
    {
        return License.LicenseExpiryDate > DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the driver's insurance is valid.
    /// </summary>
    /// <returns>True if the insurance is valid, false otherwise.</returns>
    public bool IsInsuranceValid()
    {
        return Vehicle.InsuranceExpiryDate > DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the driver has required certifications.
    /// </summary>
    /// <param name="requiredCertifications">The list of required certification names.</param>
    /// <returns>True if the driver has all required certifications, false otherwise.</returns>
    public bool HasRequiredCertifications(List<string> requiredCertifications)
    {
        if (requiredCertifications == null || !requiredCertifications.Any())
            return true;

        return requiredCertifications.All(required =>
            Certifications.Any(cert =>
                cert.Name.Equals(required, StringComparison.OrdinalIgnoreCase) &&
                (!cert.ExpiryDate.HasValue || cert.ExpiryDate.Value > DateTime.UtcNow)));
    }

    /// <summary>
    /// Calculates the driver's age.
    /// </summary>
    /// <returns>The driver's age in years.</returns>
    public int GetAge()
    {
        var today = DateTime.UtcNow;
        var age = today.Year - PersonalInfo.DateOfBirth.Year;
        if (PersonalInfo.DateOfBirth.Date > today.AddYears(-age))
            age--;
        return age;
    }

    /// <summary>
    /// Updates the driver's payment information.
    /// </summary>
    /// <param name="paymentInfo">The new payment information.</param>
    public void UpdatePaymentInfo(PaymentInfo paymentInfo)
    {
        PaymentDetails = paymentInfo;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the driver.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        CurrentStatus = DriverStatus.Offline;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactivates the driver.
    /// </summary>
    public void Reactivate()
    {
        IsActive = true;
        CurrentStatus = DriverStatus.Available;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates the driver data.
    /// </summary>
    /// <returns>True if the driver data is valid, false otherwise.</returns>
    public bool IsValid()
    {
        return Id > 0 &&
               !string.IsNullOrWhiteSpace(PersonalInfo.FirstName) &&
               !string.IsNullOrWhiteSpace(PersonalInfo.LastName) &&
               !string.IsNullOrWhiteSpace(PersonalInfo.Email) &&
               !string.IsNullOrWhiteSpace(License.LicenseNumber) &&
               IsLicenseValid() &&
               IsInsuranceValid() &&
               IsActive;
    }
}
}
