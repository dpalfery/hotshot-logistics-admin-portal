// <copyright file="IMappingServiceFactory.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Contracts.Factories
{
    using HotshotLogistics.Contracts.Services;

    /// <summary>
    /// Factory interface for creating mapping service instances based on configuration.
    /// </summary>
    public interface IMappingServiceFactory
    {
        /// <summary>
        /// Creates a mapping service instance based on the configured provider.
        /// </summary>
        /// <returns>The mapping service instance.</returns>
        IMappingService CreateMappingService();
    }
}
