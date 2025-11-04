// <copyright file="BillingService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Contracts.Repositories;
using HotshotLogistics.Domain.Repositories;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
namespace HotshotLogistics.Application.Services
{
    /// <summary>
    /// Service for billing and invoice management operations.
    /// </summary>
    public class BillingService : IBillingService
    {
        private readonly IInvoiceRepository invoiceRepository;
        private readonly IJobRepository jobRepository;
        private readonly ICustomerRepository customerRepository;
        private readonly IPaymentRepository paymentRepository;
        private readonly INotificationService notificationService;
        private readonly IPaymentProcessorFactory paymentProcessorFactory;
        private readonly ILogger<BillingService> logger;

        private readonly AsyncRetryPolicy retryPolicy;

        // Tax rates by state (simplified for demo)
        private readonly Dictionary<string, decimal> stateTaxRates = new()
        {
            { "CA", 0.0875m }, // California
            { "TX", 0.0625m }, // Texas
            { "NY", 0.08m },   // New York
            { "FL", 0.06m },   // Florida
            { "WA", 0.065m },  // Washington
            // Add more states as needed
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingService"/> class.
        /// </summary>
        /// <param name="invoiceRepository">The invoice repository.</param>
        /// <param name="jobRepository">The job repository.</param>
        /// <param name="customerRepository">The customer repository.</param>
        /// <param name="paymentRepository">The payment repository.</param>
        /// <param name="notificationService">The notification service.</param>
        /// <param name="paymentProcessorFactory">The payment processor factory.</param>
        /// <param name="logger">The logger.</param>
        public BillingService(
            IInvoiceRepository invoiceRepository,
            IJobRepository jobRepository,
            ICustomerRepository customerRepository,
            IPaymentRepository paymentRepository,
            INotificationService notificationService,
            IPaymentProcessorFactory paymentProcessorFactory,
            ILogger<BillingService> logger)
        {
            this.invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
            this.jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            this.customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
            this.paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
            this.notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            this.paymentProcessorFactory = paymentProcessorFactory ?? throw new ArgumentNullException(nameof(paymentProcessorFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Configure retry policy for payment processing
            this.retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        logger.LogWarning(exception, "Payment processing failed, retrying in {RetryTimeSpan}. Retry attempt {RetryCount}", timeSpan, retryCount);
                    });
        }

        /// <inheritdoc/>
        public async Task<Invoice> GenerateInvoiceAsync(string jobId, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Generating invoice for job");

            var job = await jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                throw new ArgumentException($"Job {jobId} not found", nameof(jobId));
            }

            if (job.Status != JobStatus.Received)
            {
                throw new InvalidOperationException($"Cannot generate invoice for job with status: {job.Status}");
            }

            var customer = await customerRepository.GetByIdAsync(job.CustomerId);
            if (customer == null)
            {
                throw new ArgumentException($"Customer {job.CustomerId} not found");
            }

            // Check if invoice already exists for this job
            var existingInvoices = await invoiceRepository.GetByJobIdAsync(jobId);
            if (existingInvoices.Any())
            {
                logger.LogWarning("Invoice already exists for job");
                return existingInvoices.First();
            }

            // Generate invoice number
            var invoiceNumber = await invoiceRepository.GetNextInvoiceNumberAsync();

            // Create invoice
            var invoice = new Invoice
            {
                Id = Guid.NewGuid().ToString(),
                InvoiceNumber = invoiceNumber,
                CustomerId = job.CustomerId,
                JobId = jobId,
                InvoiceDate = DateTime.UtcNow.Date,
                Status = InvoiceStatus.Draft,
                Terms = new PaymentTerms
                {
                    Days = 30, // NET 30
                    EarlyPaymentDiscount = 0.02m, // 2% discount
                    EarlyPaymentDiscountDays = 10, // if paid within 10 days
                    LatePaymentPenalty = 0.015m, // 1.5% penalty
                    LatePaymentPenaltyDays = 5 // after 5 days past due
                },
                Notes = $"Invoice for job: {job.Title}"
            };

            invoice.DueDate = invoice.InvoiceDate.AddDays(invoice.Terms.Days);

            // Add line items
            await AddInvoiceLineItemsAsync(invoice, job);

            // Calculate tax
            var taxRate = await CalculateTaxAsync(invoice.SubTotal, customer.BillingAddress?.State ?? "CA", cancellationToken);
            invoice.TaxRate = taxRate;
            invoice.CalculateTotals();

            // Save invoice
            var createdInvoice = await invoiceRepository.AddAsync(invoice);

            // Send notification to customer
            try
            {
                await notificationService.SendNotificationAsync(
                    customer.Id,
                    NotificationType.InvoiceGenerated,
                    "New Invoice Generated",
                    $"Invoice {invoice.InvoiceNumber} has been generated for ${invoice.TotalAmount:F2}",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send invoice notification to customer");
            }

            logger.LogInformation("Invoice generated successfully: {InvoiceNumber} for job: {JobId}", invoiceNumber, jobId);
            return createdInvoice;
        }

        /// <inheritdoc/>
        public Task<decimal> CalculateTaxAsync(decimal amount, string state, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(state))
            {
                return Task.FromResult(0m);
            }

            var stateCode = state.ToUpperInvariant();
            if (stateTaxRates.TryGetValue(stateCode, out var taxRate))
            {
                return Task.FromResult(taxRate);
            }

            // Default tax rate if state not found
            logger.LogWarning("Tax rate not found for state: {State}, using default rate", state);
            return Task.FromResult(0.07m); // 7% default
        }

        /// <inheritdoc/>
        public async Task<bool> ProcessPaymentAsync(string invoiceId, decimal paymentAmount, string paymentMethod, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Processing payment of ${Amount} for invoice: {InvoiceId} using {PaymentMethod}", paymentAmount, invoiceId, paymentMethod);

            var invoice = await invoiceRepository.GetByIdAsync(invoiceId);
            if (invoice == null)
            {
                throw new ArgumentException($"Invoice {invoiceId} not found", nameof(invoiceId));
            }

            if (paymentAmount <= 0)
            {
                throw new ArgumentException("Payment amount must be greater than zero", nameof(paymentAmount));
            }

            if (paymentAmount > invoice.BalanceDue)
            {
                throw new ArgumentException("Payment amount cannot exceed balance due", nameof(paymentAmount));
            }

            // Create payment record
            var payment = new Payment
            {
                Id = Guid.NewGuid().ToString(),
                InvoiceId = invoiceId,
                Amount = paymentAmount,
                PaymentMethod = ParsePaymentMethod(paymentMethod),
                Status = PaymentStatus.Processing
            };

            await paymentRepository.AddAsync(payment);

            try
            {
                // Get the appropriate payment processor
                var processor = paymentProcessorFactory.GetProcessorForPaymentMethod(payment.PaymentMethod);

                // Prepare payment method details
                var paymentMethodDetails = new PaymentMethodDetails
                {
                    Type = payment.PaymentMethod,
                    Token = paymentMethod, // In real implementation, this would be a secure token
                    AdditionalData = new Dictionary<string, string>
                    {
                        ["invoice_id"] = invoiceId,
                        ["customer_id"] = invoice.CustomerId
                    }
                };

                // Process payment with retry logic
                var result = await retryPolicy.ExecuteAsync(async () =>
                {
                    var processingResult = await processor.ProcessPaymentAsync(
                        paymentAmount,
                        "USD", // Default currency
                        paymentMethodDetails,
                        new Dictionary<string, string>
                        {
                            ["invoice_number"] = invoice.InvoiceNumber,
                            ["customer_id"] = invoice.CustomerId
                        },
                        cancellationToken);

                    if (!processingResult.Success)
                    {
                        throw new Exception($"Payment processing failed: {processingResult.Message}");
                    }

                    return processingResult;
                });

                // Update payment with successful result
                payment.MarkAsCompleted(result.TransactionId, result.Message);
                await paymentRepository.UpdateAsync(payment);

                // Update invoice with payment
                var success = await invoiceRepository.UpdatePaidAmountAsync(invoiceId, invoice.PaidAmount + paymentAmount);
                if (!success)
                {
                    logger.LogError("Failed to update invoice payment amount for invoice: {InvoiceId}", invoiceId);
                    return false;
                }

                // Send payment confirmation notification
                try
                {
                    await notificationService.SendNotificationAsync(
                        invoice.CustomerId,
                        NotificationType.PaymentReceived,
                        "Payment Received",
                        $"Payment of ${paymentAmount:F2} received for invoice {invoice.InvoiceNumber}",
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send payment notification for invoice {InvoiceId}", invoiceId);
                }

                logger.LogInformation("Payment processed successfully for invoice: {InvoiceId}, TransactionId: {TransactionId}", invoiceId, result.TransactionId);
                return true;
            }
            catch (Exception ex)
            {
                // Update payment with failed status
                payment.MarkAsFailed(ex.Message);
                await paymentRepository.UpdateAsync(payment);

                logger.LogError(ex, "Payment processing failed for invoice: {InvoiceId}", invoiceId);
                return false;
            }
        }

        /// <inheritdoc/>
        public Task<IEnumerable<Invoice>> GetCustomerInvoicesAsync(string customerId, CancellationToken cancellationToken = default)
        {
            return invoiceRepository.GetByCustomerIdAsync(customerId);
        }

        /// <inheritdoc/>
        public async Task<Invoice?> GetInvoiceByIdAsync(string invoiceId, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Retrieving invoice: {InvoiceId}", invoiceId);
            return await invoiceRepository.GetByIdAsync(invoiceId, cancellationToken);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync(CancellationToken cancellationToken = default)
        {
            return invoiceRepository.GetOverdueInvoicesAsync();
        }

        /// <summary>
        /// Creates a custom invoice not tied to a specific job.
        /// </summary>
        /// <param name="customerId">The customer identifier.</param>
        /// <param name="lineItems">The line items for the invoice.</param>
        /// <param name="notes">Optional notes for the invoice.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created invoice.</returns>
        public async Task<Invoice> CreateCustomInvoiceAsync(string customerId, List<InvoiceLineItem> lineItems, string? notes = null, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Creating custom invoice for customer: {CustomerId}", customerId);

            var customer = await customerRepository.GetByIdAsync(customerId);
            if (customer == null)
            {
                throw new ArgumentException($"Customer {customerId} not found", nameof(customerId));
            }

            if (lineItems == null || !lineItems.Any())
            {
                throw new ArgumentException("At least one line item is required", nameof(lineItems));
            }

            // Generate invoice number
            var invoiceNumber = await invoiceRepository.GetNextInvoiceNumberAsync();

            // Create invoice
            var invoice = new Invoice
            {
                Id = Guid.NewGuid().ToString(),
                InvoiceNumber = invoiceNumber,
                CustomerId = customerId,
                JobId = null, // Custom invoice not tied to a job
                InvoiceDate = DateTime.UtcNow.Date,
                Status = InvoiceStatus.Draft,
                Terms = new PaymentTerms
                {
                    Days = 30, // NET 30 default
                    EarlyPaymentDiscount = 0.02m, // 2% discount
                    EarlyPaymentDiscountDays = 10, // if paid within 10 days
                    LatePaymentPenalty = 0.015m, // 1.5% penalty
                    LatePaymentPenaltyDays = 5 // after 5 days past due
                },
                Notes = notes ?? string.Empty
            };

            invoice.DueDate = invoice.InvoiceDate.AddDays(invoice.Terms.Days);

            // Add line items
            foreach (var lineItem in lineItems)
            {
                invoice.AddLineItem(lineItem);
            }

            // Calculate tax based on customer location
            // Note: Assuming customer has a State property or we use a default
            var customerState = "CA"; // Default to California - in real implementation, get from customer.BillingAddress
            var taxRate = await CalculateTaxAsync(invoice.SubTotal, customerState, cancellationToken);
            invoice.TaxRate = taxRate;
            invoice.CalculateTotals();

            // Save invoice
            var createdInvoice = await invoiceRepository.AddAsync(invoice);

            logger.LogInformation("Custom invoice created successfully: {InvoiceNumber} for customer: {CustomerId}", invoiceNumber, customerId);
            return createdInvoice;
        }

        /// <summary>
        /// Sends an invoice to the customer.
        /// </summary>
        /// <param name="invoiceId">The invoice identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the invoice was sent successfully.</returns>
        public async Task<bool> SendInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Sending invoice: {InvoiceId}", invoiceId);

            var invoice = await invoiceRepository.GetByIdAsync(invoiceId);
            if (invoice == null)
            {
                throw new ArgumentException($"Invoice {invoiceId} not found", nameof(invoiceId));
            }

            var customer = await customerRepository.GetByIdAsync(invoice.CustomerId);
            if (customer == null)
            {
                throw new ArgumentException($"Customer {invoice.CustomerId} not found");
            }

            try
            {
                // Update invoice status to sent
                invoice.Status = InvoiceStatus.Sent;
                invoice.UpdatedAt = DateTime.UtcNow;
                await invoiceRepository.UpdateAsync(invoice);

                // Send notification to customer
                await notificationService.SendNotificationAsync(
                    customer.Id,
                    NotificationType.InvoiceGenerated,
                    "Invoice Sent",
                    $"Invoice {invoice.InvoiceNumber} for ${invoice.TotalAmount:F2} has been sent. Due date: {invoice.DueDate:MM/dd/yyyy}",
                    cancellationToken);

                logger.LogInformation("Invoice sent successfully: {InvoiceNumber}", invoice.InvoiceNumber);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send invoice: {InvoiceId}", invoiceId);
                return false;
            }
        }

        /// <summary>
        /// Generates an account statement for a customer.
        /// </summary>
        /// <param name="customerId">The customer identifier.</param>
        /// <param name="startDate">The start date for the statement period.</param>
        /// <param name="endDate">The end date for the statement period.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The account statement data.</returns>
        public async Task<AccountStatement> GenerateAccountStatementAsync(string customerId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Generating account statement for customer: {CustomerId} from {StartDate} to {EndDate}", customerId, startDate, endDate);

            var customer = await customerRepository.GetByIdAsync(customerId);
            if (customer == null)
            {
                throw new ArgumentException($"Customer {customerId} not found", nameof(customerId));
            }

            var invoices = await invoiceRepository.GetByCustomerIdAsync(customerId);
            var statementInvoices = invoices.Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate).ToList();

            var statement = new AccountStatement
            {
                CustomerId = customerId,
                CustomerName = customer.CompanyName,
                StatementDate = DateTime.UtcNow,
                PeriodStart = startDate,
                PeriodEnd = endDate,
                Invoices = statementInvoices.ToList(),
                TotalInvoiced = statementInvoices.Sum(i => i.TotalAmount),
                TotalPaid = statementInvoices.Sum(i => i.PaidAmount),
                TotalOutstanding = statementInvoices.Sum(i => i.BalanceDue),
                OverdueAmount = statementInvoices.Where(i => i.IsOverdue()).Sum(i => i.BalanceDue)
            };

            logger.LogInformation("Account statement generated for customer: {CustomerId}, Total Outstanding: ${TotalOutstanding:F2}", customerId, statement.TotalOutstanding);
            return statement;
        }

        /// <summary>
        /// Applies late fees to overdue invoices.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of invoices that had late fees applied.</returns>
        public async Task<int> ApplyLateFeeAsync(CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Applying late fees to overdue invoices");

            var overdueInvoices = await invoiceRepository.GetOverdueInvoicesAsync();
            var feesApplied = 0;

            foreach (var invoice in overdueInvoices)
            {
                if (invoice is Invoice concreteInvoice)
                {
                    var latePenalty = concreteInvoice.CalculateLatePenalty();
                    if (latePenalty > 0)
                    {
                        // Add late fee as a line item
                        concreteInvoice.AddLineItem(new InvoiceLineItem
                        {
                            Description = $"Late Payment Fee ({concreteInvoice.DaysOverdue()} days overdue)",
                            Quantity = 1,
                            UnitPrice = latePenalty,
                            TaxApplicable = false
                        });

                        await invoiceRepository.UpdateAsync(concreteInvoice);
                        feesApplied++;

                        logger.LogInformation("Late fee of ${LateFee:F2} applied to invoice: {InvoiceNumber}", latePenalty, invoice.InvoiceNumber);
                    }
                }
            }

            logger.LogInformation("Late fees applied to {Count} invoices", feesApplied);
            return feesApplied;
        }

        /// <summary>
        /// Adds line items to an invoice based on the job details.
        /// </summary>
        /// <param name="invoice">The invoice to add line items to.</param>
        /// <param name="job">The job to create line items from.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task AddInvoiceLineItemsAsync(Invoice invoice, Job job)
        {
            // Base service charge
            if (job.Pricing?.BaseRate > 0)
            {
                invoice.AddLineItem(new InvoiceLineItem
                {
                    Description = "Base Service Charge",
                    Quantity = 1,
                    UnitPrice = job.Pricing.BaseRate,
                    TaxApplicable = true
                });
            }

            // Mileage charge
            if (job.Pricing?.MileageRate > 0)
            {
                // Calculate distance using location coordinates or estimate
                var distanceFromCoords = job.PickupLocation.DistanceTo(job.DeliveryLocation);
                var estimatedMiles = distanceFromCoords.HasValue
                    ? (decimal)distanceFromCoords.Value
                    : (decimal)await EstimateDistanceAsync(job.PickupLocation.FullAddress, job.DeliveryLocation.FullAddress);

                invoice.AddLineItem(new InvoiceLineItem
                {
                    Description = $"Mileage Charge ({estimatedMiles:F1} miles)",
                    Quantity = estimatedMiles,
                    UnitPrice = job.Pricing.MileageRate,
                    TaxApplicable = true
                });
            }

            // Fuel surcharge
            if (job.Pricing?.FuelSurcharge > 0)
            {
                invoice.AddLineItem(new InvoiceLineItem
                {
                    Description = "Fuel Surcharge",
                    Quantity = 1,
                    UnitPrice = job.Pricing.FuelSurcharge,
                    TaxApplicable = true
                });
            }

            // Toll charges
            if (job.Pricing?.TollCharges > 0)
            {
                invoice.AddLineItem(new InvoiceLineItem
                {
                    Description = "Toll Charges",
                    Quantity = 1,
                    UnitPrice = job.Pricing.TollCharges,
                    TaxApplicable = false // Tolls typically not taxed
                });
            }

            // Additional charges based on cargo
            if (job.Cargo?.Weight > 1000) // Over 1000 lbs
            {
                var overweightCharge = (job.Cargo.Weight - 1000) * 0.10m; // $0.10 per lb over 1000
                invoice.AddLineItem(new InvoiceLineItem
                {
                    Description = $"Overweight Charge ({job.Cargo.Weight - 1000:F1} lbs over standard)",
                    Quantity = 1,
                    UnitPrice = overweightCharge,
                    TaxApplicable = true
                });
            }

            // High-value cargo insurance
            if (job.Cargo?.Value > 10000) // Over $10,000
            {
                var insuranceCharge = job.Cargo.Value * 0.005m; // 0.5% of cargo value
                invoice.AddLineItem(new InvoiceLineItem
                {
                    Description = "High-Value Cargo Insurance",
                    Quantity = 1,
                    UnitPrice = insuranceCharge,
                    TaxApplicable = false // Insurance typically not taxed
                });
            }

            // Special handling charges
            if ((job.Cargo?.IsFragile == true) || (job.Cargo?.IsHazardous == true) || (job.Cargo?.RequiresTemperatureControl == true))
            {
                var specialHandlingCharge = (job.Pricing?.BaseRate ?? 100) * 0.15m; // 15% surcharge
                var requirements = job.Cargo.GetSpecialRequirements();

                invoice.AddLineItem(new InvoiceLineItem
                {
                    Description = $"Special Handling ({requirements})",
                    Quantity = 1,
                    UnitPrice = specialHandlingCharge,
                    TaxApplicable = true
                });
            }

            // Priority surcharge
            if (job.Priority == JobPriority.High)
            {
                var priorityCharge = (job.Pricing?.BaseRate ?? 100) * 0.25m; // 25% surcharge for high priority jobs
                invoice.AddLineItem(new InvoiceLineItem
                {
                    Description = "High Priority Surcharge",
                    Quantity = 1,
                    UnitPrice = priorityCharge,
                    TaxApplicable = true
                });
            }

            // Additional charges from pricing details
            if (job.Pricing?.AdditionalCharges > 0)
            {
                invoice.AddLineItem(new InvoiceLineItem
                {
                    Description = "Additional Charges",
                    Quantity = 1,
                    UnitPrice = job.Pricing.AdditionalCharges,
                    TaxApplicable = true
                });
            }
        }

        /// <summary>
        /// Estimates the distance between two addresses.
        /// </summary>
        /// <param name="fromAddress">The origin address.</param>
        /// <param name="toAddress">The destination address.</param>
        /// <returns>The estimated distance in miles.</returns>
        private Task<double> EstimateDistanceAsync(string fromAddress, string toAddress)
        {
            // Simplified distance calculation - in real implementation, use mapping service
            // For demo purposes, calculate based on address length as a rough estimate
            if (string.IsNullOrWhiteSpace(fromAddress) || string.IsNullOrWhiteSpace(toAddress))
            {
                return Task.FromResult(100.0); // Default 100 miles
            }

            // Simple heuristic: longer addresses are typically farther apart
            var addressLengthFactor = (fromAddress.Length + toAddress.Length) / 10.0;
            var estimatedDistance = Math.Max(10.0, Math.Min(500.0, addressLengthFactor * 5.0));

            return Task.FromResult(estimatedDistance);
        }

        /// <summary>
        /// Parses the payment method string to PaymentMethodType enum.
        /// </summary>
        /// <param name="paymentMethod">The payment method string.</param>
        /// <returns>The PaymentMethodType.</returns>
        private PaymentMethodType ParsePaymentMethod(string paymentMethod)
        {
            return paymentMethod.ToLowerInvariant() switch
            {
                "credit card" or "card" or "stripe" => PaymentMethodType.CreditCard,
                "paypal" => PaymentMethodType.DigitalWallet,
                "ach" or "bank transfer" => PaymentMethodType.ACH,
                "check" => PaymentMethodType.Check,
                "wire transfer" => PaymentMethodType.WireTransfer,
                "cash" => PaymentMethodType.Cash,
                _ => PaymentMethodType.CreditCard // Default to credit card
            };
        }
    }
}
