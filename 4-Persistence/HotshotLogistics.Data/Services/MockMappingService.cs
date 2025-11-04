// <copyright file="MockMappingService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Data.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using HotshotLogistics.Contracts.Services;
    using HotshotLogistics.Domain.DTOs;
    using HotshotLogistics.Domain.Entities;
    using HotshotLogistics.Domain.ValueObjects;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Mock implementation of the mapping service for development and testing.
    /// This service provides simulated geocoding and routing without requiring real API keys.
    /// </summary>
    public class MockMappingService : IMappingService
    {
        private readonly ILogger<MockMappingService> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockMappingService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public MockMappingService(ILogger<MockMappingService> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.logger.LogInformation("Using MockMappingService for development. No real API calls will be made.");
        }

        /// <inheritdoc/>
        public Task<GeocodingResult> GeocodeAddressAsync(string address, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Mock geocoding address: {Address}", address);

            // Return mock coordinates based on a hash of the address
            var hash = Math.Abs(address.GetHashCode());
            var lat = 30.0m + (hash % 20);
            var lng = -100.0m + (hash % 30);

            return Task.FromResult(new GeocodingResult
            {
                Latitude = lat,
                Longitude = lng,
                FormattedAddress = address,
                IsValid = true,
                Confidence = 0.95,
            });
        }

        /// <inheritdoc/>
        public Task<ReverseGeocodingResult> ReverseGeocodeAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Mock reverse geocoding: {Latitude}, {Longitude}", latitude, longitude);

            return Task.FromResult(new ReverseGeocodingResult
            {
                Address = "123 Mock Street",
                City = "Mock City",
                State = "TX",
                PostalCode = "12345",
                Country = "US",
                FormattedAddress = $"123 Mock Street, Mock City, TX 12345, US",
                IsValid = true,
            });
        }

        /// <inheritdoc/>
        public Task<bool> ValidateAddressAsync(string address, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Mock validating address: {Address}", address);
            // Mock implementation always returns true for valid addresses
            return Task.FromResult(!string.IsNullOrWhiteSpace(address));
        }

        /// <inheritdoc/>
        public Task<RouteResult> CalculateRouteAsync(Location origin, Location destination, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Mock calculating route from {Origin} to {Destination}", origin.FullAddress, destination.FullAddress);

            if (!origin.HasCoordinates || !destination.HasCoordinates)
            {
                return Task.FromResult(new RouteResult
                {
                    IsValid = false,
                    ErrorMessage = "Both origin and destination must have coordinates",
                });
            }

            // Calculate approximate distance using straight-line distance
            var distance = CalculateStraightLineDistance(origin, destination);
            var duration = TimeSpan.FromHours(distance / 60.0); // Assume average speed of 60 mph

            return Task.FromResult(new RouteResult
            {
                Distance = distance,
                Duration = duration,
                Waypoints = new List<Location> { origin, destination },
                Polyline = "mock_polyline_encoded_string",
                EstimatedArrival = DateTime.UtcNow.Add(duration),
                IsValid = true,
            });
        }

        /// <inheritdoc/>
        public async Task<OptimizedRouteResult> OptimizeRouteAsync(IList<Location> waypoints, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Mock optimizing route with {WaypointCount} waypoints", waypoints.Count);

            if (waypoints.Count < 2)
            {
                return new OptimizedRouteResult
                {
                    IsValid = false,
                    ErrorMessage = "At least 2 waypoints required",
                };
            }

            var totalDistance = 0.0;
            var totalDuration = TimeSpan.Zero;
            var segments = new List<RouteResult>();

            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                var segment = await CalculateRouteAsync(waypoints[i], waypoints[i + 1], cancellationToken);
                if (!segment.IsValid)
                {
                    return new OptimizedRouteResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Failed to calculate route segment {i}",
                    };
                }

                totalDistance += segment.Distance;
                totalDuration = totalDuration.Add(segment.Duration);
                segments.Add(segment);
            }

            return new OptimizedRouteResult
            {
                OptimizedWaypoints = waypoints,
                TotalDistance = totalDistance,
                TotalDuration = totalDuration,
                RouteSegments = segments,
                EstimatedArrival = DateTime.UtcNow.Add(totalDuration),
                IsValid = true,
            };
        }

        /// <inheritdoc/>
        public async Task<DistanceResult> CalculateDistanceAsync(Location origin, Location destination, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Mock calculating distance from {Origin} to {Destination}", origin.FullAddress, destination.FullAddress);

            var route = await CalculateRouteAsync(origin, destination, cancellationToken);
            return new DistanceResult
            {
                Distance = route.Distance,
                Duration = route.Duration,
                IsValid = route.IsValid,
                ErrorMessage = route.ErrorMessage,
            };
        }

        /// <summary>
        /// Calculates the straight-line distance between two locations using the Haversine formula.
        /// </summary>
        /// <param name="origin">The starting location.</param>
        /// <param name="destination">The destination location.</param>
        /// <returns>The distance in miles.</returns>
        private double CalculateStraightLineDistance(Location origin, Location destination)
        {
            const double earthRadiusMiles = 3958.8;

            var lat1 = (double)origin.Latitude!;
            var lon1 = (double)origin.Longitude!;
            var lat2 = (double)destination.Latitude!;
            var lon2 = (double)destination.Longitude!;

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2)) +
                    (Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2));

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusMiles * c;
        }

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        /// <param name="degrees">The angle in degrees.</param>
        /// <returns>The angle in radians.</returns>
        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
