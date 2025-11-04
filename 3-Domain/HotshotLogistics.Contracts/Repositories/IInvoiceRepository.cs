using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.DTOs;

namespace HotshotLogistics.Contracts.Repositories;

/// <summary>
/// Repository interface for invoice operations.
/// </summary>
public interface IInvoiceRepository
{
    /// <summary>
    /// Gets an invoice by its identifier.
    /// </summary>
    /// <param name="id">The invoice identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The invoice if found, null otherwise.</returns>
    Task<Invoice?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all invoices.
    /// </summary>
    /// <returns>A list of all invoices.</returns>
    Task<IEnumerable<Invoice>> GetAllAsync();

    /// <summary>
    /// Adds a new invoice.
    /// </summary>
    /// <param name="invoice">The invoice to add.</param>
    /// <returns>The added invoice.</returns>
    Task<Invoice> AddAsync(Invoice invoice);

    /// <summary>
    /// Updates an existing invoice.
    /// </summary>
    /// <param name="invoice">The invoice to update.</param>
    /// <returns>The updated invoice.</returns>
    Task<Invoice> UpdateAsync(Invoice invoice);

    /// <summary>
    /// Deletes an invoice by its identifier.
    /// </summary>
    /// <param name="id">The invoice identifier.</param>
    /// <returns>True if the invoice was deleted, false otherwise.</returns>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Checks if an invoice exists by its identifier.
    /// </summary>
    /// <param name="id">The invoice identifier.</param>
    /// <returns>True if the invoice exists, false otherwise.</returns>
    Task<bool> ExistsAsync(string id);

    /// <summary>
    /// Gets invoices by customer identifier.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <returns>A list of invoices for the customer.</returns>
    Task<IEnumerable<Invoice>> GetByCustomerIdAsync(string customerId);

    /// <summary>
    /// Gets invoices by job identifier.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <returns>A list of invoices for the job.</returns>
    Task<IEnumerable<Invoice>> GetByJobIdAsync(string jobId);

    /// <summary>
    /// Gets invoices by status.
    /// </summary>
    /// <param name="status">The invoice status.</param>
    /// <returns>A list of invoices with the specified status.</returns>
    Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status);

    /// <summary>
    /// Gets overdue invoices.
    /// </summary>
    /// <returns>A list of overdue invoices.</returns>
    Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync();

    /// <summary>
    /// Gets invoices due within a specified number of days.
    /// </summary>
    /// <param name="days">The number of days to look ahead.</param>
    /// <returns>A list of invoices due within the specified days.</returns>
    Task<IEnumerable<Invoice>> GetInvoicesDueWithinDaysAsync(int days);

    /// <summary>
    /// Gets invoices by date range.
    /// </summary>
    /// <param name="startDate">The start date.</param>
    /// <param name="endDate">The end date.</param>
    /// <returns>A list of invoices within the date range.</returns>
    Task<IEnumerable<Invoice>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets invoices with pagination and filtering.
    /// </summary>
    /// <param name="filter">The invoice filter criteria.</param>
    /// <returns>A paged result of invoices.</returns>
    Task<PagedResult<Invoice>> GetPagedAsync(InvoiceFilter filter);

    /// <summary>
    /// Gets the next invoice number.
    /// </summary>
    /// <returns>The next available invoice number.</returns>
    Task<string> GetNextInvoiceNumberAsync();

    /// <summary>
    /// Gets aging report data for accounts receivable.
    /// </summary>
    /// <returns>A list of aging report entries.</returns>
    Task<IEnumerable<AgingReportEntry>> GetAgingReportAsync();

    /// <summary>
    /// Gets total outstanding balance for a customer.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <returns>The total outstanding balance.</returns>
    Task<decimal> GetOutstandingBalanceAsync(string customerId);

    /// <summary>
    /// Gets invoice summary statistics.
    /// </summary>
    /// <returns>Invoice summary statistics.</returns>
    Task<InvoiceSummary> GetInvoiceSummaryAsync();

    /// <summary>
    /// Gets invoices by invoice number (partial match).
    /// </summary>
    /// <param name="invoiceNumber">The invoice number to search for.</param>
    /// <returns>A list of matching invoices.</returns>
    Task<IEnumerable<Invoice>> SearchByInvoiceNumberAsync(string invoiceNumber);

    /// <summary>
    /// Updates the paid amount for an invoice.
    /// </summary>
    /// <param name="invoiceId">The invoice identifier.</param>
    /// <param name="paidAmount">The new paid amount.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    Task<bool> UpdatePaidAmountAsync(string invoiceId, decimal paidAmount);

    /// <summary>
    /// Updates the invoice status.
    /// </summary>
    /// <param name="invoiceId">The invoice identifier.</param>
    /// <param name="status">The new status.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    Task<bool> UpdateStatusAsync(string invoiceId, InvoiceStatus status);

    /// <summary>
    /// Generates a new invoice from job data.
    /// </summary>
    /// <param name="jobId">The job identifier to generate invoice for.</param>
    /// <returns>The generated invoice.</returns>
    Task<Invoice> GenerateInvoiceAsync(string jobId);
}

/// <summary>
/// Filter criteria for invoice queries.
/// </summary>
public class InvoiceFilter
{
    /// <summary>
    /// Gets or sets the customer identifier filter.
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the job identifier filter.
    /// </summary>
    public string? JobId { get; set; }

    /// <summary>
    /// Gets or sets the status filter.
    /// </summary>
    public InvoiceStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets the start date filter.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date filter.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the minimum amount filter.
    /// </summary>
    public decimal? MinAmount { get; set; }

    /// <summary>
    /// Gets or sets the maximum amount filter.
    /// </summary>
    public decimal? MaxAmount { get; set; }

    /// <summary>
    /// Gets or sets the search term for invoice number or customer name.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include only overdue invoices.
    /// </summary>
    public bool? IsOverdue { get; set; }

    /// <summary>
    /// Gets or sets the page number for pagination.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size for pagination.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the sort field.
    /// </summary>
    public string SortBy { get; set; } = "InvoiceDate";

    /// <summary>
    /// Gets or sets the sort direction.
    /// </summary>
    public string SortDirection { get; set; } = "DESC";
}

/// <summary>
/// Represents an entry in the aging report.
/// </summary>
public class AgingReportEntry
{
    /// <summary>
    /// Gets or sets the customer identifier.
    /// </summary>
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current balance (0-30 days).
    /// </summary>
    public decimal Current { get; set; }

    /// <summary>
    /// Gets or sets the 31-60 days balance.
    /// </summary>
    public decimal Days31To60 { get; set; }

    /// <summary>
    /// Gets or sets the 61-90 days balance.
    /// </summary>
    public decimal Days61To90 { get; set; }

    /// <summary>
    /// Gets or sets the over 90 days balance.
    /// </summary>
    public decimal Over90Days { get; set; }

    /// <summary>
    /// Gets or sets the total balance.
    /// </summary>
    public decimal TotalBalance { get; set; }
}

/// <summary>
/// Represents invoice summary statistics.
/// </summary>
public class InvoiceSummary
{
    /// <summary>
    /// Gets or sets the total number of invoices.
    /// </summary>
    public int TotalInvoices { get; set; }

    /// <summary>
    /// Gets or sets the total invoice amount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the total paid amount.
    /// </summary>
    public decimal TotalPaid { get; set; }

    /// <summary>
    /// Gets or sets the total outstanding amount.
    /// </summary>
    public decimal TotalOutstanding { get; set; }

    /// <summary>
    /// Gets or sets the number of overdue invoices.
    /// </summary>
    public int OverdueCount { get; set; }

    /// <summary>
    /// Gets or sets the total overdue amount.
    /// </summary>
    public decimal OverdueAmount { get; set; }

    /// <summary>
    /// Gets or sets the average days to payment.
    /// </summary>
    public double AverageDaysToPayment { get; set; }
}
