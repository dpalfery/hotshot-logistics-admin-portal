using HotshotLogistics.Domain.Entities;

namespace HotshotLogistics.Contracts.Services;

/// <summary>
/// Factory interface for creating communication service instances.
/// </summary>
public interface ICommunicationServiceFactory
{
    /// <summary>
    /// Gets the communication service for the specified type.
    /// </summary>
    /// <param name="type">The communication type.</param>
    /// <returns>The communication service instance.</returns>
    ICommunicationService GetService(string type);
}
