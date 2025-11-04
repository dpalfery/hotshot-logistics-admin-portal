// <copyright file="LocationTracking.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>


namespace HotshotLogistics.Domain.Entities
{

    /// <summary>
    /// Represents a location tracking record in the system.
    /// </summary>
    public class LocationTracking 
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LocationTracking"/> class.
        /// </summary>
        public LocationTracking()
        {
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationTracking"/> class with coordinates.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="driverId">The driver identifier.</param>
        /// <param name="latitude">The latitude coordinate.</param>
        /// <param name="longitude">The longitude coordinate.</param>
        public LocationTracking(string jobId, int driverId, decimal latitude, decimal longitude)
        {
            JobId = jobId;
            DriverId = driverId;
            Latitude = latitude;
            Longitude = longitude;
            Timestamp = DateTime.UtcNow;
        }

        /// <inheritdoc/>
        public long Id { get; set; }

        /// <inheritdoc/>
        public string JobId { get; set; } = string.Empty;

        /// <inheritdoc/>
        public int DriverId { get; set; }

        /// <inheritdoc/>
        public decimal Latitude { get; set; }

        /// <inheritdoc/>
        public decimal Longitude { get; set; }

        /// <inheritdoc/>
        public decimal? Speed { get; set; }

        /// <inheritdoc/>
        public int? Heading { get; set; }

        /// <inheritdoc/>
        public decimal? Accuracy { get; set; }

        /// <inheritdoc/>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Creates a LocationTracking instance from a LocationUpdate.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="driverId">The driver identifier.</param>
        /// <param name="locationUpdate">The location update.</param>
        /// <returns>A new LocationTracking instance.</returns>
        public static LocationTracking FromLocationUpdate(string jobId, int driverId, LocationUpdate locationUpdate)
        {
            if (locationUpdate == null)
                throw new ArgumentNullException(nameof(locationUpdate));

            return new LocationTracking
            {
                JobId = jobId,
                DriverId = driverId,
                Latitude = locationUpdate.Latitude,
                Longitude = locationUpdate.Longitude,
                Speed = locationUpdate.Speed,
                Heading = locationUpdate.Heading,
                Accuracy = locationUpdate.Accuracy,
                Timestamp = locationUpdate.Timestamp
            };
        }

        /// <summary>
        /// Converts this LocationTracking to a LocationUpdate.
        /// </summary>
        /// <returns>A LocationUpdate instance.</returns>
        public LocationUpdate ToLocationUpdate()
        {
            return new LocationUpdate(Latitude, Longitude)
            {
                Speed = Speed,
                Heading = Heading,
                Accuracy = Accuracy,
                Timestamp = Timestamp
            };
        }

        /// <summary>
        /// Calculates the distance to another location tracking record.
        /// </summary>
        /// <param name="other">The other location tracking record.</param>
        /// <returns>The distance in miles.</returns>
        public double DistanceTo(LocationTracking other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

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
        /// Validates the location tracking data.
        /// </summary>
        /// <returns>True if the location tracking is valid, false otherwise.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(JobId) &&
                   DriverId > 0 &&
                   Latitude >= -90 && Latitude <= 90 &&
                   Longitude >= -180 && Longitude <= 180 &&
                   Timestamp != default &&
                   (!Speed.HasValue || Speed >= 0) &&
                   (!Heading.HasValue || (Heading >= 0 && Heading <= 359)) &&
                   (!Accuracy.HasValue || Accuracy >= 0);
        }
    }
}
