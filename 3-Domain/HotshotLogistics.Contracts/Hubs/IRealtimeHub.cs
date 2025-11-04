using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Domain.ValueObjects;
namespace HotshotLogistics.Contracts.Hubs;

/// <summary>
/// Interface defining SignalR hub methods for real-time communication
/// </summary>
public interface IRealtimeHub
{
    // Server to Client Events
    Task JobStatusUpdated(string jobId, JobStatus status);
    Task LocationUpdated(string jobId, LocationUpdate location);
    Task DriverStatusChanged(int driverId, DriverStatus status);
    Task NewJobAvailable(Job job);
    Task NotificationReceived(NotificationMessageDto message);
}

/// <summary>
/// Interface defining client methods that can be called from the hub
/// </summary>
public interface IRealtimeHubClient
{
    Task JobStatusUpdated(string jobId, JobStatus status);
    Task LocationUpdated(string jobId, LocationUpdate location);
    Task DriverStatusChanged(int driverId, DriverStatus status);
    Task NewJobAvailable(Job job);
    Task NotificationReceived(NotificationMessageDto message);
}
