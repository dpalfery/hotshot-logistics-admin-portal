// <copyright file="DistanceResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Domain.ValueObjects
{
    using System;

    /// <summary>
    /// Represents the result of a distance calculation between two locations.
    /// </summary>
    public class DistanceResult
    {
        /// <summary>
        /// Gets or sets the distance in miles.
        /// </summary>
        public double Distance { get; set; }

        /// <summary>
        /// Gets or sets the estimated travel time.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the distance calculation was successful.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets any error message if the distance calculation failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}


