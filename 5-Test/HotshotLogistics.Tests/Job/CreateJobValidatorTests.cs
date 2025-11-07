using System;
using FluentValidation.TestHelper;
using HotshotLogistics.Application.Validators;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Domain.ValueObjects;
using HotshotLogistics.Domain.Entities;
using Xunit;

namespace HotshotLogistics.Tests.Job
{
    /// <summary>
    /// Unit tests for CreateJobValidator.
    /// </summary>
    public class CreateJobValidatorTests
    {
        private readonly CreateJobValidator validator = new CreateJobValidator();

        /// <summary>
        /// Tests that validation passes for a valid job DTO.
        /// </summary>
        [Fact]
        public void Validate_ValidJobDto_ShouldPass()
        {
            // Arrange
            var jobDto = new ContractsJobDto
            {
                Title = "Delivery",
                PickupAddress = "123 Main St",
                DropoffAddress = "456 Oak Ave",
                Amount = 100m,
                ScheduledPickupTime = DateTime.UtcNow.AddHours(2),
                EstimatedDeliveryTime = DateTime.UtcNow.AddHours(5),
                CustomerId = "CUST-1",
                PickupLocation = new Location { Address = "123 Main St", City = "City", State = "ST", PostalCode = "12345", Latitude = 1m, Longitude = 1m },
                DeliveryLocation = new Location { Address = "456 Oak Ave", City = "City", State = "ST", PostalCode = "67890", Latitude = 2m, Longitude = 2m },
                Cargo = new CargoDetails { Description = "Boxes" },
                Pricing = new PricingDetails { BaseRate = 50m, MileageRate = 1m }
            };

            // Act
            var result = validator.TestValidate(jobDto);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        /// <summary>
        /// Tests that validation fails when title is empty.
        /// </summary>
        [Fact]
        public void Validate_EmptyTitle_ShouldFail()
        {
            // Arrange
            var jobDto = new ContractsJobDto { Title = string.Empty };

            // Act
            var result = validator.TestValidate(jobDto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                  .WithErrorMessage("Job title is required.");
        }

        /// <summary>
        /// Tests that validation fails when title is too long.
        /// </summary>
        [Fact]
        public void Validate_TitleTooLong_ShouldFail()
        {
            // Arrange
            var jobDto = new ContractsJobDto { Title = new string('A', 201) };

            // Act
            var result = validator.TestValidate(jobDto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                  .WithErrorMessage("Job title cannot exceed 200 characters.");
        }

        /// <summary>
        /// Tests that validation fails when pickup address is empty.
        /// </summary>
        [Fact]
        public void Validate_EmptyPickupAddress_ShouldFail()
        {
            // Arrange
            var jobDto = new ContractsJobDto { PickupAddress = string.Empty };

            // Act
            var result = validator.TestValidate(jobDto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.PickupAddress)
                  .WithErrorMessage("Pickup address is required.");
        }

        /// <summary>
        /// Tests that validation fails when amount is zero.
        /// </summary>
        [Fact]
        public void Validate_ZeroAmount_ShouldFail()
        {
            // Arrange
            var jobDto = new ContractsJobDto { Amount = 0 };

            // Act
            var result = validator.TestValidate(jobDto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Amount)
                  .WithErrorMessage("Job amount must be greater than zero.");
        }

        /// <summary>
        /// Tests that validation fails when amount exceeds maximum.
        /// </summary>
        [Fact]
        public void Validate_AmountTooHigh_ShouldFail()
        {
            // Arrange
            var jobDto = new ContractsJobDto { Amount = 200000m };

            // Act
            var result = validator.TestValidate(jobDto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Amount)
                  .WithErrorMessage("Job amount cannot exceed $100,000.");
        }

        /// <summary>
        /// Tests that validation fails when scheduled pickup time is in the past.
        /// </summary>
        [Fact]
        public void Validate_PastPickupTime_ShouldFail()
        {
            // Arrange
            var jobDto = new ContractsJobDto { ScheduledPickupTime = DateTime.UtcNow.AddHours(-1), Status = HotshotLogistics.Core.Enums.JobStatus.Pending };

            // Act
            var result = validator.TestValidate(jobDto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ScheduledPickupTime)
                  .WithErrorMessage("Scheduled pickup time must be in the future.");
        }

        /// <summary>
        /// Tests that validation fails when estimated delivery time is before pickup time.
        /// </summary>
        [Fact]
        public void Validate_EarlyDeliveryTime_ShouldFail()
        {
            // Arrange
            var pickupTime = DateTime.UtcNow.AddHours(2);
            var jobDto = new ContractsJobDto
            {
                ScheduledPickupTime = pickupTime,
                EstimatedDeliveryTime = pickupTime.AddHours(-1)
            };

            // Act
            var result = validator.TestValidate(jobDto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.EstimatedDeliveryTime)
                  .WithErrorMessage("Estimated delivery time must be after scheduled pickup time.");
        }

        /// <summary>
        /// Tests that validation fails when customer ID is empty.
        /// </summary>
        [Fact]
        public void Validate_EmptyCustomerId_ShouldFail()
        {
            // Arrange
            var jobDto = new ContractsJobDto { CustomerId = string.Empty };

            // Act
            var result = validator.TestValidate(jobDto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.CustomerId)
                  .WithErrorMessage("Customer ID is required.");
        }

        /// <summary>
        /// Tests that validation fails when pickup location is null.
        /// </summary>
        [Fact]
        public void Validate_NullPickupLocation_ShouldFail()
        {
            // Arrange
            var jobDto = new ContractsJobDto { PickupLocation = null! };

            // Act
            var result = validator.TestValidate(jobDto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.PickupLocation)
                  .WithErrorMessage("Pickup location is required.");
        }

        /// <summary>
        /// Tests that validation fails when cargo details are null.
        /// </summary>
        [Fact]
        public void Validate_NullCargo_ShouldFail()
        {
            // Arrange
            var jobDto = new ContractsJobDto { Cargo = null! };

            // Act
            var result = validator.TestValidate(jobDto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Cargo)
                  .WithErrorMessage("Cargo details are required.");
        }

        /// <summary>
        /// Tests that validation fails when pricing details are null.
        /// </summary>
        [Fact]
        public void Validate_NullPricing_ShouldFail()
        {
            // Arrange
            var jobDto = new ContractsJobDto { Pricing = null! };

            // Act
            var result = validator.TestValidate(jobDto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Pricing)
                  .WithErrorMessage("Pricing details are required.");
        }
    }
}
