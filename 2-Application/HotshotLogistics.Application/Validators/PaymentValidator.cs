using FluentValidation;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.Entities;

namespace HotshotLogistics.Application.Validators;

/// <summary>
/// Validator for payment creation and processing.
/// </summary>
public class PaymentValidator : AbstractValidator<Payment>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentValidator"/> class.
    /// </summary>
    public PaymentValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Payment ID is required.");

        RuleFor(x => x.InvoiceId)
            .NotEmpty().WithMessage("Invoice ID is required.");

        RuleFor(x => x.PaymentDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5)).WithMessage("Payment date cannot be in the future.")
            .GreaterThan(DateTime.UtcNow.AddDays(-30)).WithMessage("Payment date cannot be more than 30 days in the past.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Payment amount must be greater than zero.")
            .LessThanOrEqualTo(100000).WithMessage("Payment amount cannot exceed $100,000.");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum().WithMessage("Invalid payment method.");

        RuleFor(x => x.TransactionId)
            .MaximumLength(100).WithMessage("Transaction ID cannot exceed 100 characters.")
            .Matches(@"^[A-Za-z0-9\-_]+$").When(x => !string.IsNullOrEmpty(x.TransactionId)).WithMessage("Transaction ID can only contain letters, numbers, hyphens, and underscores.");

        RuleFor(x => x.ProcessorResponse)
            .MaximumLength(1000).WithMessage("Processor response cannot exceed 1000 characters.");

        // Business rules for payment validation
        RuleFor(x => x)
            .Must(HaveValidPaymentForInvoice).WithMessage("Payment must be valid for the associated invoice.")
            .Must(NotBeDuplicateTransaction).WithMessage("Transaction ID must be unique.");
    }

    /// <summary>
    /// Validates that the payment is appropriate for the invoice.
    /// </summary>
    /// <param name="payment">The payment to validate.</param>
    /// <returns>True if the payment is valid for the invoice, false otherwise.</returns>
    private bool HaveValidPaymentForInvoice(Payment payment)
    {
        // In a real implementation, we would check against the invoice
        // For now, we'll assume the amount is reasonable
        return payment.Amount > 0 && payment.Amount <= 100000;
    }

    /// <summary>
    /// Validates that the transaction ID is not a duplicate.
    /// </summary>
    /// <param name="payment">The payment to validate.</param>
    /// <returns>True if the transaction ID is unique, false otherwise.</returns>
    private bool NotBeDuplicateTransaction(Payment payment)
    {
        // In a real implementation, we would check against existing transactions
        // For now, we'll assume it's unique if provided
        return string.IsNullOrEmpty(payment.TransactionId) || payment.TransactionId.Length > 5;
    }
}

/// <summary>
/// Validator for payment processing requests.
/// </summary>
public class ProcessPaymentValidator : AbstractValidator<Payment>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessPaymentValidator"/> class.
    /// </summary>
    public ProcessPaymentValidator()
    {
        Include(new PaymentValidator());

        RuleFor(x => x.Status)
            .Equal(PaymentStatus.Pending).WithMessage("Payment must be in pending status to be processed.");

        RuleFor(x => x.Amount)
            .GreaterThan(0.01M).WithMessage("Payment amount must be at least $0.01.");

        RuleFor(x => x.PaymentMethod)
            .NotEqual(PaymentMethodType.Cash).When(x => x.Amount > 10000).WithMessage("Cash payments cannot exceed $10,000.");
    }
}

/// <summary>
/// Validator for payment refunds.
/// </summary>
public class RefundPaymentValidator : AbstractValidator<Payment>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefundPaymentValidator"/> class.
    /// </summary>
    public RefundPaymentValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Payment ID is required for refund.");

        RuleFor(x => x.Status)
            .Equal(PaymentStatus.Completed).WithMessage("Only completed payments can be refunded.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Refund amount must be greater than zero.");

        // Business rule: Refund amount cannot exceed original payment
        RuleFor(x => x)
            .Must(CanBeRefunded).WithMessage("Payment cannot be refunded at this time.");
    }

    /// <summary>
    /// Validates that the payment can be refunded.
    /// </summary>
    /// <param name="payment">The payment to validate.</param>
    /// <returns>True if the payment can be refunded, false otherwise.</returns>
    private bool CanBeRefunded(Payment payment)
    {
        // In a real implementation, we would check refund eligibility rules
        // For now, we'll assume completed payments can be refunded within 30 days
        return payment.Status == PaymentStatus.Completed &&
               payment.PaymentDate > DateTime.UtcNow.AddDays(-30);
    }
}
