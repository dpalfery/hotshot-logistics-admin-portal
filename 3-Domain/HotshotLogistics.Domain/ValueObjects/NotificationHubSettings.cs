namespace HotshotLogistics.Domain.ValueObjects;

/// <summary>
/// Settings for Azure Notification Hub.
/// </summary>
public class NotificationHubSettings
{
    /// <summary>
    /// Gets or sets the connection string for the Notification Hub.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the Notification Hub.
    /// </summary>
    public string HubName { get; set; } = string.Empty;
}
