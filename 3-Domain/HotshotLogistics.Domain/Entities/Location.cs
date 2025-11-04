// <copyright file="Location.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Domain.Entities
{
    using System;

    /// <summary>
    /// Represents a geographical location with address and coordinates.
    /// </summary>
    public class Location
    {
        /// <summary>
        /// Gets or sets the street address.
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the city.
        /// </summary>
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the state or province.
        /// </summary>
        public string State { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the postal code.
        /// </summary>
        public string PostalCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the country.
        /// </summary>
        public string Country { get; set; } = "USA";

        /// <summary>
        /// Gets or sets the latitude coordinate.
        /// </summary>
        public decimal? Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude coordinate.
        /// </summary>
        public decimal? Longitude { get; set; }

        /// <summary>
        /// Gets or sets any special instructions for this location.
        /// </summary>
        public string Instructions { get; set; } = string.Empty;

        /// <summary>
        /// Gets the full address as a formatted string.
        /// </summary>
        public string FullAddress => $"{Address}, {City}, {State} {PostalCode}, {Country}".Trim(' ', ',');

        /// <summary>
        /// Gets a value indicating whether this location has valid coordinates.
        /// </summary>
        public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;

        /// <summary>
        /// Calculates the distance to another location using the Haversine formula.
        /// </summary>
        /// <param name="other">The other location.</param>
        /// <returns>The distance in miles, or null if either location lacks coordinates.</returns>
        public double? DistanceTo(Location other)
        {
            if (!HasCoordinates || !other.HasCoordinates)
                return null;

            const double earthRadiusMiles = 3959.0;

            var lat1Rad = (double)(Latitude!.Value * (decimal)Math.PI / 180);
            var lat2Rad = (double)(other.Latitude!.Value * (decimal)Math.PI / 180);
            var deltaLatRad = (double)((other.Latitude!.Value - Latitude!.Value) * (decimal)Math.PI / 180);
            var deltaLonRad = (double)((other.Longitude!.Value - Longitude!.Value) * (decimal)Math.PI / 180);

            var a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusMiles * c;
        }

        /// <summary>
        /// Validates the location data.
        /// </summary>
        /// <returns>True if the location is valid, false otherwise.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Address) &&
                   !string.IsNullOrWhiteSpace(City) &&
                   !string.IsNullOrWhiteSpace(State) &&
                   !string.IsNullOrWhiteSpace(PostalCode) &&
                   (!Latitude.HasValue || (Latitude >= -90 && Latitude <= 90)) &&
                   (!Longitude.HasValue || (Longitude >= -180 && Longitude <= 180));
        }
    }
}
