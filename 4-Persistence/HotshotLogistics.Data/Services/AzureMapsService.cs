// <copyright file="AzureMapsService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Data.Services
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using HotshotLogistics.Contracts.Services;
    using HotshotLogistics.Domain.ValueObjects;
    using HotshotLogistics.Domain.Entities;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using HotshotLogistics.Domain.DTOs;

    /// <summary>
    /// Azure Maps implementation of the mapping service.
    /// </summary>
    public class AzureMapsService : IMappingService
    {
        private readonly HttpClient httpClient;
        private readonly ILogger<AzureMapsService> logger;
        private readonly string subscriptionKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureMapsService"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client for API calls.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="settings">The Azure Maps settings.</param>
        public AzureMapsService(HttpClient httpClient, ILogger<AzureMapsService> logger, IOptions<AzureMapsSettings> settings)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var settingsValue = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            this.subscriptionKey = settingsValue.SubscriptionKey ?? throw new ArgumentNullException(nameof(settingsValue.SubscriptionKey));
        }

        /// <inheritdoc/>
        public async Task<GeocodingResult> GeocodeAddressAsync(string address, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"https://atlas.microsoft.com/search/address/json?api-version=1.0&subscription-key={subscriptionKey}&query={Uri.EscapeDataString(address)}";

                var response = await httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<AzureMapsGeocodeResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Results?.Count > 0)
                {
                    var firstResult = result.Results[0];
                    return new GeocodingResult
                    {
                        Latitude = (decimal)firstResult.Position.Lat,
                        Longitude = (decimal)firstResult.Position.Lon,
                        FormattedAddress = firstResult.Address?.FreeformAddress ?? address,
                        IsValid = true,
                        Confidence = firstResult.Confidence ?? 0.0,
                    };
                }

                return new GeocodingResult
                {
                    IsValid = false,
                    ErrorMessage = "No geocoding results found",
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to geocode address: {Address}", address);
                return new GeocodingResult
                {
                    IsValid = false,
                    ErrorMessage = ex.Message,
                };
            }
        }

        /// <inheritdoc/>
        public async Task<ReverseGeocodingResult> ReverseGeocodeAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"https://atlas.microsoft.com/search/address/reverse/json?api-version=1.0&subscription-key={subscriptionKey}&query={latitude},{longitude}";

                var response = await httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<AzureMapsReverseGeocodeResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Addresses?.Count > 0)
                {
                    var address = result.Addresses[0];
                    return new ReverseGeocodingResult
                    {
                        Address = address.Address?.StreetName ?? string.Empty,
                        City = address.Address?.Municipality ?? string.Empty,
                        State = address.Address?.CountrySubdivision ?? string.Empty,
                        PostalCode = address.Address?.PostalCode ?? string.Empty,
                        Country = address.Address?.CountryCode ?? string.Empty,
                        FormattedAddress = address.Address?.FreeformAddress ?? string.Empty,
                        IsValid = true,
                    };
                }

                return new ReverseGeocodingResult
                {
                    IsValid = false,
                    ErrorMessage = "No reverse geocoding results found",
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to reverse geocode coordinates");
                return new ReverseGeocodingResult
                {
                    IsValid = false,
                    ErrorMessage = ex.Message,
                };
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ValidateAddressAsync(string address, CancellationToken cancellationToken = default)
        {
            var result = await GeocodeAddressAsync(address, cancellationToken);
            return result.IsValid && result.Confidence > 0.7; // Require high confidence for validation
        }

        /// <inheritdoc/>
        public async Task<RouteResult> CalculateRouteAsync(Location origin, Location destination, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!origin.HasCoordinates || !destination.HasCoordinates)
                {
                    return new RouteResult
                    {
                        IsValid = false,
                        ErrorMessage = "Both origin and destination must have coordinates",
                    };
                }

                var url = $"https://atlas.microsoft.com/route/directions/json?api-version=1.0&subscription-key={subscriptionKey}&query={origin.Latitude},{origin.Longitude}:{destination.Latitude},{destination.Longitude}";

                var response = await httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<AzureMapsRouteResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Routes?.Count > 0)
                {
                    var route = result.Routes[0];
                    var summary = route.Summary;

                    return new RouteResult
                    {
                        Distance = summary.LengthInMeters * 0.000621371, // Convert meters to miles
                        Duration = TimeSpan.FromSeconds(summary.TravelTimeInSeconds),
                        Waypoints = new List<Location> { origin, destination },
                        Polyline = string.Empty, // Azure Maps doesn't provide polyline in basic response
                        EstimatedArrival = DateTime.UtcNow.AddSeconds(summary.TravelTimeInSeconds),
                        IsValid = true,
                    };
                }

                return new RouteResult
                {
                    IsValid = false,
                    ErrorMessage = "No route found",
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to calculate route between {Origin} and {Destination}", origin.FullAddress, destination.FullAddress);
                return new RouteResult
                {
                    IsValid = false,
                    ErrorMessage = ex.Message,
                };
            }
        }

        /// <inheritdoc/>
        public async Task<OptimizedRouteResult> OptimizeRouteAsync(IList<Location> waypoints, CancellationToken cancellationToken = default)
        {
            // For simplicity, return the waypoints in order without optimization
            // In a real implementation, this would use Azure Maps Route Optimization API
            try
            {
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
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to optimize route with {WaypointCount} waypoints", waypoints.Count);
                return new OptimizedRouteResult
                {
                    IsValid = false,
                    ErrorMessage = ex.Message,
                };
            }
        }

        /// <inheritdoc/>
        public async Task<DistanceResult> CalculateDistanceAsync(Location origin, Location destination, CancellationToken cancellationToken = default)
        {
            var route = await CalculateRouteAsync(origin, destination, cancellationToken);
            return new DistanceResult
            {
                Distance = route.Distance,
                Duration = route.Duration,
                IsValid = route.IsValid,
                ErrorMessage = route.ErrorMessage,
            };
        }

        // Internal classes for Azure Maps API responses
        private class AzureMapsGeocodeResponse
        {
            public List<GeocodeResultItem> Results { get; set; } = new List<GeocodeResultItem>();
        }

        private class GeocodeResultItem
        {
            public Position Position { get; set; } = new Position();
            public Address Address { get; set; } = new Address();
            public double? Confidence { get; set; }
        }

        private class Position
        {
            public double Lat { get; set; }
            public double Lon { get; set; }
        }

        private class Address
        {
            public string FreeformAddress { get; set; } = string.Empty;
        }

        private class AzureMapsReverseGeocodeResponse
        {
            public List<ReverseGeocodeAddress> Addresses { get; set; } = new List<ReverseGeocodeAddress>();
        }

        private class ReverseGeocodeAddress
        {
            public ReverseAddress Address { get; set; } = new ReverseAddress();
        }

        private class ReverseAddress
        {
            public string StreetName { get; set; } = string.Empty;
            public string Municipality { get; set; } = string.Empty;
            public string CountrySubdivision { get; set; } = string.Empty;
            public string PostalCode { get; set; } = string.Empty;
            public string CountryCode { get; set; } = string.Empty;
            public string FreeformAddress { get; set; } = string.Empty;
        }

        private class AzureMapsRouteResponse
        {
            public List<RouteItem> Routes { get; set; } = new List<RouteItem>();
        }

        private class RouteItem
        {
            public RouteSummary Summary { get; set; } = new RouteSummary();
        }

        private class RouteSummary
        {
            public double LengthInMeters { get; set; }
            public double TravelTimeInSeconds { get; set; }
        }
    }
}
