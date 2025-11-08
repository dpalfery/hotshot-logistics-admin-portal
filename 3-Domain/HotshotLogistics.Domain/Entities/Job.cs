// <copyright file="Job.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>


using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.ValueObjects;

namespace HotshotLogistics.Domain.Entities
{

    /// <summary>
    /// Represents a job in the logistics system.
    /// </summary>
    public class Job
    {
        /// <summary>
        /// Gets or sets the unique identifier for the job.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the customer identifier for the job.
        /// </summary>
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the title of the job.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the pickup location for the job.
        /// </summary>
        public Location PickupLocation { get; set; } = new Location();

        /// <summary>
        /// Gets or sets the delivery location for the job.
        /// </summary>
        public Location DeliveryLocation { get; set; } = new Location();

        /// <summary>
        /// Gets or sets the cargo details for the job.
        /// </summary>
        public CargoDetails Cargo { get; set; } = new CargoDetails();

        /// <summary>
        /// Gets or sets the current status of the job.
        /// </summary>
        public JobStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the priority level of the job.
        /// </summary>
        public JobPriority Priority { get; set; }

        /// <summary>
        /// Gets or sets the pricing details for the job.
        /// </summary>
        public PricingDetails Pricing { get; set; } = new PricingDetails();

        /// <summary>
        /// Gets or sets the total amount for the job.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Gets or sets the scheduled pickup time for the job.
        /// </summary>
        public DateTime ScheduledPickupTime { get; set; }

        /// <summary>
        /// Gets or sets the estimated delivery time for the job.   
        /// </summary>
        public DateTime EstimatedDeliveryTime { get; set; }

        /// <summary>
        /// Gets or sets the actual pickup time for the job.
        /// </summary>
        public DateTime? ActualPickupTime { get; set; }

        /// <summary>
        /// Gets or sets the actual delivery time for the job.
        /// </summary>
        public DateTime? ActualDeliveryTime { get; set; }

        /// <summary>
        /// Gets or sets the special instructions for the job.
        /// </summary>
        public string SpecialInstructions { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the assigned driver.
        /// </summary>
        public int? AssignedDriverId { get; set; }

        /// <summary>
        /// Gets or sets the list of documents associated with the job.
        /// </summary>
        public List<JobDocument> Documents { get; set; } = new List<JobDocument>();

        /// <summary>
        /// Gets or sets the tracking information for the job.
        /// </summary>
        public TrackingInfo Tracking { get; set; } = new TrackingInfo();

        /// <summary>
        /// Gets or sets the creation timestamp of the job.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last update timestamp of the job.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Job"/> class.
        /// </summary>
        public Job()
        {
            if (CreatedAt == default)
                CreatedAt = DateTime.UtcNow;

            if (ScheduledPickupTime == default)
                ScheduledPickupTime = DateTime.UtcNow.AddHours(2); // Default to 2 hours from now

            if (EstimatedDeliveryTime == default)
                EstimatedDeliveryTime = DateTime.UtcNow.AddHours(4); // Default to 4 hours from now
        }

        /// <summary>
        /// Calculates the total pricing for the job.
        /// </summary>
        /// <param name="distance">The distance in miles.</param>
        /// <param name="fuelSurcharge">The fuel surcharge percentage.</param>
        public void CalculatePricing(double distance, decimal fuelSurcharge = 0)
        {
            Pricing.TotalAmount = Pricing.BaseRate + (Pricing.MileageRate * (decimal)distance) + Pricing.FuelSurcharge + Pricing.TollCharges + Pricing.AdditionalCharges;
        }

        /// <summary>
        /// Updates the job status and tracking information.
        /// </summary>
        /// <param name="newStatus">The new status.</param>
        /// <param name="locationUpdate">Optional location update.</param>
        public void UpdateStatus(JobStatus newStatus, LocationUpdate? locationUpdate = null)
        {
            Status = newStatus;
            UpdatedAt = DateTime.UtcNow;

            if (locationUpdate != null)
            {
                Tracking.Updates.Add(locationUpdate);
                Tracking.LastUpdateTime = locationUpdate.Timestamp;
                Tracking.CurrentStatus = newStatus.ToString();
            }
        }

        /// <summary>
        /// Adds a document to the job.
        /// </summary>
        /// <param name="document">The document to add.</param>
        public void AddDocument(JobDocument document)
        {
            Documents.Add(document);
        }

        /// <summary>
        /// Removes a document from the job.
        /// </summary>
        /// <param name="documentId">The ID of the document to remove.</param>
        public void RemoveDocument(string documentId)
        {
            var document = Documents.FirstOrDefault(d => d.Id == documentId);
            if (document != null)
            {
                Documents.Remove(document);
            }
        }

        /// <summary>
        /// Gets the current location based on the latest tracking update.
        /// </summary>
        /// <returns>The current location, or null if no updates exist.</returns>
        public LocationUpdate? GetCurrentLocation()
        {
            return Tracking.Updates
                .OrderByDescending(u => u.Timestamp)
                .FirstOrDefault();
        }

        /// <summary>
        /// Validates the job data.
        /// </summary>
        /// <returns>True if the job data is valid, false otherwise.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Title) &&
                   !string.IsNullOrWhiteSpace(CustomerId) &&
                   !string.IsNullOrWhiteSpace(Id) &&
                   PickupLocation != null &&
                   DeliveryLocation != null &&
                   Cargo != null &&
                   Pricing != null &&
                   ScheduledPickupTime > DateTime.UtcNow &&
                   EstimatedDeliveryTime > ScheduledPickupTime;
        }

        /// <summary>
        /// Gets the estimated time remaining based on current progress.
        /// </summary>
        /// <returns>The estimated time remaining.</returns>
        public TimeSpan GetEstimatedTimeRemaining()
        {
            var currentLocation = GetCurrentLocation();
            if (currentLocation == null)
            {
                return EstimatedDeliveryTime - DateTime.UtcNow;
            }

            // Simple estimation based on current progress
            var totalDuration = EstimatedDeliveryTime - ScheduledPickupTime;
            var elapsed = DateTime.UtcNow - ScheduledPickupTime;

            if (elapsed >= totalDuration)
                return TimeSpan.Zero;

            return totalDuration - elapsed;
        }
    }
}
