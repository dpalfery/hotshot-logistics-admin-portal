using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;

namespace HotshotLogistics.Tests.Utils.TestHelpers
{
    // A lightweight mapping service used only by integration tests.
    // The type name intentionally ends with "Service" so MappingServiceFactory will map it to provider name "Mock".
    public class MockService : IMappingService
    {
        public Task<GeocodingResult> GeocodeAddressAsync(string address, CancellationToken cancellationToken = default)
        {
            var result = new GeocodingResult
            {
                Latitude = 40.0m,
                Longitude = -75.0m,
                FormattedAddress = address,
                IsValid = !string.IsNullOrWhiteSpace(address),
                Confidence = 0.9
            };
            return Task.FromResult(result);
        }

        public Task<ReverseGeocodingResult> ReverseGeocodeAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
        {
            var result = new ReverseGeocodingResult
            {
                Address = "123 Test St",
                City = "Testville",
                State = "TS",
                PostalCode = "00000",
                Country = "US",
                FormattedAddress = "123 Test St, Testville, TS 00000",
                IsValid = true
            };
            return Task.FromResult(result);
        }

        public Task<bool> ValidateAddressAsync(string address, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(!string.IsNullOrWhiteSpace(address));
        }

        public Task<RouteResult> CalculateRouteAsync(Location origin, Location destination, CancellationToken cancellationToken = default)
        {
            if (origin == null || destination == null || !origin.HasCoordinates || !destination.HasCoordinates)
            {
                return Task.FromResult(new RouteResult { IsValid = false, ErrorMessage = "Invalid locations" });
            }

            var distance = 10.0; // mock
            var duration = TimeSpan.FromMinutes(15);
            var route = new RouteResult
            {
                Distance = distance,
                Duration = duration,
                Waypoints = new List<Location> { origin, destination },
                Polyline = "mock",
                EstimatedArrival = DateTime.UtcNow.Add(duration),
                IsValid = true
            };
            return Task.FromResult(route);
        }

        public Task<OptimizedRouteResult> OptimizeRouteAsync(IList<Location> waypoints, CancellationToken cancellationToken = default)
        {
            if (waypoints == null || waypoints.Count < 2)
            {
                return Task.FromResult(new OptimizedRouteResult { IsValid = false, ErrorMessage = "Need at least 2 waypoints" });
            }

            double totalDistance = 0;
            TimeSpan totalDuration = TimeSpan.Zero;
            var segments = new List<RouteResult>();

            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                var seg = new RouteResult { Distance = 5.0, Duration = TimeSpan.FromMinutes(7), IsValid = true };
                segments.Add(seg);
                totalDistance += seg.Distance;
                totalDuration = totalDuration.Add(seg.Duration);
            }

            var optimized = new OptimizedRouteResult
            {
                OptimizedWaypoints = waypoints,
                TotalDistance = totalDistance,
                TotalDuration = totalDuration,
                RouteSegments = segments,
                EstimatedArrival = DateTime.UtcNow.Add(totalDuration),
                IsValid = true
            };

            return Task.FromResult(optimized);
        }

        public Task<DistanceResult> CalculateDistanceAsync(Location origin, Location destination, CancellationToken cancellationToken = default)
        {
            var route = new RouteResult { Distance = 10.0, Duration = TimeSpan.FromMinutes(15), IsValid = true };
            var result = new DistanceResult { Distance = route.Distance, Duration = route.Duration, IsValid = true };
            return Task.FromResult(result);
        }
    }
}
