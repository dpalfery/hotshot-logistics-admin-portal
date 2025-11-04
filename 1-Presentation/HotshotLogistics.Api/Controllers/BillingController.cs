// <copyright file="BillingController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotshotLogistics.Application.Services;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using HotshotLogistics.Application.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HotshotLogistics.Api.Controllers
{
    /// <summary>
    /// API controller for billing and financial operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BillingController : ControllerBase
    {
        private readonly IBillingService billingService;
        private readonly IPaymentProcessorFactory paymentProcessorFactory;
        private readonly ILogger<BillingController> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingController"/> class.
        /// </summary>
        /// <param name="billingService">The billing service.</param>
        /// <param name="paymentProcessorFactory">The payment processor factory.</param>
        /// <param name="logger">The logger.</param>
        public BillingController(
            IBillingService billingService,
            IPaymentProcessorFactory paymentProcessorFactory,
            ILogger<BillingController> logger)
        {
            this.billingService = billingService ?? throw new ArgumentNullException(nameof(billingService));
            this.paymentProcessorFactory = paymentProcessorFactory ?? throw new ArgumentNullException(nameof(paymentProcessorFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Generates an invoice for a completed job.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The generated invoice.</returns>
        [HttpPost("invoices/generate/{jobId}")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(Invoice), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Invoice>> GenerateInvoice(string jobId, CancellationToken cancellationToken = default)
        {
            try
            {
                var invoice = await billingService.GenerateInvoiceAsync(jobId, cancellationToken);
                return CreatedAtAction(
                    nameof(GetInvoice),
                    new { id = invoice.Id },
                    invoice);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid job ID provided for invoice generation");
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Job not found for invoice generation");
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while generating invoice for job");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Gets an invoice by ID.
        /// </summary>
        /// <param name="id">The invoice ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The invoice if found; otherwise, 404 Not Found.</returns>
        [HttpGet("invoices/{id}")]
        [Authorize(Policy = AuthorizationPolicies.OwnResource)]
        [ProducesResponseType(typeof(Invoice), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Invoice>> GetInvoice(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var invoice = await billingService.GetInvoiceByIdAsync(id, cancellationToken);
                if (invoice == null)
                {
                    return NotFound($"Invoice with ID {id} not found");
                }

                return Ok(invoice);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving invoice");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Gets invoices for a specific customer.
        /// </summary>
        /// <param name="customerId">The customer ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of invoices for the customer.</returns>
        [HttpGet("invoices/customer/{customerId}")]
        [Authorize(Policy = AuthorizationPolicies.CustomerResource)]
        [ProducesResponseType(typeof(IEnumerable<Invoice>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Invoice>>> GetCustomerInvoices(string customerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var invoices = await billingService.GetCustomerInvoicesAsync(customerId, cancellationToken);
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving invoices for customer");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Gets overdue invoices.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of overdue invoices.</returns>
        [HttpGet("invoices/overdue")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(IEnumerable<Invoice>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Invoice>>> GetOverdueInvoices(CancellationToken cancellationToken = default)
        {
            try
            {
                var invoices = await billingService.GetOverdueInvoicesAsync(cancellationToken);
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving overdue invoices");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Processes a payment for an invoice.
        /// </summary>
        /// <param name="invoiceId">The invoice ID.</param>
        /// <param name="request">The payment request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Success status of the payment processing.</returns>
        [HttpPost("invoices/{invoiceId}/payments")]
        [Authorize(Policy = AuthorizationPolicies.CustomerResource)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PaymentResult>> ProcessPayment(
            string invoiceId,
            [FromBody] ProcessPaymentRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Payment request is required");
                }

                if (request.Amount <= 0)
                {
                    return BadRequest("Payment amount must be greater than zero");
                }

                if (string.IsNullOrWhiteSpace(request.PaymentMethod))
                {
                    return BadRequest("Payment method is required");
                }

                var success = await billingService.ProcessPaymentAsync(
                    invoiceId,
                    request.Amount,
                    request.PaymentMethod,
                    cancellationToken);

                var result = new PaymentResult
                {
                    Success = success,
                    InvoiceId = invoiceId,
                    Amount = request.Amount,
                    PaymentMethod = request.PaymentMethod,
                    ProcessedAt = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid payment request for invoice: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Invoice not found for payment processing");
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while processing payment for invoice");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Calculates tax for a given amount and location.
        /// </summary>
        /// <param name="request">The tax calculation request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The calculated tax amount.</returns>
        [HttpPost("tax/calculate")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(TaxCalculationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TaxCalculationResult>> CalculateTax(
            [FromBody] TaxCalculationRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Tax calculation request is required");
                }

                if (request.Amount <= 0)
                {
                    return BadRequest("Amount must be greater than zero");
                }

                if (string.IsNullOrWhiteSpace(request.State))
                {
                    return BadRequest("State is required for tax calculation");
                }

                var taxAmount = await billingService.CalculateTaxAsync(request.Amount, request.State, cancellationToken);

                var result = new TaxCalculationResult
                {
                    Amount = request.Amount,
                    State = request.State,
                    TaxAmount = taxAmount,
                    TotalAmount = request.Amount + taxAmount,
                    TaxRate = request.Amount > 0 ? taxAmount / request.Amount : 0
                };

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid tax calculation request: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while calculating tax");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Gets accounts receivable report.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Accounts receivable summary.</returns>
        [HttpGet("reports/accounts-receivable")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(AccountsReceivableReport), StatusCodes.Status200OK)]
        public async Task<ActionResult<AccountsReceivableReport>> GetAccountsReceivableReport(CancellationToken cancellationToken = default)
        {
            try
            {
                var overdueInvoices = await billingService.GetOverdueInvoicesAsync(cancellationToken);

                var report = new AccountsReceivableReport
                {
                    TotalOverdueAmount = overdueInvoices.Sum(i => i.BalanceDue),
                    OverdueInvoiceCount = overdueInvoices.Count(),
                    GeneratedAt = DateTime.UtcNow,
                    OverdueInvoices = overdueInvoices.Select(i => new OverdueInvoiceSummary
                    {
                        InvoiceId = i.Id,
                        InvoiceNumber = i.InvoiceNumber,
                        CustomerId = i.CustomerId,
                        Amount = i.TotalAmount,
                        BalanceDue = i.BalanceDue,
                        DueDate = i.DueDate,
                        DaysOverdue = (int)(DateTime.UtcNow - i.DueDate).TotalDays
                    }).ToList()
                };

                return Ok(report);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while generating accounts receivable report");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Handles Stripe webhook events.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The webhook processing result.</returns>
        [HttpPost("webhooks/stripe")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> HandleStripeWebhook(CancellationToken cancellationToken = default)
        {
            try
            {
                var processor = paymentProcessorFactory.GetProcessor("Stripe");

                // Read the request body
                using var reader = new StreamReader(Request.Body);
                var payload = await reader.ReadToEndAsync(cancellationToken);

                // Get the Stripe signature from headers
                var signature = Request.Headers["Stripe-Signature"].FirstOrDefault() ?? string.Empty;

                var webhookData = new WebhookEventData
                {
                    Payload = payload,
                    Signature = signature,
                    Headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.FirstOrDefault() ?? string.Empty)
                };

                // Extract event type from payload (simplified)
                webhookData.EventType = ExtractStripeEventType(payload);

                var result = await processor.ProcessWebhookAsync(webhookData, cancellationToken);

                if (result.Success && result.StatusUpdate != null)
                {
                    // Handle payment status update
                    await HandlePaymentStatusUpdateAsync(result.StatusUpdate, cancellationToken);
                }

                logger.LogInformation("Stripe webhook processed: {Success}", result.Success);

                return Ok(new { received = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing Stripe webhook");
                return BadRequest(new { error = "Webhook processing failed" });
            }
        }

        /// <summary>
        /// Handles PayPal webhook events.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The webhook processing result.</returns>
        [HttpPost("webhooks/paypal")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> HandlePayPalWebhook(CancellationToken cancellationToken = default)
        {
            try
            {
                var processor = paymentProcessorFactory.GetProcessor("PayPal");

                // Read the request body
                using var reader = new StreamReader(Request.Body);
                var payload = await reader.ReadToEndAsync(cancellationToken);

                // Get PayPal signature from headers
                var signature = Request.Headers["PayPal-Transmission-Signature"].FirstOrDefault() ?? string.Empty;

                var webhookData = new WebhookEventData
                {
                    Payload = payload,
                    Signature = signature,
                    Headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.FirstOrDefault() ?? string.Empty)
                };

                // Extract event type from payload (simplified)
                webhookData.EventType = ExtractPayPalEventType(payload);

                var result = await processor.ProcessWebhookAsync(webhookData, cancellationToken);

                if (result.Success && result.StatusUpdate != null)
                {
                    // Handle payment status update
                    await HandlePaymentStatusUpdateAsync(result.StatusUpdate, cancellationToken);
                }

                logger.LogInformation("PayPal webhook processed: {Success}", result.Success);

                return Ok(new { received = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing PayPal webhook");
                return BadRequest(new { error = "Webhook processing failed" });
            }
        }

        /// <summary>
        /// Handles payment status updates from webhooks.
        /// </summary>
        /// <param name="statusUpdate">The payment status update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandlePaymentStatusUpdateAsync(PaymentStatusUpdate statusUpdate, CancellationToken cancellationToken)
        {
            logger.LogInformation("Handling payment status update: Status={Status}", statusUpdate.Status);

            try
            {
                // Update payment status in the database
                // Note: In a real implementation, you'd have a payment repository method for this
                // For now, we'll assume the payment record exists and update it

                if (statusUpdate.Status == PaymentStatus.Completed)
                {
                    // Update invoice with payment amount
                    var invoice = await billingService.GetCustomerInvoicesAsync(statusUpdate.InvoiceId, cancellationToken);
                    var targetInvoice = invoice.FirstOrDefault(i => i.Id == statusUpdate.InvoiceId);

                    if (targetInvoice != null)
                    {
                        // Send payment confirmation notification
                        try
                        {
                            await billingService.GetCustomerInvoicesAsync(targetInvoice.CustomerId, cancellationToken); // Just to get customer context
                            // Note: In real implementation, you'd have a method to send payment notifications
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Failed to send payment notification for invoice {InvoiceId}", statusUpdate.InvoiceId);
                        }
                    }
                }

                logger.LogInformation("Payment status update handled successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling payment status update");
                throw;
            }
        }

        /// <summary>
        /// Extracts the event type from a Stripe webhook payload.
        /// </summary>
        /// <param name="payload">The webhook payload.</param>
        /// <returns>The event type.</returns>
        private string ExtractStripeEventType(string payload)
        {
            // Simplified extraction - in real implementation, parse JSON properly
            if (payload.Contains("payment_intent.succeeded"))
                return "payment_intent.succeeded";
            if (payload.Contains("payment_intent.payment_failed"))
                return "payment_intent.payment_failed";
            return "unknown";
        }

        /// <summary>
        /// Extracts the event type from a PayPal webhook payload.
        /// </summary>
        /// <param name="payload">The webhook payload.</param>
        /// <returns>The event type.</returns>
        private string ExtractPayPalEventType(string payload)
        {
            // Simplified extraction - in real implementation, parse JSON properly
            if (payload.Contains("PAYMENT.CAPTURE.COMPLETED"))
                return "PAYMENT.CAPTURE.COMPLETED";
            if (payload.Contains("PAYMENT.CAPTURE.DENIED"))
                return "PAYMENT.CAPTURE.DENIED";
            return "unknown";
        }
    }

    /// <summary>
    /// Request model for processing payments.
    /// </summary>
    public class ProcessPaymentRequest
    {
        /// <summary>
        /// Gets or sets the payment amount.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Gets or sets the payment method.
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets optional payment reference.
        /// </summary>
        public string? Reference { get; set; }
    }

    /// <summary>
    /// Result model for payment processing.
    /// </summary>
    public class PaymentResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the payment was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the invoice ID.
        /// </summary>
        public string InvoiceId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the payment amount.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Gets or sets the payment method.
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the processing timestamp.
        /// </summary>
        public DateTime ProcessedAt { get; set; }
    }

    /// <summary>
    /// Request model for tax calculations.
    /// </summary>
    public class TaxCalculationRequest
    {
        /// <summary>
        /// Gets or sets the amount to calculate tax for.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Gets or sets the state for tax calculation.
        /// </summary>
        public string State { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result model for tax calculations.
    /// </summary>
    public class TaxCalculationResult
    {
        /// <summary>
        /// Gets or sets the original amount.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        public string State { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the calculated tax amount.
        /// </summary>
        public decimal TaxAmount { get; set; }

        /// <summary>
        /// Gets or sets the total amount including tax.
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Gets or sets the tax rate applied.
        /// </summary>
        public decimal TaxRate { get; set; }
    }

    /// <summary>
    /// Report model for accounts receivable.
    /// </summary>
    public class AccountsReceivableReport
    {
        /// <summary>
        /// Gets or sets the total overdue amount.
        /// </summary>
        public decimal TotalOverdueAmount { get; set; }

        /// <summary>
        /// Gets or sets the count of overdue invoices.
        /// </summary>
        public int OverdueInvoiceCount { get; set; }

        /// <summary>
        /// Gets or sets the report generation timestamp.
        /// </summary>
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// Gets or sets the list of overdue invoices.
        /// </summary>
        public List<OverdueInvoiceSummary> OverdueInvoices { get; set; } = new List<OverdueInvoiceSummary>();
    }

    /// <summary>
    /// Summary model for overdue invoices.
    /// </summary>
    public class OverdueInvoiceSummary
    {
        /// <summary>
        /// Gets or sets the invoice ID.
        /// </summary>
        public string InvoiceId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the invoice number.
        /// </summary>
        public string InvoiceNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the customer ID.
        /// </summary>
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the invoice amount.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Gets or sets the balance due.
        /// </summary>
        public decimal BalanceDue { get; set; }

        /// <summary>
        /// Gets or sets the due date.
        /// </summary>
        public DateTime DueDate { get; set; }

        /// <summary>
        /// Gets or sets the number of days overdue.
        /// </summary>
        public int DaysOverdue { get; set; }
    }
}
