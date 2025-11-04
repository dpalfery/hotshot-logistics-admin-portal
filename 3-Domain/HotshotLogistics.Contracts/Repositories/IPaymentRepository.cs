using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Contracts.Repositories;
using HotshotLogistics.Core.Enums;

namespace HotshotLogistics.Domain.Repositories;

/// <summary>
/// Repository interface for payment operations.
/// </summary>
public interface IPaymentRepository
{
    /// <summary>
    /// Gets a payment by its identifier.
    /// </summary>
    /// <param name="id">The payment identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The payment if found, null otherwise.</returns>
    Task<Payment?> GetByIdAsync(object id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all payments.
    /// </summary>
    /// <returns>A list of all payments.</returns>
    Task<IEnumerable<Payment>> GetAllAsync();

    /// <summary>
    /// Adds a new payment.
    /// </summary>
    /// <param name="entity">The payment to add.</param>
    /// <returns>The added payment.</returns>
    Task<Payment> AddAsync(Payment entity);

    /// <summary>
    /// Updates an existing payment.
    /// </summary>
    /// <param name="entity">The payment to update.</param>
    /// <returns>The updated payment.</returns>
    Task<Payment> UpdateAsync(Payment entity);

    /// <summary>
    /// Deletes a payment by its identifier.
    /// </summary>
    /// <param name="id">The payment identifier.</param>
    /// <returns>True if the payment was deleted, false otherwise.</returns>
    Task<bool> DeleteAsync(object id);

    /// <summary>
    /// Checks if a payment exists by its identifier.
    /// </summary>
    /// <param name="id">The payment identifier.</param>
    /// <returns>True if the payment exists, false otherwise.</returns>
    Task<bool> ExistsAsync(object id);

    /// <summary>
    /// Gets payments by invoice ID.
    /// </summary>
    /// <param name="invoiceId">The invoice ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The payments for the invoice.</returns>
    Task<IEnumerable<Payment>> GetByInvoiceIdAsync(string invoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payments by transaction ID.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The payment with the transaction ID.</returns>
    Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payments by status.
    /// </summary>
    /// <param name="status">The payment status.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The payments with the specified status.</returns>
    Task<IEnumerable<Payment>> GetByStatusAsync(PaymentStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the payment status.
    /// </summary>
    /// <param name="paymentId">The payment ID.</param>
    /// <param name="status">The new status.</param>
    /// <param name="processorResponse">The processor response.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the update was successful.</returns>
    Task<bool> UpdatePaymentStatusAsync(string paymentId, PaymentStatus status, string? processorResponse = null, CancellationToken cancellationToken = default);
}
