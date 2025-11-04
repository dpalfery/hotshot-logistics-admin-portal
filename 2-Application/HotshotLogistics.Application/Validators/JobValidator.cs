using FluentValidation;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;


namespace HotshotLogistics.Application.Validators;

/// <summary>
/// Validator for Job entity.
/// </summary>
public class JobValidator : AbstractValidator<Job>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JobValidator"/> class.
    /// </summary>
    public JobValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Job title is required.")
            .MaximumLength(200).WithMessage("Job title cannot exceed 200 characters.");

        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.PickupLocation)
            .NotNull().WithMessage("Pickup location is required.")
            .SetValidator(new LocationValidator()!);

        RuleFor(x => x.DeliveryLocation)
            .NotNull().WithMessage("Delivery location is required.")
            .SetValidator(new LocationValidator()!);

        RuleFor(x => x.Cargo)
            .NotNull().WithMessage("Cargo details are required.")
            .SetValidator(new CargoDetailsValidator()!);

        RuleFor(x => x.Pricing)
            .NotNull().WithMessage("Pricing details are required.")
            .SetValidator(new PricingDetailsValidator()!);

        RuleFor(x => x.ScheduledPickupTime)
            .Must(dt => dt > DateTime.UtcNow).WithMessage("Scheduled pickup time must be in the future.")
            .When(x => x.Status == Core.Enums.JobStatus.Pending);

        RuleFor(x => x.EstimatedDeliveryTime)
            .Must((job, etd) => etd > job.ScheduledPickupTime)
                .WithMessage("Estimated delivery time must be after scheduled pickup time.");

        RuleFor(x => x.SpecialInstructions)
            .MaximumLength(1000).WithMessage("Special instructions cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.SpecialInstructions));
    }
}
