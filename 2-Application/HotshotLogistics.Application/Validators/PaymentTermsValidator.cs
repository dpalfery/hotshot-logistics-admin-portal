using FluentValidation;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;


namespace HotshotLogistics.Application.Validators;
/// <summary>
/// Validator for payment terms.
/// </summary>
public class PaymentTermsValidator : AbstractValidator<PaymentTerms?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentTermsValidator"/> class.
    /// </summary>
    public PaymentTermsValidator()
    {
        // Guard for null PaymentTerms when used as IValidator<PaymentTerms?>
        When(x => x != null, () =>
        {
            RuleFor(x => x!.Days)
                .GreaterThanOrEqualTo(0).WithMessage("Payment terms days cannot be negative.")
                .LessThanOrEqualTo(365).WithMessage("Payment terms days cannot exceed 365.");

            RuleFor(x => x!.EarlyPaymentDiscount)
                .InclusiveBetween(0, 50).WithMessage("Early payment discount must be between 0 and 50 percent.");

            RuleFor(x => x!.EarlyPaymentDiscountDays)
                .GreaterThanOrEqualTo(0).WithMessage("Early payment discount days cannot be negative.")
                .LessThanOrEqualTo(x => x!.Days).WithMessage("Early payment discount days cannot exceed payment terms days.");

            RuleFor(x => x!.LatePaymentPenalty)
                .InclusiveBetween(0, 50).WithMessage("Late payment penalty must be between 0 and 50 percent.");

            RuleFor(x => x!.LatePaymentPenaltyDays)
                .GreaterThanOrEqualTo(0).WithMessage("Late payment penalty days cannot be negative.");
        });
    }
}
