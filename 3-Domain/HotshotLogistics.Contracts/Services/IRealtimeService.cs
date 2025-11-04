using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.DTOs;

namespace HotshotLogistics.Contracts.Services;

/// <summary>
/// Service interface for managing real-time communications via SignalR
/// </summary>
public interface IRealtimeService
{
    /// <summary>
    /// Broadcast job status update to all clients tracking the job
    /// </summary>
    Task BroadcastJobStatusUpdate(string jobId, JobStatus status);

    /// <summary>
    /// Broadcast location update to clients tracking the job
    /// </summary>
    Task BroadcastLocationUpdate(string jobId, LocationUpdate location);

    /// <summary>
    /// Broadcast driver status change to relevant clients
    /// </summary>
    Task BroadcastDriverStatusChange(int driverId, DriverStatus status);

    /// <summary>
    /// Broadcast new job availability to available drivers
    /// </summary>
    Task BroadcastNewJobAvailable(ContractsJobDto job);

    /// <summary>
    /// Send notification to specific user or broadcast to all
    /// </summary>
    Task SendNotification(NotificationMessageDto message);

    /// <summary>
    /// Add user to a specific group for targeted messaging
    /// </summary>
    Task AddUserToGroup(string userId, string groupName);

    /// <summary>
    /// Remove user from a specific group
    /// </summary>
    Task RemoveUserFromGroup(string userId, string groupName);

    /// <summary>
    /// Send message to all users in a specific group
    /// </summary>
    Task SendToGroup(string groupName, string method, object data);

    /// <summary>
    /// Send message to a specific user
    /// </summary>
    Task SendToUser(string userId, string method, object data);
}
