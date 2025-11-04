// <copyright file="RouteResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Domain.DTOs
{
    using System;
    using System.Collections.Generic;
    using HotshotLogistics.Domain.Entities;

    /// <summary>
    /// Represents the result of a route calculation between two locations.
    /// </summary>
    public class RouteResult
    {
        /// <summary>
        /// Gets or sets the total distance of the route in miles.
        /// </summary>
        public double Distance { get; set; }

        /// <summary>
        /// Gets or sets the estimated travel time.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets or sets the list of waypoints that make up the route.
        /// </summary>
        public IList<Location> Waypoints { get; set; } = new List<Location>();

        /// <summary>
        /// Gets or sets the encoded polyline representing the route path.
        /// </summary>
        public string Polyline { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the estimated time of arrival based on current time.
        /// </summary>
        public DateTime EstimatedArrival { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the route calculation was successful.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets any error message if the route calculation failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
