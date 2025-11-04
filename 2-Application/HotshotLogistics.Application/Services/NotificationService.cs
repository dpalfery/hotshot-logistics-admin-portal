// <copyright file="NotificationService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Application.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using HotshotLogistics.Domain.Entities;
    using HotshotLogistics.Domain.ValueObjects;
    using HotshotLogistics.Contracts.Services;
    using HotshotLogistics.Core.Enums;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Caching.Distributed;
    using System.Text.Json;

    /// <summary>
    /// Service for notification operations with multi-channel support.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly IDistributedCache cache;
        private readonly ILogger<NotificationService> logger;
        private readonly ICommunicationServiceFactory communicationFactory;

        // Retry configuration
        private const int MaxRetryAttempts = 3;
        private static readonly TimeSpan BaseRetryDelay = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationService"/> class.
        /// </summary>
        /// <param name="cache">The distributed cache.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="communicationFactory">The communication service factory.</param>
        public NotificationService(
            IDistributedCache cache,
            ILogger<NotificationService> logger,
            ICommunicationServiceFactory communicationFactory)
        {
            this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.communicationFactory = communicationFactory ?? throw new ArgumentNullException(nameof(communicationFactory));
        }

        /// <inheritdoc/>
        public async Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                throw new ArgumentException("Phone number cannot be empty", nameof(phoneNumber));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be empty", nameof(message));
            }

            return await ExecuteWithRetryAsync(async () =>
            {
                logger.LogInformation("Sending SMS to {PhoneNumber}", phoneNumber);

                var commMessage = new CommunicationMessage
                {
                    To = phoneNumber,
                    Body = message
                };

                var service = communicationFactory.GetService("Sms");
                return await service.SendAsync(commMessage, cancellationToken);
            }, $"SMS to {phoneNumber}");
        }

        /// <inheritdoc/>
        public async Task<bool> SendEmailAsync(string emailAddress, string subject, string message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
            {
                throw new ArgumentException("Email address cannot be empty", nameof(emailAddress));
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new ArgumentException("Subject cannot be empty", nameof(subject));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be empty", nameof(message));
            }

            return await ExecuteWithRetryAsync(async () =>
            {
                logger.LogInformation("Sending email with subject: {Subject} to recipient", subject);

                var commMessage = new CommunicationMessage
                {
                    To = emailAddress,
                    Subject = subject,
                    Body = message
                };

                var service = communicationFactory.GetService("Email");
                return await service.SendAsync(commMessage, cancellationToken);
            }, "Email notification");
        }

        /// <inheritdoc/>
        public async Task<bool> SendPushNotificationAsync(string deviceToken, string title, string message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(deviceToken))
            {
                throw new ArgumentException("Device token cannot be empty", nameof(deviceToken));
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Title cannot be empty", nameof(title));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be empty", nameof(message));
            }

            return await ExecuteWithRetryAsync(async () =>
            {
                logger.LogInformation("Sending push notification to device {DeviceToken}", deviceToken);

                var commMessage = new CommunicationMessage
                {
                    To = deviceToken,
                    Title = title,
                    Body = message
                };

                var service = communicationFactory.GetService("Push");
                return await service.SendAsync(commMessage, cancellationToken);
            }, $"Push notification to {deviceToken}");
        }

        /// <inheritdoc/>
        public async Task<bool> SendNotificationAsync(string userId, NotificationType notificationType, string title, string message, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Sending {NotificationType} notification to user {UserId}", notificationType, userId);

            var preferences = await GetNotificationPreferencesAsync(userId, cancellationToken);
            if (preferences == null)
            {
                logger.LogWarning("No notification preferences found for user {UserId}", userId);
                return false;
            }

            // Check if user wants to receive this type of notification
            if (!preferences.EnabledNotificationTypes.Contains(notificationType))
            {
                logger.LogDebug("User {UserId} has disabled {NotificationType} notifications", userId, notificationType);
                return true; // Return true as it's not an error, just user preference
            }

            var results = new List<bool>();

            // Send via enabled channels
            if (preferences.SmsEnabled && !string.IsNullOrWhiteSpace(preferences.PhoneNumber))
            {
                var smsResult = await SendSmsAsync(preferences.PhoneNumber, message, cancellationToken);
                results.Add(smsResult);
            }

            if (preferences.EmailEnabled && !string.IsNullOrWhiteSpace(preferences.EmailAddress))
            {
                var emailResult = await SendEmailAsync(preferences.EmailAddress, title, message, cancellationToken);
                results.Add(emailResult);
            }

            if (preferences.PushEnabled && !string.IsNullOrWhiteSpace(preferences.DeviceToken))
            {
                var pushResult = await SendPushNotificationAsync(preferences.DeviceToken, title, message, cancellationToken);
                results.Add(pushResult);
            }

            // Return true if at least one notification was sent successfully
            var success = results.Any() && results.Any(r => r);

            // Log notification to history
            await LogNotificationAsync(userId, notificationType, title, message, success, cancellationToken);

            if (success)
            {
                logger.LogInformation("Notification sent successfully to user {UserId}", userId);
            }
            else
            {
                logger.LogWarning("Failed to send notification to user {UserId}", userId);
            }

            return success;
        }

        /// <inheritdoc/>
        public async Task<NotificationPreferences?> GetNotificationPreferencesAsync(string userId, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"notification_preferences:{userId}";
            var preferencesJson = await cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(preferencesJson))
            {
                try
                {
                    return JsonSerializer.Deserialize<NotificationPreferences>(preferencesJson);
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex, "Failed to deserialize notification preferences for user {UserId}", userId);
                }
            }

            // In real implementation, get from database
            // For demo purposes, return default preferences
            var defaultPreferences = new NotificationPreferences
            {
                UserId = userId,
                SmsEnabled = true,
                EmailEnabled = true,
                PushEnabled = true,
                PhoneNumber = "+1234567890", // Demo phone number
                EmailAddress = $"user{userId}@example.com", // Demo email
                DeviceToken = $"device_token_{userId}", // Demo device token
                EnabledNotificationTypes = Enum.GetValues<NotificationType>().ToList()
            };

            // Cache the default preferences
            await UpdateNotificationPreferencesAsync(userId, defaultPreferences, cancellationToken);

            return defaultPreferences;
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateNotificationPreferencesAsync(string userId, NotificationPreferences preferences, CancellationToken cancellationToken = default)
        {
            if (preferences == null)
            {
                throw new ArgumentNullException(nameof(preferences));
            }

            try
            {
                var cacheKey = $"notification_preferences:{userId}";
                var preferencesJson = JsonSerializer.Serialize(preferences);

                await cache.SetStringAsync(cacheKey, preferencesJson, new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromDays(30) // Cache for 30 days
                }, cancellationToken);

                logger.LogInformation("Notification preferences updated for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update notification preferences for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Gets notification history for a user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="startDate">The start date for the history.</param>
        /// <param name="endDate">The end date for the history.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The notification history.</returns>
        public async Task<List<NotificationHistory>> GetNotificationHistoryAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
        {
            logger.LogDebug("Getting notification history for user {UserId}", userId);

            var cacheKey = $"notification_history:{userId}";
            var historyJson = await cache.GetStringAsync(cacheKey, cancellationToken);

            var history = new List<NotificationHistory>();

            if (!string.IsNullOrEmpty(historyJson))
            {
                try
                {
                    history = JsonSerializer.Deserialize<List<NotificationHistory>>(historyJson) ?? new List<NotificationHistory>();
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex, "Failed to deserialize notification history for user {UserId}", userId);
                }
            }

            // Filter by date range if specified
            if (startDate.HasValue || endDate.HasValue)
            {
                history = history.Where(h =>
                    (!startDate.HasValue || h.SentAt >= startDate.Value) &&
                    (!endDate.HasValue || h.SentAt <= endDate.Value)
                ).ToList();
            }

            logger.LogDebug("Retrieved {Count} notification history records for user {UserId}", history.Count, userId);
            return history;
        }

        /// <summary>
        /// Sends batch notifications to multiple users.
        /// </summary>
        /// <param name="userIds">The list of user identifiers.</param>
        /// <param name="notificationType">The type of notification.</param>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of notifications sent successfully.</returns>
        public async Task<int> SendBatchNotificationAsync(List<string> userIds, NotificationType notificationType, string title, string message, CancellationToken cancellationToken = default)
        {
            if (userIds == null || !userIds.Any())
            {
                throw new ArgumentException("User IDs list cannot be empty", nameof(userIds));
            }

            logger.LogInformation("Sending batch {NotificationType} notification to {UserCount} users", notificationType, userIds.Count);

            var successCount = 0;
            var tasks = new List<Task<bool>>();

            // Throttle batch sending to prevent overwhelming the system
            const int batchSize = 10;
            for (int i = 0; i < userIds.Count; i += batchSize)
            {
                var batch = userIds.Skip(i).Take(batchSize);
                var batchTasks = batch.Select(userId => SendNotificationAsync(userId, notificationType, title, message, cancellationToken));
                tasks.AddRange(batchTasks);

                // Wait for current batch to complete before starting next batch
                var batchResults = await Task.WhenAll(batchTasks);
                successCount += batchResults.Count(r => r);

                // Small delay between batches to prevent rate limiting
                if (i + batchSize < userIds.Count)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }

            logger.LogInformation("Batch notification completed: {SuccessCount}/{TotalCount} sent successfully", successCount, userIds.Count);
            return successCount;
        }

        /// <summary>
        /// Sends an urgent notification using all available channels simultaneously.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if at least one channel succeeded.</returns>
        public async Task<bool> SendUrgentNotificationAsync(string userId, string title, string message, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Sending urgent notification to user {UserId}", userId);

            var preferences = await GetNotificationPreferencesAsync(userId, cancellationToken);
            if (preferences == null)
            {
                logger.LogWarning("No notification preferences found for user {UserId}", userId);
                return false;
            }

            var tasks = new List<Task<bool>>();

            // Send via all available channels simultaneously for urgent notifications
            if (!string.IsNullOrWhiteSpace(preferences.PhoneNumber))
            {
                tasks.Add(SendSmsAsync(preferences.PhoneNumber, $"URGENT: {message}", cancellationToken));
            }

            if (!string.IsNullOrWhiteSpace(preferences.EmailAddress))
            {
                tasks.Add(SendEmailAsync(preferences.EmailAddress, $"URGENT: {title}", message, cancellationToken));
            }

            if (!string.IsNullOrWhiteSpace(preferences.DeviceToken))
            {
                tasks.Add(SendPushNotificationAsync(preferences.DeviceToken, $"URGENT: {title}", message, cancellationToken));
            }

            if (!tasks.Any())
            {
                logger.LogWarning("No notification channels available for user {UserId}", userId);
                return false;
            }

            var results = await Task.WhenAll(tasks);
            var success = results.Any(r => r);

            // Log notification to history
            await LogNotificationAsync(userId, NotificationType.Emergency, title, message, success, cancellationToken);

            if (success)
            {
                logger.LogInformation("Urgent notification sent successfully to user {UserId}", userId);
            }
            else
            {
                logger.LogError("Failed to send urgent notification to user {UserId} via all channels", userId);
            }

            return success;
        }

        /// <summary>
        /// Validates notification preferences.
        /// </summary>
        /// <param name="preferences">The notification preferences to validate.</param>
        /// <returns>True if preferences are valid, false otherwise.</returns>
        public bool ValidateNotificationPreferences(NotificationPreferences preferences)
        {
            if (preferences == null)
                return false;

            if (string.IsNullOrWhiteSpace(preferences.UserId))
                return false;

            // Validate phone number format if SMS is enabled
            if (preferences.SmsEnabled && !string.IsNullOrWhiteSpace(preferences.PhoneNumber))
            {
                if (!IsValidPhoneNumber(preferences.PhoneNumber))
                {
                    logger.LogWarning("Invalid phone number format: {PhoneNumber}", preferences.PhoneNumber);
                    return false;
                }
            }

            // Validate email format if email is enabled
            if (preferences.EmailEnabled && !string.IsNullOrWhiteSpace(preferences.EmailAddress))
            {
                if (!IsValidEmail(preferences.EmailAddress))
                {
                    logger.LogWarning("Invalid email format: {EmailAddress}", preferences.EmailAddress);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Logs a notification to the user's history.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="notificationType">The notification type.</param>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        /// <param name="success">Whether the notification was sent successfully.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task LogNotificationAsync(string userId, NotificationType notificationType, string title, string message, bool success, CancellationToken cancellationToken)
        {
            try
            {
                var history = await GetNotificationHistoryAsync(userId, cancellationToken: cancellationToken);

                var notification = new NotificationHistory
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Type = notificationType,
                    Title = title,
                    Message = message,
                    SentAt = DateTime.UtcNow,
                    Success = success
                };

                history.Add(notification);

                // Keep only last 100 notifications per user
                if (history.Count > 100)
                {
                    history = history.OrderByDescending(h => h.SentAt).Take(100).ToList();
                }

                var cacheKey = $"notification_history:{userId}";
                var historyJson = JsonSerializer.Serialize(history);

                await cache.SetStringAsync(cacheKey, historyJson, new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromDays(30)
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to log notification history for user {UserId}", userId);
            }
        }

        /// <summary>
        /// Validates a phone number format.
        /// </summary>
        /// <param name="phoneNumber">The phone number to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        private static bool IsValidPhoneNumber(string phoneNumber)
        {
            // Simple validation - in real implementation, use a proper phone number validation library
            return !string.IsNullOrWhiteSpace(phoneNumber) &&
                   phoneNumber.Length >= 10 &&
                   phoneNumber.All(c => char.IsDigit(c) || c == '+' || c == '-' || c == '(' || c == ')' || c == ' ');
        }

        /// <summary>
        /// Validates an email address format.
        /// </summary>
        /// <param name="email">The email address to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        private static bool IsValidEmail(string email)
        {
            // Simple validation - in real implementation, use a proper email validation library
            return !string.IsNullOrWhiteSpace(email) &&
                   email.Contains('@') &&
                   email.Contains('.') &&
                   email.Length > 5;
        }

        /// <summary>
        /// Executes an operation with exponential backoff retry logic.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="operationName">The name of the operation for logging.</param>
        /// <returns>True if the operation succeeded, false otherwise.</returns>
        private async Task<bool> ExecuteWithRetryAsync(Func<Task<bool>> operation, string operationName)
        {
            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                try
                {
                    var result = await operation();
                    if (result)
                    {
                        return true;
                    }

                    if (attempt < MaxRetryAttempts)
                    {
                        var delay = TimeSpan.FromMilliseconds(BaseRetryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                        logger.LogInformation("Retrying {OperationName} in {Delay}ms (attempt {Attempt}/{MaxAttempts})",
                            operationName, delay.TotalMilliseconds, attempt, MaxRetryAttempts);
                        await Task.Delay(delay);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Attempt {Attempt}/{MaxAttempts} failed for {OperationName}",
                        attempt, MaxRetryAttempts, operationName);

                    if (attempt == MaxRetryAttempts)
                    {
                        logger.LogError(ex, "All retry attempts failed for {OperationName}", operationName);
                        return false;
                    }

                    var delay = TimeSpan.FromMilliseconds(BaseRetryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                    await Task.Delay(delay);
                }
            }

            return false;
        }
    }
}
