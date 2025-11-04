// <copyright file="IMappingService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;
namespace HotshotLogistics.Contracts.Services
{

    /// <summary>
    /// Interface for mapping services providing geocoding, routing, and location validation.
    /// </summary>
    public interface IMappingService
    {
        /// <summary>
        /// Converts an address to geographic coordinates.
        /// </summary>
        /// <param name="address">The address to geocode.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The geocoding result containing coordinates and validation status.</returns>
        Task<GeocodingResult> GeocodeAddressAsync(string address, CancellationToken cancellationToken = default);

        /// <summary>
        /// Converts geographic coordinates to an address.
        /// </summary>
        /// <param name="latitude">The latitude coordinate.</param>
        /// <param name="longitude">The longitude coordinate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The reverse geocoding result containing the address.</returns>
        Task<ReverseGeocodingResult> ReverseGeocodeAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if an address is real and can be geocoded.
        /// </summary>
        /// <param name="address">The address to validate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the address is valid, false otherwise.</returns>
        Task<bool> ValidateAddressAsync(string address, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates the route between two locations.
        /// </summary>
        /// <param name="origin">The starting location.</param>
        /// <param name="destination">The destination location.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The route calculation result containing distance, duration, and route details.</returns>
        Task<RouteResult> CalculateRouteAsync(Location origin, Location destination, CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes a route for multiple stops.
        /// </summary>
        /// <param name="waypoints">The list of locations to visit in order.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The optimized route result with reordered waypoints and total metrics.</returns>
        Task<OptimizedRouteResult> OptimizeRouteAsync(IList<Location> waypoints, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates distance and estimated travel time between two locations.
        /// </summary>
        /// <param name="origin">The starting location.</param>
        /// <param name="destination">The destination location.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The distance calculation result.</returns>
        Task<DistanceResult> CalculateDistanceAsync(Location origin, Location destination, CancellationToken cancellationToken = default);
    }
}
