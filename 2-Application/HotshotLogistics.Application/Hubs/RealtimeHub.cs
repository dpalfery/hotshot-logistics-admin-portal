using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using HotshotLogistics.Contracts.Hubs;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Domain.DTOs;


namespace HotshotLogistics.Application.Hubs;

/// <summary>
/// SignalR hub for real-time communication with clients
/// </summary>
public class RealtimeHub : Hub<IRealtimeHubClient>
{
    private readonly ILogger<RealtimeHub> _logger;
    private readonly ISignalRClientWrapper _signalRClient;
    private readonly IConnectionManagerService _connectionManager;

    public RealtimeHub(ILogger<RealtimeHub> logger, ISignalRClientWrapper signalRClient, IConnectionManagerService connectionManager)
    {
        _logger = logger;
        _signalRClient = signalRClient;
        _connectionManager = connectionManager;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier ?? Context.ConnectionId;
        await _connectionManager.AddConnectionAsync(userId, Context.ConnectionId);
        _logger.LogInformation("Client connected: User {UserId}, Connection {ConnectionId}", userId, Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier ?? Context.ConnectionId;
        await _connectionManager.RemoveConnectionAsync(Context.ConnectionId);
        _logger.LogInformation("Client disconnected: User {UserId}, Connection {ConnectionId}", userId, Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Broadcast job status update to all connected clients tracking the job
    /// </summary>
    public async Task JobStatusUpdated(string jobId, JobStatus status)
    {
        try
        {
            await _signalRClient.SendToGroupAsync($"job-{jobId}", "JobStatusUpdated", jobId, status);
            _logger.LogInformation("Job status update sent for job {JobId}: {Status}", jobId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting job status update for job {JobId}", jobId);
        }
    }

    /// <summary>
    /// Broadcast location update to clients tracking the job
    /// </summary>
    public async Task LocationUpdated(string jobId, LocationUpdate location)
    {
        try
        {
            await _signalRClient.SendToGroupAsync($"job-{jobId}", "LocationUpdated", jobId, location);
            _logger.LogDebug("Location update sent for job {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting location update for job {JobId}", jobId);
        }
    }

    /// <summary>
    /// Broadcast driver status change to relevant clients
    /// </summary>
    public async Task DriverStatusChanged(int driverId, DriverStatus status)
    {
        try
        {
            await _signalRClient.SendToGroupAsync($"driver-{driverId}", "DriverStatusChanged", driverId, status);
            // Also notify admin clients
            await _signalRClient.SendToGroupAsync("admins", "DriverStatusChanged", driverId, status);
            _logger.LogInformation("Driver status update sent for driver {DriverId}: {Status}", driverId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting driver status update for driver {DriverId}", driverId);
        }
    }

    /// <summary>
    /// Broadcast new job availability to available drivers
    /// </summary>
    public async Task NewJobAvailable(ContractsJobDto job)
    {
        try
        {
            await _signalRClient.SendToGroupAsync("available-drivers", "NewJobAvailable", job);
            _logger.LogInformation("New job availability broadcast for job {JobId}", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting new job availability for job {JobId}", job.Id);
        }
    }

    /// <summary>
    /// Send notification to specific user or broadcast to all
    /// </summary>
    public async Task NotificationReceived(NotificationMessageDto message)
    {
        try
        {
            if (!string.IsNullOrEmpty(message.UserId))
            {
                await _connectionManager.SendToUserAsync(message.UserId, "NotificationReceived", message);
            }
            else
            {
                await _signalRClient.SendToAllAsync("NotificationReceived", message);
            }

            _logger.LogInformation("Notification sent: {Title}", message.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification: {Title}", message.Title);
        }
    }

    /// <summary>
    /// Join job tracking group
    /// </summary>
    public async Task JoinJobTracking(string jobId)
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"job-{jobId}");
            _logger.LogInformation("Connection {ConnectionId} joined job tracking for {JobId}",
                Context.ConnectionId, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining job tracking");
        }
    }

    /// <summary>
    /// Leave job tracking group
    /// </summary>
    public async Task LeaveJobTracking(string jobId)
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"job-{jobId}");
            _logger.LogInformation("Connection {ConnectionId} left job tracking for {JobId}",
                Context.ConnectionId, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving job tracking");
        }
    }
}
