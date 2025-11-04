// <copyright file="LocationUpdate.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Domain.Entities
{
    using System;

    /// <summary>
    /// Represents a GPS location update from a driver during job execution.
    /// </summary>
    public class LocationUpdate
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
        /// Gets or sets the speed in miles per hour.
        /// </summary>
        public decimal? Speed { get; set; }

        /// <summary>
        /// Gets or sets the heading/direction in degrees (0-359).
        /// </summary>
        public int? Heading { get; set; }

        /// <summary>
        /// Gets or sets the GPS accuracy in meters.
        /// </summary>
        public decimal? Accuracy { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this location was recorded.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets any status message associated with this update.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets any notes or comments for this location update.
        /// </summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationUpdate"/> class.
        /// </summary>
        public LocationUpdate()
        {
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationUpdate"/> class with coordinates.
        /// </summary>
        /// <param name="latitude">The latitude coordinate.</param>
        /// <param name="longitude">The longitude coordinate.</param>
        public LocationUpdate(decimal latitude, decimal longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Calculates the distance to another location update.
        /// </summary>
        /// <param name="other">The other location update.</param>
        /// <returns>The distance in miles.</returns>
        public double DistanceTo(LocationUpdate other)
        {
            const double earthRadiusMiles = 3959.0;

            var lat1Rad = (double)(Latitude * (decimal)Math.PI / 180);
            var lat2Rad = (double)(other.Latitude * (decimal)Math.PI / 180);
            var deltaLatRad = (double)((other.Latitude - Latitude) * (decimal)Math.PI / 180);
            var deltaLonRad = (double)((other.Longitude - Longitude) * (decimal)Math.PI / 180);

            var a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusMiles * c;
        }

        /// <summary>
        /// Calculates the distance to a location.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns>The distance in miles, or null if location has no coordinates.</returns>
        public double? DistanceTo(Location location)
        {
            if (!location.HasCoordinates)
                return null;

            const double earthRadiusMiles = 3959.0;

            var lat1Rad = (double)(Latitude * (decimal)Math.PI / 180);
            var lat2Rad = (double)(location.Latitude!.Value * (decimal)Math.PI / 180);
            var deltaLatRad = (double)((location.Latitude!.Value - Latitude) * (decimal)Math.PI / 180);
            var deltaLonRad = (double)((location.Longitude!.Value - Longitude) * (decimal)Math.PI / 180);

            var a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusMiles * c;
        }

        /// <summary>
        /// Validates the location update data.
        /// </summary>
        /// <returns>True if the location update is valid, false otherwise.</returns>
        public bool IsValid()
        {
            return Latitude >= -90 && Latitude <= 90 &&
                   Longitude >= -180 && Longitude <= 180 &&
                   Timestamp != default &&
                   (!Speed.HasValue || Speed >= 0) &&
                   (!Heading.HasValue || (Heading >= 0 && Heading <= 359)) &&
                   (!Accuracy.HasValue || Accuracy >= 0);
        }
    }
}
