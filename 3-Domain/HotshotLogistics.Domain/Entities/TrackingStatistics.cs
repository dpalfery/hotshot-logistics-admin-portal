// <copyright file="TrackingStatistics.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Domain.Entities
{
    using System;

    /// <summary>
    /// Represents tracking statistics for a job.
    /// </summary>
    public class TrackingStatistics
    {
        /// <summary>
        /// Gets or sets the job identifier.
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total number of location updates.
        /// </summary>
        public int TotalUpdates { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the first location update.
        /// </summary>
        public DateTime? FirstUpdate { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last location update.
        /// </summary>
        public DateTime? LastUpdate { get; set; }

        /// <summary>
        /// Gets or sets the total distance traveled in miles.
        /// </summary>
        public decimal TotalDistance { get; set; }

        /// <summary>
        /// Gets or sets the total duration of tracking.
        /// </summary>
        public TimeSpan? TotalDuration { get; set; }

        /// <summary>
        /// Gets or sets the average speed in miles per hour.
        /// </summary>
        public decimal? AverageSpeed { get; set; }

        /// <summary>
        /// Gets or sets the maximum speed recorded in miles per hour.
        /// </summary>
        public decimal? MaxSpeed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether tracking is currently active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets the average speed calculated from distance and duration.
        /// </summary>
        public decimal? CalculatedAverageSpeed
        {
            get
            {
                if (!TotalDuration.HasValue || TotalDuration.Value.TotalHours == 0 || TotalDistance == 0)
                    return null;

                return TotalDistance / (decimal)TotalDuration.Value.TotalHours;
            }
        }

        /// <summary>
        /// Gets the update frequency in updates per hour.
        /// </summary>
        public decimal? UpdateFrequency
        {
            get
            {
                if (!TotalDuration.HasValue || TotalDuration.Value.TotalHours == 0 || TotalUpdates == 0)
                    return null;

                return TotalUpdates / (decimal)TotalDuration.Value.TotalHours;
            }
        }

        /// <summary>
        /// Gets a summary of the tracking statistics.
        /// </summary>
        /// <returns>A formatted summary string.</returns>
        public string GetSummary()
        {
            var summary = $"Job {JobId}: {TotalUpdates} updates";

            if (TotalDistance > 0)
            {
                summary += $", {TotalDistance:F1} miles traveled";
            }

            if (AverageSpeed.HasValue)
            {
                summary += $", avg speed {AverageSpeed:F1} mph";
            }

            if (TotalDuration.HasValue)
            {
                summary += $", duration {TotalDuration.Value:hh\\:mm\\:ss}";
            }

            summary += IsActive ? " (Active)" : " (Inactive)";

            return summary;
        }
    }
}
