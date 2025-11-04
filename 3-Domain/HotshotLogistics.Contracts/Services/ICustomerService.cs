using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;
namespace HotshotLogistics.Contracts.Services;

/// <summary>
/// Service interface for customer management operations.
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// Gets all customers.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of customers.</returns>
    Task<IEnumerable<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a customer by ID.
    /// </summary>
    /// <param name="id">The customer ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The customer if found; otherwise, null.</returns>
    Task<Customer?> GetCustomerByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    /// <param name="customer">The customer to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created customer.</returns>
    Task<Customer> CreateCustomerAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing customer.
    /// </summary>
    /// <param name="id">The customer ID.</param>
    /// <param name="customer">The updated customer data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated customer if found; otherwise, null.</returns>
    Task<Customer?> UpdateCustomerAsync(string id, Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a customer.
    /// </summary>
    /// <param name="id">The customer ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the customer was deleted; otherwise, false.</returns>
    Task<bool> DeleteCustomerAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active customers.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of active customers.</returns>
    Task<IEnumerable<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customers with overdue invoices.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of customers with overdue invoices.</returns>
    Task<IEnumerable<Customer>> GetOverdueCustomersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets jobs for a specific customer.
    /// </summary>
    /// <param name="customerId">The customer ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of jobs for the customer.</returns>
    Task<IEnumerable<Job>> GetCustomerJobsAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets invoices for a specific customer.
    /// </summary>
    /// <param name="customerId">The customer ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of invoices for the customer.</returns>
    Task<IEnumerable<Invoice>> GetCustomerInvoicesAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a customer's credit limit.
    /// </summary>
    /// <param name="customerId">The customer ID.</param>
    /// <param name="newLimit">The new credit limit.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    Task<bool> UpdateCreditLimitAsync(string customerId, decimal newLimit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a customer's credit terms.
    /// </summary>
    /// <param name="customerId">The customer ID.</param>
    /// <param name="creditTerms">The new credit terms.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    Task<bool> UpdateCreditTermsAsync(string customerId, CreditTerms creditTerms, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a customer for business rules.
    /// </summary>
    /// <param name="customer">The customer to validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the customer is valid; otherwise, false.</returns>
    Task<bool> ValidateCustomerAsync(Customer customer, CancellationToken cancellationToken = default);
}
