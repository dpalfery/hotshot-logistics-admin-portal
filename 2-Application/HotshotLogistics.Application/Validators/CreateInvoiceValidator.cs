using FluentValidation;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;


namespace HotshotLogistics.Application.Validators;

/// <summary>
/// Validator for invoice creation requests.
/// </summary>
public class CreateInvoiceValidator : AbstractValidator<Invoice>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateInvoiceValidator"/> class.
    /// </summary>
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.InvoiceNumber)
            .NotEmpty().WithMessage("Invoice number is required.")
            .MaximumLength(50).WithMessage("Invoice number cannot exceed 50 characters.")
            .Matches(@"^[A-Z0-9\-]+$").WithMessage("Invoice number can only contain letters, numbers, and hyphens.");

        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.JobId)
            .NotEmpty().When(x => x.JobId != null).WithMessage("Job ID cannot be empty if provided.");

        RuleFor(x => x.InvoiceDate)
            .NotEmpty().WithMessage("Invoice date is required.")
            .LessThanOrEqualTo(_ => DateTime.UtcNow.Date).WithMessage("Invoice date cannot be in the future.");

        RuleFor(x => x.DueDate)
            .NotEmpty().WithMessage("Due date is required.")
            .GreaterThan(x => x.InvoiceDate).WithMessage("Due date must be after invoice date.")
            .LessThan(x => x.InvoiceDate.AddDays(365)).WithMessage("Due date cannot be more than 365 days after invoice date.");

        RuleFor(x => x.LineItems)
            .NotEmpty().WithMessage("At least one line item is required.")
            .Must(x => x.Count <= 100).WithMessage("Cannot have more than 100 line items.");

        RuleForEach(x => x.LineItems)
            .SetValidator(new InvoiceLineItemValidator());

        RuleFor(x => x.SubTotal)
            .GreaterThanOrEqualTo(0).WithMessage("Subtotal cannot be negative.")
            .Equal(x => x.LineItems.Sum(li => li.Amount)).WithMessage("Subtotal must equal the sum of line item amounts.");

        RuleFor(x => x.TaxRate)
            .InclusiveBetween(0, 1).WithMessage("Tax rate must be between 0 and 100 percent.");

        RuleFor(x => x.TaxAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Tax amount cannot be negative.")
            .Equal(x => Math.Round(x.SubTotal * x.TaxRate, 2)).WithMessage("Tax amount must be correctly calculated.");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Discount amount cannot be negative.")
            .LessThanOrEqualTo(x => x.SubTotal).WithMessage("Discount amount cannot exceed subtotal.");

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0).WithMessage("Total amount must be greater than zero.")
            .Equal(x => x.SubTotal + x.TaxAmount - x.DiscountAmount).WithMessage("Total amount must be correctly calculated.");

        RuleFor(x => x.PaidAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Paid amount cannot be negative.")
            .LessThanOrEqualTo(x => x.TotalAmount).WithMessage("Paid amount cannot exceed total amount.");

        RuleFor(x => x.BalanceDue)
            .Equal(x => x.TotalAmount - x.PaidAmount).WithMessage("Balance due must equal total amount minus paid amount.");

        RuleFor(x => x.Terms)
            .NotNull().WithMessage("Payment terms are required.")
            .SetValidator(new PaymentTermsValidator());

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.");
    }
}


