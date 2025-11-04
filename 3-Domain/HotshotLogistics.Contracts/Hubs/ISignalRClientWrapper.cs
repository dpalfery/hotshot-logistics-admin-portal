using HotshotLogistics.Domain.Entities;

namespace HotshotLogistics.Contracts.Hubs;

/// <summary>
/// Wrapper interface for SignalR client operations to enable testing
/// </summary>
public interface ISignalRClientWrapper
{
    /// <summary>
    /// Send a message to a specific group
    /// </summary>
    Task SendToGroupAsync(string groupName, string methodName, params object[] args);

    /// <summary>
    /// Send a message to a specific user
    /// </summary>
    Task SendToUserAsync(string userId, string methodName, params object[] args);

    /// <summary>
    /// Send a message to all connected clients
    /// </summary>
    Task SendToAllAsync(string methodName, params object[] args);

    /// <summary>
    /// Send a message to the caller
    /// </summary>
    Task SendToCallerAsync(string methodName, params object[] args);
}
