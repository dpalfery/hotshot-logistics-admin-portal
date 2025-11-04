using FluentValidation;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;

namespace HotshotLogistics.Application.Validators;

/// <summary>
/// Validator for job creation requests.
/// </summary>
public class CreateJobValidator : AbstractValidator<ContractsJobDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateJobValidator"/> class.
    /// </summary>
    public CreateJobValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Job title is required.")
            .MaximumLength(200).WithMessage("Job title cannot exceed 200 characters.");

        RuleFor(x => x.PickupAddress)
            .NotEmpty().WithMessage("Pickup address is required.")
            .MaximumLength(500).WithMessage("Pickup address cannot exceed 500 characters.");

        RuleFor(x => x.DropoffAddress)
            .NotEmpty().WithMessage("Dropoff address is required.")
            .MaximumLength(500).WithMessage("Dropoff address cannot exceed 500 characters.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Job amount must be greater than zero.")
            .LessThanOrEqualTo(100000).WithMessage("Job amount cannot exceed $100,000.");

        RuleFor(x => (DateTime?)x.ScheduledPickupTime)
            .NotNull().WithMessage("Scheduled pickup time is required.")
            .Must(dt => dt > DateTime.UtcNow).WithMessage("Scheduled pickup time must be in the future.")
            .Must(dt => dt < DateTime.UtcNow.AddDays(365)).WithMessage("Scheduled pickup time cannot be more than 365 days in the future.");

        RuleFor(x => (DateTime?)x.EstimatedDeliveryTime)
            .NotNull().WithMessage("Estimated delivery time is required.")
            .Must((dto, etd) => etd > (DateTime?)dto.ScheduledPickupTime)
                .WithMessage("Estimated delivery time must be after scheduled pickup time.")
            .Must((dto, etd) => etd < ((DateTime?)dto.ScheduledPickupTime)?.AddDays(30))
                .WithMessage("Estimated delivery time cannot be more than 30 days after pickup.")
            .When(x => x.EstimatedDeliveryTime != null);

        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.PickupLocation)
            .NotNull().WithMessage("Pickup location is required.")
            .SetValidator(new LocationValidator());

        RuleFor(x => x.DeliveryLocation)
            .NotNull().WithMessage("Delivery location is required.")
            .SetValidator(new LocationValidator());

        RuleFor(x => x.Cargo)
            .NotNull().WithMessage("Cargo details are required.")
            .SetValidator(new CargoDetailsValidator());

        RuleFor(x => x.Pricing)
            .NotNull().WithMessage("Pricing details are required.")
            .SetValidator(new PricingDetailsValidator());

        RuleFor(x => x.SpecialInstructions)
            .MaximumLength(1000).WithMessage("Special instructions cannot exceed 1000 characters.")
            .When(x => x.SpecialInstructions != null);
    }
}

