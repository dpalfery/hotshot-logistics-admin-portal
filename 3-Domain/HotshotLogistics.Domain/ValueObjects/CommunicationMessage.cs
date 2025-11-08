
namespace HotshotLogistics.Domain.ValueObjects;

/// <summary>
/// Represents a communication message to be sent via various channels.
/// </summary>
public class CommunicationMessage
{
    /// <summary>
    /// Gets or sets the recipient address (phone number for SMS, email for email, device token for push).
    /// </summary>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subject line (used for email).
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the message body.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the title (used for push notifications).
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the template data for message formatting.
    /// </summary>
    public Dictionary<string, string> TemplateData { get; set; } = new();
}
