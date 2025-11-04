// <copyright file="CustomerController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Api.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using HotshotLogistics.Application.Authorization;
    using HotshotLogistics.Domain.Entities;
    using HotshotLogistics.Contracts.Services;
    using HotshotLogistics.Domain.Entities;
    using HotshotLogistics.Domain.ValueObjects;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// API controller for customer management operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService customerService;
        private readonly ILogger<CustomerController> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomerController"/> class.
        /// </summary>
        /// <param name="customerService">The customer service.</param>
        /// <param name="logger">The logger.</param>
        public CustomerController(
            ICustomerService customerService,
            ILogger<CustomerController> logger)
        {
            this.customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all customers.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of customers.</returns>
        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(IEnumerable<Customer>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers(CancellationToken cancellationToken = default)
        {
            try
            {
                var customers = await customerService.GetCustomersAsync(cancellationToken);
                return Ok(customers);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving customers");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Gets a customer by ID.
        /// </summary>
        /// <param name="id">The customer ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The customer if found; otherwise, 404 Not Found.</returns>
        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.OwnResource)]
        [ProducesResponseType(typeof(Customer), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Customer>> GetCustomerById(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var customer = await customerService.GetCustomerByIdAsync(id, cancellationToken);
                if (customer == null)
                {
                    return NotFound($"Customer with ID {id} not found");
                }

                return Ok(customer);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving customer");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Creates a new customer.
        /// </summary>
        /// <param name="customer">The customer data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created customer.</returns>
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(Customer), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<Customer>> CreateCustomer(
            [FromBody] Customer customer,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (customer == null)
                {
                    return BadRequest("Customer data is required");
                }

                var createdCustomer = await customerService.CreateCustomerAsync(customer, cancellationToken);
                return CreatedAtAction(
                    nameof(GetCustomerById),
                    new { id = createdCustomer.Id },
                    createdCustomer);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid customer data provided: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while creating customer");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Updates an existing customer.
        /// </summary>
        /// <param name="id">The customer ID.</param>
        /// <param name="customer">The updated customer data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated customer.</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(Customer), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Customer>> UpdateCustomer(
            string id,
            [FromBody] Customer customer,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (customer == null)
                {
                    return BadRequest("Customer data is required");
                }

                // Ensure the ID in the URL matches the customer data
                customer.Id = id;

                var updatedCustomer = await customerService.UpdateCustomerAsync(id, customer, cancellationToken);
                if (updatedCustomer == null)
                {
                    return NotFound($"Customer with ID {id} not found");
                }

                return Ok(updatedCustomer);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid customer data provided: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while updating customer");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Deletes a customer.
        /// </summary>
        /// <param name="id">The customer ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content if successful; otherwise, 404 Not Found.</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = AuthorizationPolicies.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCustomer(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await customerService.DeleteCustomerAsync(id, cancellationToken);
                if (!result)
                {
                    return NotFound($"Customer with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while deleting customer");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Gets active customers.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of active customers.</returns>
        [HttpGet("active")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(IEnumerable<Customer>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Customer>>> GetActiveCustomers(CancellationToken cancellationToken = default)
        {
            try
            {
                var customers = await customerService.GetActiveCustomersAsync(cancellationToken);
                return Ok(customers);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving active customers");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Gets customers with overdue invoices.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of customers with overdue invoices.</returns>
        [HttpGet("overdue")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(IEnumerable<Customer>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Customer>>> GetOverdueCustomers(CancellationToken cancellationToken = default)
        {
            try
            {
                var customers = await customerService.GetOverdueCustomersAsync(cancellationToken);
                return Ok(customers);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving overdue customers");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Gets jobs for a specific customer.
        /// </summary>
        /// <param name="id">The customer ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of jobs for the customer.</returns>
        [HttpGet("{id}/jobs")]
        [Authorize(Policy = AuthorizationPolicies.CustomerResource)]
        [ProducesResponseType(typeof(IEnumerable<Job>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<Job>>> GetCustomerJobs(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                // First check if customer exists
                var customer = await customerService.GetCustomerByIdAsync(id, cancellationToken);
                if (customer == null)
                {
                    return NotFound($"Customer with ID {id} not found");
                }

                var jobs = await customerService.GetCustomerJobsAsync(id, cancellationToken);
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving jobs for customer");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Gets invoices for a specific customer.
        /// </summary>
        /// <param name="id">The customer ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of invoices for the customer.</returns>
        [HttpGet("{id}/invoices")]
        [Authorize(Policy = AuthorizationPolicies.CustomerResource)]
        [ProducesResponseType(typeof(IEnumerable<Invoice>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<Invoice>>> GetCustomerInvoices(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                // First check if customer exists
                var customer = await customerService.GetCustomerByIdAsync(id, cancellationToken);
                if (customer == null)
                {
                    return NotFound($"Customer with ID {id} not found");
                }

                var invoices = await customerService.GetCustomerInvoicesAsync(id, cancellationToken);
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving invoices for customer");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Updates a customer's credit limit.
        /// </summary>
        /// <param name="id">The customer ID.</param>
        /// <param name="request">The credit limit update request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content if successful; otherwise, appropriate error response.</returns>
        [HttpPost("{id}/credit-limit")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCreditLimit(
            string id,
            [FromBody] UpdateCreditLimitRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Credit limit update request is required");
                }

                if (request.NewLimit < 0)
                {
                    return BadRequest("Credit limit cannot be negative");
                }

                var result = await customerService.UpdateCreditLimitAsync(id, request.NewLimit, cancellationToken);
                if (!result)
                {
                    return NotFound($"Customer with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while updating credit limit for customer");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Updates a customer's credit terms.
        /// </summary>
        /// <param name="id">The customer ID.</param>
        /// <param name="creditTerms">The new credit terms.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content if successful; otherwise, appropriate error response.</returns>
        [HttpPut("{id}/credit-terms")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCreditTerms(
            string id,
            [FromBody] CreditTerms creditTerms,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (creditTerms == null)
                {
                    return BadRequest("Credit terms are required");
                }

                if (creditTerms.PaymentTermsDays <= 0)
                {
                    return BadRequest("Payment terms days must be greater than zero");
                }

                var result = await customerService.UpdateCreditTermsAsync(id, creditTerms, cancellationToken);
                if (!result)
                {
                    return NotFound($"Customer with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while updating credit terms for customer");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }
    }

    /// <summary>
    /// Request model for updating customer credit limit.
    /// </summary>
    public class UpdateCreditLimitRequest
    {
        /// <summary>
        /// Gets or sets the new credit limit.
        /// </summary>
        public decimal NewLimit { get; set; }
    }
}
