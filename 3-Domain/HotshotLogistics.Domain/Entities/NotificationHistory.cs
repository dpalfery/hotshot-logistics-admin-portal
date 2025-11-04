using System;
using HotshotLogistics.Core.Enums;

namespace HotshotLogistics.Domain.Entities;

/// <summary>
/// Represents a notification history record.
/// </summary>
public class NotificationHistory
{
    /// <summary>
    /// Gets or sets the unique identifier for the notification history record.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification type.
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Gets or sets the notification title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the notification was sent.
    /// </summary>
    public DateTime SentAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the notification was sent successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the delivery method used for the notification.
    /// </summary>
    public string? DeliveryMethod { get; set; }

    /// <summary>
    /// Gets or sets any error message if the notification failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of retry attempts made.
    /// </summary>
    public int RetryCount { get; set; }
}
