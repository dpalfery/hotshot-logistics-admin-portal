// <copyright file="CustomerService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Application.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using HotshotLogistics.Domain.Entities;
    using HotshotLogistics.Contracts.Repositories;
    using HotshotLogistics.Domain.ValueObjects;
    using HotshotLogistics.Contracts.Services;

    /// <summary>
    /// Service for managing customers.
    /// </summary>
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository customerRepository;
        private readonly IJobRepository jobRepository;
        private readonly IInvoiceRepository invoiceRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomerService"/> class.
        /// </summary>
        /// <param name="customerRepository">The customer repository.</param>
        /// <param name="jobRepository">The job repository.</param>
        /// <param name="invoiceRepository">The invoice repository.</param>
        public CustomerService(
            ICustomerRepository customerRepository,
            IJobRepository jobRepository,
            IInvoiceRepository invoiceRepository)
        {
            this.customerRepository = customerRepository;
            this.jobRepository = jobRepository;
            this.invoiceRepository = invoiceRepository;
        }

        /// <inheritdoc/>
        public Task<IEnumerable<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default)
        {
            return customerRepository.GetAllAsync();
        }

        /// <inheritdoc/>
        public Task<Customer?> GetCustomerByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return customerRepository.GetByIdAsync(id);
        }

        /// <inheritdoc/>
        public Task<Customer> CreateCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            return customerRepository.AddAsync(customer);
        }

        /// <inheritdoc/>
        public async Task<Customer?> UpdateCustomerAsync(string id, Customer customer, CancellationToken cancellationToken = default)
        {
            var existingCustomer = await customerRepository.GetByIdAsync(id);
            if (existingCustomer == null)
            {
                return null;
            }

            customer.Id = id;
            return await customerRepository.UpdateAsync(customer);
        }

        /// <inheritdoc/>
        public Task<bool> DeleteCustomerAsync(string id, CancellationToken cancellationToken = default)
        {
            return customerRepository.DeleteAsync(id);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken = default)
        {
            return customerRepository.GetActiveCustomersAsync();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<Customer>> GetOverdueCustomersAsync(CancellationToken cancellationToken = default)
        {
            return customerRepository.GetOverdueCustomersAsync();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<Job>> GetCustomerJobsAsync(string customerId, CancellationToken cancellationToken = default)
        {
            return jobRepository.GetJobsByCustomerAsync(customerId, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<Invoice>> GetCustomerInvoicesAsync(string customerId, CancellationToken cancellationToken = default)
        {
            return invoiceRepository.GetByCustomerIdAsync(customerId);
        }

        /// <inheritdoc/>
        public Task<bool> UpdateCreditLimitAsync(string customerId, decimal newLimit, CancellationToken cancellationToken = default)
        {
            return customerRepository.UpdateCreditLimitAsync(customerId, newLimit);
        }

        /// <inheritdoc/>
        public Task<bool> UpdateCreditTermsAsync(string customerId, CreditTerms creditTerms, CancellationToken cancellationToken = default)
        {
            return customerRepository.UpdateCreditTermsAsync(customerId, creditTerms);
        }

        /// <inheritdoc/>
        public Task<bool> ValidateCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            // Basic validation - can be extended
            var isValid = !string.IsNullOrWhiteSpace(customer.CompanyName) &&
                         !string.IsNullOrWhiteSpace(customer.Id) &&
                         customer.CreditLimit >= 0;
            return Task.FromResult(isValid);
        }
    }
}
