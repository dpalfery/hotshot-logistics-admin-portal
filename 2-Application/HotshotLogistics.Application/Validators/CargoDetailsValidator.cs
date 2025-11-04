using FluentValidation;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;

namespace HotshotLogistics.Application.Validators;

/// <summary>
/// Validator for cargo details.
/// </summary>
public class CargoDetailsValidator : AbstractValidator<CargoDetails?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CargoDetailsValidator"/> class.
    /// </summary>
    public CargoDetailsValidator()
    {
        When(x => x != null, () =>
        {
            RuleFor(x => x!.Description)
                .NotEmpty().WithMessage("Cargo description is required.")
                .MaximumLength(500).WithMessage("Cargo description cannot exceed 500 characters.");

            RuleFor(x => x!.Weight)
                .GreaterThan(0).WithMessage("Cargo weight must be greater than zero.")
                .LessThanOrEqualTo(50000).WithMessage("Cargo weight cannot exceed 50,000 lbs.");

            RuleFor(x => x!.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.")
                .LessThanOrEqualTo(1000).WithMessage("Quantity cannot exceed 1,000 pieces.");

            RuleFor(x => x!.Value)
                .GreaterThanOrEqualTo(0).WithMessage("Cargo value cannot be negative.")
                .LessThanOrEqualTo(1000000).WithMessage("Cargo value cannot exceed $1,000,000.");

            RuleFor(x => x!.Dimensions)
                .MaximumLength(100).WithMessage("Dimensions description cannot exceed 100 characters.");

            RuleFor(x => x!.TemperatureRange)
                .MaximumLength(50).WithMessage("Temperature range cannot exceed 50 characters.");

            RuleFor(x => x!.SpecialInstructions)
                .MaximumLength(500).WithMessage("Special instructions cannot exceed 500 characters.");

            RuleFor(x => x!.PackagingType)
                .MaximumLength(100).WithMessage("Packaging type cannot exceed 100 characters.");
        });
    }
}