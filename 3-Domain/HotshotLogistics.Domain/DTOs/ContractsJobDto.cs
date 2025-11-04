using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;

namespace HotshotLogistics.Domain.DTOs
{
    /// <summary>
    /// Data transfer object for job information.
    /// </summary>
    public class ContractsJobDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the job.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the title of the job.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the pickup address for the job.
        /// </summary>
        public string PickupAddress { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the dropoff address for the job.
        /// </summary>
        public string DropoffAddress { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current status of the job.
        /// </summary>
        public JobStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the monetary amount for the job.
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
        /// Gets or sets the customer identifier.
        /// </summary>
        public string CustomerId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the pickup location.
        /// </summary>
        public Location? PickupLocation { get; set; }
        
        /// <summary>
        /// Gets or sets the delivery location.
        /// </summary>
        public Location? DeliveryLocation { get; set; }
        
        /// <summary>
        /// Gets or sets the cargo details.
        /// </summary>
        public CargoDetails? Cargo { get; set; }
        
        /// <summary>
        /// Gets or sets the pricing details.
        /// </summary>
        public PricingDetails? Pricing { get; set; }
        
        /// <summary>
        /// Gets or sets the special instructions.
        /// </summary>
        public string? SpecialInstructions { get; set; }
    }
}
