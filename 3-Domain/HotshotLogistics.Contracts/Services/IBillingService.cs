using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.DTOs;

namespace HotshotLogistics.Contracts.Services;

/// <summary>
/// Service interface for billing and invoice management operations.
/// </summary>
public interface IBillingService
{
    /// <summary>
    /// Generates an invoice for a completed job.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generated invoice.</returns>
    Task<Invoice> GenerateInvoiceAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an invoice by its identifier.
    /// </summary>
    /// <param name="invoiceId">The invoice identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The invoice if found, null otherwise.</returns>
    Task<Invoice?> GetInvoiceByIdAsync(string invoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates tax amount based on location and amount.
    /// </summary>
    /// <param name="amount">The amount to calculate tax for.</param>
    /// <param name="state">The state for tax calculation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The calculated tax amount.</returns>
    Task<decimal> CalculateTaxAsync(decimal amount, string state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a payment for an invoice.
    /// </summary>
    /// <param name="invoiceId">The invoice identifier.</param>
    /// <param name="paymentAmount">The payment amount.</param>
    /// <param name="paymentMethod">The payment method.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if payment was processed successfully.</returns>
    Task<bool> ProcessPaymentAsync(string invoiceId, decimal paymentAmount, string paymentMethod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all invoices for a customer.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The customer's invoices.</returns>
    Task<IEnumerable<Invoice>> GetCustomerInvoicesAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all invoices with pagination.
    /// </summary>
    /// <param name="pageNumber">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paged result of invoices.</returns>
    Task<PagedResult<Invoice>> GetAllInvoicesAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets overdue invoices.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The overdue invoices.</returns>
    Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync(CancellationToken cancellationToken = default);
}
