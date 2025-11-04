using FluentValidation;
using HotshotLogistics.Domain.Entities;

namespace HotshotLogistics.Application.Validators;

/// <summary>
/// Validator for location coordinates.
/// </summary>
public class LocationValidator : AbstractValidator<Location?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LocationValidator"/> class.
    /// </summary>
    public LocationValidator()
    {
        // Guard for null Location when using with IValidator<Location?>
        When(x => x != null, () =>
        {
            RuleFor(x => x!.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90 degrees.")
                .When(x => x!.Latitude.HasValue);

            RuleFor(x => x!.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180 degrees.")
                .When(x => x!.Longitude.HasValue);

            RuleFor(x => x!.Address)
                .NotEmpty().WithMessage("Address is required.")
                .MaximumLength(500).WithMessage("Address cannot exceed 500 characters.");

            RuleFor(x => x!.City)
                .NotEmpty().WithMessage("City is required.")
                .MaximumLength(100).WithMessage("City cannot exceed 100 characters.");

            RuleFor(x => x!.State)
                .NotEmpty().WithMessage("State is required.")
                .MaximumLength(50).WithMessage("State cannot exceed 50 characters.");

            RuleFor(x => x!.PostalCode)
                .NotEmpty().WithMessage("Postal code is required.")
                .MaximumLength(20).WithMessage("Postal code cannot exceed 20 characters.");

            RuleFor(x => x!.Country)
                .NotEmpty().WithMessage("Country is required.")
                .MaximumLength(50).WithMessage("Country cannot exceed 50 characters.");
        });
    }
}