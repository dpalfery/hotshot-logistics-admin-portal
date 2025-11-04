namespace HotshotLogistics.Domain.DTOs;

/// <summary>
/// Settings for Twilio SMS service.
/// </summary>
public class TwilioSettings
{
    /// <summary>
    /// Gets or sets the Twilio account SID.
    /// </summary>
    public string AccountSid { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Twilio auth token.
    /// </summary>
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the from phone number.
    /// </summary>
    public string FromPhoneNumber { get; set; } = string.Empty;
}
