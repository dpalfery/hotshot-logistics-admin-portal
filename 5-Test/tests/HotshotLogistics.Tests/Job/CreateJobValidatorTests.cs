using FluentValidation.TestHelper;
using HotshotLogistics.Application.Validators;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;

namespace HotshotLogistics.Tests.Job
{
    /// <summary>
/// Unit tests for CreateJobValidator.
/// </summary>
public class CreateJobValidatorTests
{
    private readonly CreateJobValidator validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateJobValidatorTests"/> class.
    /// </summary>
    public CreateJobValidatorTests()
    {
        this.validator = new CreateJobValidator();
    }

    /// <summary>
    /// Tests that validation passes for a valid job DTO.
    /// </summary>
    [Fact]
    public void Validate_ValidJobDto_ShouldPass()
    {
        // Arrange
        var jobDto = new Domain.Entities.JobDto
        {
            Title = "Test Job",
            PickupAddress = "123 Main St, City, ST 12345",
            DropoffAddress = "456 Oak Ave, City, ST 12345",
            Amount = 500.00m,
            ScheduledPickupTime = DateTime.UtcNow.AddHours(2),
            CustomerId = "customer-123",
            PickupLocation = new Location
            {
                Address = "123 Main St",
                City = "Test City",
                State = "TS",
                PostalCode = "12345",
                Country = "USA",
                Latitude = 40.7128m,
                Longitude = -74.0060m
            },
            DeliveryLocation = new Location
            {
                Address = "456 Oak Ave",
                City = "Test City",
                State = "TS",
                PostalCode = "12345",
                Country = "USA",
                Latitude = 40.7589m,
                Longitude = -73.9851m
            },
            Cargo = new CargoDetails
            {
                Description = "Test cargo",
                Weight = 100.0m,
                Quantity = 1,
                Value = 1000.00m
            },
            Pricing = new PricingDetails
            {
                BaseRate = 400.00m,
                TotalAmount = 500.00m,
                Currency = "USD"
            }
        };

        // Act
        var result = this.validator.TestValidate(jobDto);

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
        var jobDto = new Domain.Entities.JobDto { Title = string.Empty };

        // Act
        var result = this.validator.TestValidate(jobDto);

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
        var jobDto = new Domain.Entities.JobDto { Title = new string('A', 201) };

        // Act
        var result = this.validator.TestValidate(jobDto);

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
        var jobDto = new Domain.Entities.JobDto { PickupAddress = string.Empty };

        // Act
        var result = this.validator.TestValidate(jobDto);

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
        var jobDto = new Domain.Entities.JobDto { Amount = 0 };

        // Act
        var result = this.validator.TestValidate(jobDto);

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
        var jobDto = new Domain.Entities.JobDto { Amount = 150000.00m };

        // Act
        var result = this.validator.TestValidate(jobDto);

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
        var jobDto = new Domain.Entities.JobDto { ScheduledPickupTime = DateTime.UtcNow.AddHours(-1) };

        // Act
        var result = this.validator.TestValidate(jobDto);

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
        var jobDto = new Domain.Entities.JobDto
        {
            ScheduledPickupTime = pickupTime,
            EstimatedDeliveryTime = pickupTime.AddHours(-1)
        };

        // Act
        var result = this.validator.TestValidate(jobDto);

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
        var jobDto = new Domain.Entities.JobDto { CustomerId = string.Empty };

        // Act
        var result = this.validator.TestValidate(jobDto);

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
        var jobDto = new Domain.Entities.JobDto { PickupLocation = null! };

        // Act
        var result = this.validator.TestValidate(jobDto);

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
        var jobDto = new Domain.Entities.JobDto { Cargo = null! };

        // Act
        var result = this.validator.TestValidate(jobDto);

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
        var jobDto = new Domain.Entities.JobDto { Pricing = null! };

        // Act
        var result = this.validator.TestValidate(jobDto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Pricing)
              .WithErrorMessage("Pricing details are required.");
    }
}
}
