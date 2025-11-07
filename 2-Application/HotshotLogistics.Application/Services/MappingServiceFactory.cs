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
            var mappingServiceDict = mappingServices
                .ToDictionary(s => s.GetType().Name.Replace("Service", string.Empty), StringComparer.OrdinalIgnoreCase);

            // Try exact match first
            if (mappingServiceDict.TryGetValue(providerName, out var exact))
            {
                logger.LogInformation("Using mapping service (exact): {ProviderName}", providerName);
                return exact;
            }

            // Try more flexible matching: key contains providerName or providerName contains key
            var flexibleKey = mappingServiceDict.Keys
                .FirstOrDefault(k => k.IndexOf(providerName, StringComparison.OrdinalIgnoreCase) >= 0
                                     || providerName.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);

            if (flexibleKey != null && mappingServiceDict.TryGetValue(flexibleKey, out var flexibleService))
            {
                logger.LogInformation("Using mapping service (flexible match): configured={Configured}, matched={Matched}", providerName, flexibleKey);
                return flexibleService;
            }

            // Fallback: if any registered mapping service looks like a mock, use it
            var mockKey = mappingServiceDict.Keys.FirstOrDefault(k => k.IndexOf("mock", StringComparison.OrdinalIgnoreCase) >= 0);
            if (mockKey != null && mappingServiceDict.TryGetValue(mockKey, out var mockService))
            {
                logger.LogWarning("Unsupported mapping provider: {ProviderName}. Falling back to mock provider: {MockKey}.", providerName, mockKey);
                return mockService;
            }

            logger.LogError("Unsupported mapping provider: {ProviderName} and no mock service found.", providerName);
            throw new InvalidOperationException($"Unsupported mapping provider: {providerName} and no Mock service found.");
        }
    }
}
