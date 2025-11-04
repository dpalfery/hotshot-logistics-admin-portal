using HotshotLogistics.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR;
using HotshotLogistics.Application.Hubs;

namespace HotshotLogistics.Application.Services;

/// <summary>
/// Implementation of SignalR client wrapper for real SignalR operations.
/// </summary>
public class SignalRClientWrapper : ISignalRClientWrapper
{
    private readonly IHubContext<RealtimeHub, IRealtimeHubClient> _hubContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalRClientWrapper"/> class.
    /// </summary>
    /// <param name="hubContext">The hub context.</param>
    public SignalRClientWrapper(IHubContext<RealtimeHub, IRealtimeHubClient> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <inheritdoc/>
    public async Task SendToGroupAsync(string groupName, string methodName, params object[] args)
    {
        var result = _hubContext.Clients.Group(groupName)
            .GetType()
            .GetMethod(methodName)
            ?.Invoke(_hubContext.Clients.Group(groupName), args);
        if (result is Task task)
        {
            await task;
        }
    }

    /// <inheritdoc/>
    public async Task SendToUserAsync(string userId, string methodName, params object[] args)
    {
        var result = _hubContext.Clients.User(userId)
            .GetType()
            .GetMethod(methodName)
            ?.Invoke(_hubContext.Clients.User(userId), args);
        if (result is Task task)
        {
            await task;
        }
    }

    /// <inheritdoc/>
    public async Task SendToAllAsync(string methodName, params object[] args)
    {
        var result = _hubContext.Clients.All
            .GetType()
            .GetMethod(methodName)
            ?.Invoke(_hubContext.Clients.All, args);
        if (result is Task task)
        {
            await task;
        }
    }

    /// <inheritdoc/>
    public Task SendToCallerAsync(string methodName, params object[] args)
    {
        // Note: Caller is not available in this context for server-side SignalR.
        // This would need to be implemented differently for client-side hubs.
        throw new NotSupportedException("Caller is not supported in server-side SignalR context.");
    }
}
