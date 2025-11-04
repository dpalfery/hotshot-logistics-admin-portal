namespace HotshotLogistics.Domain.DTOs;

/// <summary>
/// Settings for SendGrid email service.
/// </summary>
public class SendGridSettings
{
    /// <summary>
    /// Gets or sets the SendGrid API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the from email address.
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the from name.
    /// </summary>
    public string FromName { get; set; } = string.Empty;
}
