using HotshotLogistics.Core.Enums;

namespace HotshotLogistics.Contracts.Services;

/// <summary>
/// Interface for payment processing operations.
/// </summary>
public interface IPaymentProcessor
{
    /// <summary>
    /// Gets the name of the payment processor.
    /// </summary>
    string ProcessorName { get; }

    /// <summary>
    /// Processes a payment through the payment gateway.
    /// </summary>
    /// <param name="amount">The payment amount.</param>
    /// <param name="currency">The currency code (e.g., "USD").</param>
    /// <param name="paymentMethod">The payment method details.</param>
    /// <param name="metadata">Additional metadata for the payment.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The payment processing result.</returns>
    Task<PaymentProcessingResult> ProcessPaymentAsync(
        decimal amount,
        string currency,
        PaymentMethodDetails paymentMethod,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refunds a payment.
    /// </summary>
    /// <param name="transactionId">The original transaction ID.</param>
    /// <param name="amount">The refund amount.</param>
    /// <param name="reason">The reason for the refund.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The refund processing result.</returns>
    Task<PaymentProcessingResult> RefundPaymentAsync(
        string transactionId,
        decimal amount,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates webhook signature for security.
    /// </summary>
    /// <param name="payload">The webhook payload.</param>
    /// <param name="signature">The signature to validate.</param>
    /// <param name="secret">The webhook secret.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    bool ValidateWebhookSignature(string payload, string signature, string secret);

    /// <summary>
    /// Processes a webhook event.
    /// </summary>
    /// <param name="webhookData">The webhook event data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The webhook processing result.</returns>
    Task<WebhookProcessingResult> ProcessWebhookAsync(
        WebhookEventData webhookData,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents payment method details.
/// </summary>
public class PaymentMethodDetails
{
    /// <summary>
    /// Gets or sets the payment method type.
    /// </summary>
    public PaymentMethodType Type { get; set; }

    /// <summary>
    /// Gets or sets the token or reference for the payment method.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional payment method data.
    /// </summary>
    public Dictionary<string, string> AdditionalData { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents the result of a payment processing operation.
/// </summary>
public class PaymentProcessingResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the processor response message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata from the processor.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the error code if the operation failed.
    /// </summary>
    public string? ErrorCode { get; set; }
}

/// <summary>
/// Represents webhook event data.
/// </summary>
public class WebhookEventData
{
    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event data payload.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the webhook signature.
    /// </summary>
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional headers.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents the result of webhook processing.
/// </summary>
public class WebhookProcessingResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the webhook was processed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the payment status update if applicable.
    /// </summary>
    public PaymentStatusUpdate? StatusUpdate { get; set; }

    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Represents a payment status update from a webhook.
/// </summary>
public class PaymentStatusUpdate
{
    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new payment status.
    /// </summary>
    public PaymentStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the invoice ID.
    /// </summary>
    public string InvoiceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public decimal Amount { get; set; }
}
