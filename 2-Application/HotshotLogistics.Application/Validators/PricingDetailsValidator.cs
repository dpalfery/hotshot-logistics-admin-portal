using FluentValidation;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;

namespace HotshotLogistics.Application.Validators;

/// <summary>
/// Validator for pricing details.
/// </summary>
public class PricingDetailsValidator : AbstractValidator<PricingDetails?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PricingDetailsValidator"/> class.
    /// </summary>
    public PricingDetailsValidator()
    {
        When(x => x != null, () =>
        {
            RuleFor(x => x!.BaseRate)
                .GreaterThanOrEqualTo(0).WithMessage("Base rate cannot be negative.");

            RuleFor(x => x!.MileageRate)
                .GreaterThanOrEqualTo(0).WithMessage("Mileage rate cannot be negative.");

            RuleFor(x => x!.FuelSurcharge)
                .GreaterThanOrEqualTo(0).WithMessage("Fuel surcharge cannot be negative.");

            RuleFor(x => x!.TollCharges)
                .GreaterThanOrEqualTo(0).WithMessage("Toll charges cannot be negative.");

            RuleFor(x => x!.AdditionalCharges)
                .GreaterThanOrEqualTo(0).WithMessage("Additional charges cannot be negative.");

            RuleFor(x => x!.TotalAmount)
                .GreaterThanOrEqualTo(0).WithMessage("Total amount cannot be negative.");

            RuleFor(x => x!.Discount)
                .GreaterThanOrEqualTo(0).WithMessage("Discount cannot be negative.");

            RuleFor(x => x!.Tax)
                .GreaterThanOrEqualTo(0).WithMessage("Tax cannot be negative.");

            RuleFor(x => x!.TaxRate)
                .InclusiveBetween(0, 100).WithMessage("Tax rate must be between 0 and 100 percent.");

            RuleFor(x => x!.Currency)
                .NotEmpty().WithMessage("Currency is required.")
                .Length(3).WithMessage("Currency must be a 3-letter code.");

            RuleFor(x => x!.Notes)
                .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");
        });
    }
}
