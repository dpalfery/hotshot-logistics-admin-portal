// <copyright file="MappingServiceFactory.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Application.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using HotshotLogistics.Contracts.Factories;
    using HotshotLogistics.Contracts.Services;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Factory for creating mapping service instances based on configuration.
    /// </summary>
    public class MappingServiceFactory : IMappingServiceFactory
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<MappingServiceFactory> logger;
        private readonly IServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingServiceFactory"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="logger">The logger for the factory.</param>
        /// <param name="serviceProvider">The service provider to resolve mapping services.</param>
        public MappingServiceFactory(
            IConfiguration configuration,
            ILogger<MappingServiceFactory> logger,
            IServiceProvider serviceProvider)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Creates a mapping service instance based on the configured provider.
        /// </summary>
        /// <returns>The mapping service instance.</returns>
        public IMappingService CreateMappingService()
        {
            var providerName = configuration["Mapping:Provider"] ?? "Mock";
            var mappingServices = serviceProvider.GetServices<IMappingService>();
            var mappingServiceDict = mappingServices.ToDictionary(s => s.GetType().Name.Replace("Service", string.Empty), StringComparer.OrdinalIgnoreCase);

            if (mappingServiceDict.TryGetValue(providerName, out var service))
            {
                logger.LogInformation("Using mapping service: {ProviderName}", providerName);
                return service;
            }

            logger.LogError("Unsupported mapping provider: {ProviderName}. Falling back to Mock.", providerName);
            if (mappingServiceDict.TryGetValue("Mock", out var mockService))
            {
                return mockService;
            }

            throw new InvalidOperationException($"Unsupported mapping provider: {providerName} and no Mock service found.");
        }
    }
}
