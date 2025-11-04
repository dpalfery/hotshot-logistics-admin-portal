using HotshotLogistics.Domain.ValueObjects;

namespace HotshotLogistics.Contracts.Services;

/// <summary>
/// Interface for communication services that handle sending messages via various channels.
/// </summary>
public interface ICommunicationService
{
    /// <summary>
    /// Gets the type of communication this service handles.
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Sends a communication message asynchronously.
    /// </summary>
    /// <param name="message">The communication message to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the message was sent successfully, false otherwise.</returns>
    Task<bool> SendAsync(CommunicationMessage message, CancellationToken cancellationToken = default);
}
