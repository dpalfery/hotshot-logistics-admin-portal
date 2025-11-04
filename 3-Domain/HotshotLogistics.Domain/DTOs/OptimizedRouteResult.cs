// <copyright file="OptimizedRouteResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;
namespace HotshotLogistics.Domain.DTOs
{


    /// <summary>
    /// Represents the result of an optimized route calculation for multiple stops.
    /// </summary>
    public class OptimizedRouteResult
    {
        /// <summary>
        /// Gets or sets the optimized order of waypoints.
        /// </summary>
        public IList<Location> OptimizedWaypoints { get; set; } = new List<Location>();

        /// <summary>
        /// Gets or sets the total distance of the optimized route in miles.
        /// </summary>
        public double TotalDistance { get; set; }

        /// <summary>
        /// Gets or sets the total estimated travel time for the optimized route.
        /// </summary>
        public TimeSpan TotalDuration { get; set; }

        /// <summary>
        /// Gets or sets the list of individual route segments.
        /// </summary>
        public IList<RouteResult> RouteSegments { get; set; } = new List<RouteResult>();

        /// <summary>
        /// Gets or sets the encoded polyline representing the complete optimized route.
        /// </summary>
        public string Polyline { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the estimated time of arrival at the final destination.
        /// </summary>
        public DateTime EstimatedArrival { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the route optimization was successful.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets any error message if the route optimization failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
