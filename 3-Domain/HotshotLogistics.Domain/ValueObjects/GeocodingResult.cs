// <copyright file="GeocodingResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Domain.ValueObjects
{
    /// <summary>
    /// Represents the result of a geocoding operation.
    /// </summary>
    public class GeocodingResult
    {
        /// <summary>
        /// Gets or sets the latitude coordinate.
        /// </summary>
        public decimal Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude coordinate.
        /// </summary>
        public decimal Longitude { get; set; }

        /// <summary>
        /// Gets or sets the formatted address returned by the geocoding service.
        /// </summary>
        public string FormattedAddress { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the geocoding was successful.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the confidence level of the geocoding result (0-1).
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Gets or sets any error message if the geocoding failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
