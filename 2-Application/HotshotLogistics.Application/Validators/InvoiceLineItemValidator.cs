using FluentValidation;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;


namespace HotshotLogistics.Application.Validators;
/// <summary>
/// Validator for invoice line items.
/// </summary>
public class InvoiceLineItemValidator : AbstractValidator<InvoiceLineItem?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvoiceLineItemValidator"/> class.
    /// </summary>
    public InvoiceLineItemValidator()
    {
        // Guard for null InvoiceLineItem when used as IValidator<InvoiceLineItem?>
        When(x => x != null, () =>
        {
            RuleFor(x => x!.Id)
                .GreaterThan(0).WithMessage("Line item ID must be greater than zero.");

            RuleFor(x => x!.Description)
                .NotEmpty().WithMessage("Line item description is required.")
                .MaximumLength(500).WithMessage("Line item description cannot exceed 500 characters.");

            RuleFor(x => x!.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.")
                .LessThanOrEqualTo(10000).WithMessage("Quantity cannot exceed 10,000.");

            RuleFor(x => x!.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative.")
                .LessThanOrEqualTo(100000).WithMessage("Unit price cannot exceed $100,000.");

            RuleFor(x => x!.Amount)
                .GreaterThan(0).WithMessage("Line item amount must be greater than zero.")
                .Equal(x => Math.Round(x!.Quantity * x!.UnitPrice, 2)).WithMessage("Amount must equal quantity times unit price.");

            RuleFor(x => x!.TaxApplicable)
                .NotNull().WithMessage("Tax applicable flag is required.");

            RuleFor(x => x!.SortOrder)
                .GreaterThanOrEqualTo(0).WithMessage("Sort order must be zero or greater.");
        });
    }
}