// <copyright file="TrackingInfo.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Domain.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents tracking information for a job including location updates and status.
    /// </summary>
    public class TrackingInfo
    {
        /// <summary>
        /// Gets or sets the list of location updates for this job.
        /// </summary>
        public List<LocationUpdate> Updates { get; set; } = new List<LocationUpdate>();

        /// <summary>
        /// Gets or sets the current status of the tracking.
        /// </summary>
        public string CurrentStatus { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp of the last update.
        /// </summary>
        public DateTime? LastUpdateTime { get; set; }

        /// <summary>
        /// Gets or sets the estimated time of arrival.
        /// </summary>
        public DateTime? EstimatedArrival { get; set; }

        /// <summary>
        /// Gets or sets the total distance traveled in miles.
        /// </summary>
        public decimal TotalDistance { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether tracking is currently active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets the current location based on the latest update.
        /// </summary>
        public LocationUpdate? CurrentLocation => Updates
            .OrderByDescending(u => u.Timestamp)
            .FirstOrDefault();

        /// <summary>
        /// Gets the number of location updates.
        /// </summary>
        public int UpdateCount => Updates.Count;

        /// <summary>
        /// Adds a new location update to the tracking information.
        /// </summary>
        /// <param name="update">The location update to add.</param>
        public void AddUpdate(LocationUpdate update)
        {
            if (update == null || !update.IsValid())
                return;

            Updates.Add(update);
            LastUpdateTime = update.Timestamp;

            // Calculate distance if we have a previous update
            if (Updates.Count > 1)
            {
                var previousUpdate = Updates[Updates.Count - 2];
                var distance = (decimal)previousUpdate.DistanceTo(update);
                TotalDistance += distance;
            }

            // Sort updates by timestamp to maintain order
            Updates = Updates.OrderBy(u => u.Timestamp).ToList();
        }

        /// <summary>
        /// Gets location updates within a specific time range.
        /// </summary>
        /// <param name="startTime">The start time.</param>
        /// <param name="endTime">The end time.</param>
        /// <returns>A list of location updates within the time range.</returns>
        public List<LocationUpdate> GetUpdatesInRange(DateTime startTime, DateTime endTime)
        {
            return Updates
                .Where(u => u.Timestamp >= startTime && u.Timestamp <= endTime)
                .OrderBy(u => u.Timestamp)
                .ToList();
        }

        /// <summary>
        /// Gets the latest location updates up to a specified count.
        /// </summary>
        /// <param name="count">The maximum number of updates to return.</param>
        /// <returns>The latest location updates.</returns>
        public List<LocationUpdate> GetLatestUpdates(int count = 10)
        {
            return Updates
                .OrderByDescending(u => u.Timestamp)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Calculates the average speed based on location updates.
        /// </summary>
        /// <returns>The average speed in miles per hour, or null if insufficient data.</returns>
        public decimal? GetAverageSpeed()
        {
            var updatesWithSpeed = Updates.Where(u => u.Speed.HasValue).ToList();

            if (!updatesWithSpeed.Any())
                return null;

            return updatesWithSpeed.Average(u => u.Speed!.Value);
        }

        /// <summary>
        /// Gets the time elapsed since tracking started.
        /// </summary>
        /// <returns>The elapsed time, or null if no updates exist.</returns>
        public TimeSpan? GetElapsedTime()
        {
            if (!Updates.Any())
                return null;

            var firstUpdate = Updates.OrderBy(u => u.Timestamp).First();
            var lastUpdate = Updates.OrderByDescending(u => u.Timestamp).First();

            return lastUpdate.Timestamp - firstUpdate.Timestamp;
        }

        /// <summary>
        /// Checks if the tracking has been inactive for a specified duration.
        /// </summary>
        /// <param name="inactiveThreshold">The threshold for considering tracking inactive.</param>
        /// <returns>True if tracking is inactive, false otherwise.</returns>
        public bool IsInactive(TimeSpan inactiveThreshold)
        {
            if (!LastUpdateTime.HasValue)
                return true;

            return DateTime.UtcNow - LastUpdateTime.Value > inactiveThreshold;
        }

        /// <summary>
        /// Clears all tracking updates and resets the tracking information.
        /// </summary>
        public void Reset()
        {
            Updates.Clear();
            CurrentStatus = string.Empty;
            LastUpdateTime = null;
            EstimatedArrival = null;
            TotalDistance = 0;
            IsActive = false;
        }
    }
}
