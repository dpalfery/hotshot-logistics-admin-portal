// <copyright file="CommunicationServiceFactory.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Application.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using HotshotLogistics.Domain.Entities;
    using HotshotLogistics.Contracts.Services;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Factory for creating communication service instances.
    /// </summary>
    public class CommunicationServiceFactory : ICommunicationServiceFactory
    {
        private readonly Dictionary<string, ICommunicationService> _services;
        private readonly ILogger<CommunicationServiceFactory> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommunicationServiceFactory"/> class.
        /// </summary>
        /// <param name="services">The collection of communication services.</param>
        /// <param name="logger">The logger.</param>
        public CommunicationServiceFactory(
            IEnumerable<ICommunicationService> services,
            ILogger<CommunicationServiceFactory> logger)
        {
            _services = services.ToDictionary(s => s.Type);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Validate that all communication types are registered
            var expectedTypes = new[] { "Sms", "Email", "Push" };
            foreach (var type in expectedTypes)
            {
                if (!_services.ContainsKey(type))
                {
                    _logger.LogWarning("Communication service for type {Type} is not registered", type);
                }
            }
        }

        /// <inheritdoc/>
        public ICommunicationService GetService(string type)
        {
            if (_services.TryGetValue(type, out var service))
            {
                return service;
            }

            throw new InvalidOperationException($"Communication service for type {type} is not available");
        }
    }
}
