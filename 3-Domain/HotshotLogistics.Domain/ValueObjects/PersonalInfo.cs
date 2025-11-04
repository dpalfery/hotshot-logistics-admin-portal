using System;

namespace HotshotLogistics.Domain.ValueObjects;

/// <summary>
/// Represents personal information for a driver.
/// </summary>
public class PersonalInfo
{
    /// <summary>
    /// Gets or sets the first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the phone number.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date of birth.
    /// </summary>
    public DateTime DateOfBirth { get; set; }

    /// <summary>
    /// Gets or sets the social security number (encrypted).
    /// </summary>
    public string SSN { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the emergency contact name.
    /// </summary>
    public string EmergencyContactName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the emergency contact phone number.
    /// </summary>
    public string EmergencyContactPhone { get; set; } = string.Empty;
}
