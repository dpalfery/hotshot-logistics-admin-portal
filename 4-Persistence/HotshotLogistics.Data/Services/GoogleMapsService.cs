// <copyright file="GoogleMapsService.cs" company="PlaceholderCompany">
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
    using HotshotLogistics.Domain.DTOs;
    using HotshotLogistics.Domain.Entities;
    using HotshotLogistics.Domain.ValueObjects;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Google Maps implementation of the mapping service.
    /// </summary>
    public class GoogleMapsService : IMappingService
    {
        private readonly HttpClient httpClient;
        private readonly ILogger<GoogleMapsService> logger;
        private readonly string apiKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleMapsService"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client for API calls.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="settings">The Google Maps settings.</param>
        public GoogleMapsService(HttpClient httpClient, ILogger<GoogleMapsService> logger, IOptions<GoogleMapsSettings> settings)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var googleMapsSettings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            this.apiKey = googleMapsSettings.ApiKey ?? throw new ArgumentNullException(nameof(googleMapsSettings.ApiKey));
        }

        /// <inheritdoc/>
        public async Task<GeocodingResult> GeocodeAddressAsync(string address, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&key={apiKey}";

                var response = await httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<GoogleMapsGeocodeResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Status == "OK" && result.Results?.Count > 0)
                {
                    var firstResult = result.Results[0];
                    var location = firstResult.Geometry.Location;

                    return new GeocodingResult
                    {
                        Latitude = (decimal)location.Lat,
                        Longitude = (decimal)location.Lng,
                        FormattedAddress = firstResult.FormattedAddress,
                        IsValid = true,
                        Confidence = 1.0, // Google Maps doesn't provide confidence scores
                    };
                }

                return new GeocodingResult
                {
                    IsValid = false,
                    ErrorMessage = result?.Status ?? "Geocoding failed",
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
                var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={latitude},{longitude}&key={apiKey}";

                var response = await httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<GoogleMapsGeocodeResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Status == "OK" && result.Results?.Count > 0)
                {
                    var firstResult = result.Results[0];
                    var components = firstResult.AddressComponents;

                    return new ReverseGeocodingResult
                    {
                        Address = GetAddressComponent(components, "street_number") + " " + GetAddressComponent(components, "route"),
                        City = GetAddressComponent(components, "locality"),
                        State = GetAddressComponent(components, "administrative_area_level_1"),
                        PostalCode = GetAddressComponent(components, "postal_code"),
                        Country = GetAddressComponent(components, "country"),
                        FormattedAddress = firstResult.FormattedAddress,
                        IsValid = true,
                    };
                }

                return new ReverseGeocodingResult
                {
                    IsValid = false,
                    ErrorMessage = result?.Status ?? "Reverse geocoding failed",
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
            return result.IsValid;
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

                var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={origin.Latitude},{origin.Longitude}&destination={destination.Latitude},{destination.Longitude}&key={apiKey}";

                var response = await httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<GoogleMapsDirectionsResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Status == "OK" && result.Routes?.Count > 0)
                {
                    var route = result.Routes[0];
                    var leg = route.Legs[0];

                    return new RouteResult
                    {
                        Distance = leg.Distance.Value * 0.000621371, // Convert meters to miles
                        Duration = TimeSpan.FromSeconds(leg.Duration.Value),
                        Waypoints = new List<Location> { origin, destination },
                        Polyline = route.OverviewPolyline?.Points ?? string.Empty,
                        EstimatedArrival = DateTime.UtcNow.AddSeconds(leg.Duration.Value),
                        IsValid = true,
                    };
                }

                return new RouteResult
                {
                    IsValid = false,
                    ErrorMessage = result?.Status ?? "No route found",
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
            // In a real implementation, this would use Google Maps Directions API with waypoints optimization
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

        private static string GetAddressComponent(List<AddressComponent> components, string type)
        {
            var component = components?.Find(c => c.Types.Contains(type));
            return component?.LongName ?? string.Empty;
        }

        // Internal classes for Google Maps API responses
        private class GoogleMapsGeocodeResponse
        {
            public string Status { get; set; } = string.Empty;
            public List<GeocodeResult> Results { get; set; } = new List<GeocodeResult>();
        }

        private class GeocodeResult
        {
            [System.Text.Json.Serialization.JsonPropertyName("formatted_address")]
            public string FormattedAddress { get; set; } = string.Empty;
            public Geometry Geometry { get; set; } = new Geometry();
            public List<AddressComponent> AddressComponents { get; set; } = new List<AddressComponent>();
        }

        private class Geometry
        {
            [System.Text.Json.Serialization.JsonPropertyName("location")]
            public Coordinate Location { get; set; } = new Coordinate();
        }

        private class Coordinate
        {
            [System.Text.Json.Serialization.JsonPropertyName("lat")]
            public double Lat { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("lng")]
            public double Lng { get; set; }
        }

        private class AddressComponent
        {
            public string LongName { get; set; } = string.Empty;
            public List<string> Types { get; set; } = new List<string>();
        }

        private class GoogleMapsDirectionsResponse
        {
            public string Status { get; set; } = string.Empty;
            public List<DirectionsRoute> Routes { get; set; } = new List<DirectionsRoute>();
        }

        private class DirectionsRoute
        {
            public List<DirectionsLeg> Legs { get; set; } = new List<DirectionsLeg>();
            [System.Text.Json.Serialization.JsonPropertyName("overview_polyline")]
            public Polyline OverviewPolyline { get; set; } = new Polyline();
        }

        private class DirectionsLeg
        {
            public Distance Distance { get; set; } = new Distance();
            public Duration Duration { get; set; } = new Duration();
        }

        private class Distance
        {
            [System.Text.Json.Serialization.JsonPropertyName("value")]
            public int Value { get; set; } // meters
        }

        private class Duration
        {
            [System.Text.Json.Serialization.JsonPropertyName("value")]
            public int Value { get; set; } // seconds
        }

        private class Polyline
        {
            [System.Text.Json.Serialization.JsonPropertyName("points")]
            public string Points { get; set; } = string.Empty;
        }
    }
}
