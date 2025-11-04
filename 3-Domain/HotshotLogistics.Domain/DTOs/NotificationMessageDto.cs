using HotshotLogistics.Core.Enums;


namespace HotshotLogistics.Domain.DTOs
{
    /// <summary>
    /// Notification message model for real-time notifications
    /// </summary>
    public class NotificationMessageDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public string? UserId { get; set; }
        public Dictionary<string, object>? Data { get; set; }
    }
}
