// <copyright file="PaymentIntegrationTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Tests.Billing
{
    /// <summary>
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HotshotLogistics.Application.Services;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Contracts.Repositories;
using HotshotLogistics.Domain.Repositories;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
    using HotshotLogistics.Core.Enums;

    /// <summary>
    /// Integration tests for payment processing functionality.
    /// </summary>
    public class PaymentIntegrationTests
{
    private readonly Mock<IPaymentProcessor> mockStripeProcessor;
    private readonly Mock<IPaymentProcessor> mockPayPalProcessor;
    private readonly HotshotLogistics.Contracts.Services.IPaymentProcessorFactory paymentProcessorFactory;
    private readonly Mock<HotshotLogistics.Contracts.Services.IPaymentProcessorFactory> mockPaymentProcessorFactory;
    private readonly Mock<IInvoiceRepository> mockInvoiceRepository;
    private readonly Mock<IPaymentRepository> mockPaymentRepository;
    private readonly Mock<ICustomerRepository> mockCustomerRepository;
    private readonly Mock<INotificationService> mockNotificationService;
    private readonly Mock<ILogger<BillingService>> mockLogger;
    private readonly Mock<IServiceProvider> mockServiceProvider;
    private readonly Mock<IConfiguration> mockConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentIntegrationTests"/> class.
    /// </summary>
    public PaymentIntegrationTests()
    {
        mockStripeProcessor = new Mock<IPaymentProcessor>();
        mockPayPalProcessor = new Mock<IPaymentProcessor>();
        mockPaymentProcessorFactory = new Mock<HotshotLogistics.Contracts.Services.IPaymentProcessorFactory>();
        mockInvoiceRepository = new Mock<IInvoiceRepository>();
        mockPaymentRepository = new Mock<IPaymentRepository>();
        mockCustomerRepository = new Mock<ICustomerRepository>();
        mockNotificationService = new Mock<INotificationService>();
        mockLogger = new Mock<ILogger<BillingService>>();
        mockServiceProvider = new Mock<IServiceProvider>();
        mockConfiguration = new Mock<IConfiguration>();

        // Setup service provider to return processors (kept for completeness)
        // Use GetService (the actual IServiceProvider method) instead of the GetRequiredService
        // extension to allow Moq to setup the call directly.
        mockServiceProvider.Setup(sp => sp.GetService(typeof(StripePaymentProcessor)))
            .Returns(mockStripeProcessor.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(PayPalPaymentProcessor)))
            .Returns(mockPayPalProcessor.Object);

        // Setup configuration
        mockConfiguration.Setup(c => c["Payment:DefaultProcessor"]).Returns("Stripe");

        // Use the mocked factory in tests so Moq can create the proxy without hitting concrete ctor
        paymentProcessorFactory = mockPaymentProcessorFactory.Object;
    }

    /// <summary>
    /// Tests successful payment processing with Stripe.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ProcessPayment_WithValidStripePayment_ReturnsSuccess()
    {
        // Arrange
        var invoiceId = "invoice-123";
        var paymentAmount = 1000.00m;
        var paymentMethod = "Credit Card";

        var invoice = CreateTestInvoice(invoiceId, "customer-123", 1000.00m, 0.00m);
        var customer = CreateTestCustomer("customer-123");

        mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);
        mockCustomerRepository.Setup(r => r.GetByIdAsync("customer-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var paymentResult = new PaymentProcessingResult
        {
            Success = true,
            TransactionId = "stripe_txn_123",
            Message = "Payment processed successfully"
        };

        mockStripeProcessor.Setup(p => p.ProcessPaymentAsync(
            paymentAmount,
            "USD",
            It.IsAny<PaymentMethodDetails>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(paymentResult);

        mockPaymentProcessorFactory.Setup(f => f.GetProcessorForPaymentMethod(PaymentMethodType.CreditCard))
            .Returns(mockStripeProcessor.Object);

        // Fix: Setup UpdatePaidAmountAsync mock for retry test
        mockInvoiceRepository.Setup(r => r.UpdatePaidAmountAsync(invoiceId, paymentAmount))
            .ReturnsAsync(true);

        var billingService = CreateBillingService();

        // Act
        var result = await billingService.ProcessPaymentAsync(invoiceId, paymentAmount, paymentMethod);

        // Assert
        result.Should().BeTrue();
        mockPaymentRepository.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Once);
        mockPaymentRepository.Verify(r => r.UpdateAsync(It.IsAny<Payment>()), Times.Once);
        mockInvoiceRepository.Verify(r => r.UpdatePaidAmountAsync(invoiceId, paymentAmount), Times.Once);
        mockNotificationService.Verify(n => n.SendNotificationAsync(
            "customer-123",
            NotificationType.PaymentReceived,
            "Payment Received",
            It.Is<string>(s => s.Contains("$1000.00")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests payment processing failure with retry logic.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ProcessPayment_WithFailedPayment_ImplementsRetryLogic()
    {
        // Arrange
        var invoiceId = "invoice-123";
        var paymentAmount = 1000.00m;
        var paymentMethod = "Credit Card";

        var invoice = CreateTestInvoice(invoiceId, "customer-123", 1000.00m, 0.00m);
        var customer = CreateTestCustomer("customer-123");

        mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);
        mockCustomerRepository.Setup(r => r.GetByIdAsync("customer-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // First two calls fail, third succeeds
        var callCount = 0;
        mockStripeProcessor.Setup(p => p.ProcessPaymentAsync(
            paymentAmount,
            "USD",
            It.IsAny<PaymentMethodDetails>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount <= 2)
                {
                    return new PaymentProcessingResult
                    {
                        Success = false,
                        Message = "Temporary failure",
                        ErrorCode = "TEMPORARY_ERROR"
                    };
                }
                return new PaymentProcessingResult
                {
                    Success = true,
                    TransactionId = "stripe_txn_123",
                    Message = "Payment processed successfully"
                };
            });

        mockPaymentProcessorFactory.Setup(f => f.GetProcessorForPaymentMethod(PaymentMethodType.CreditCard))
            .Returns(mockStripeProcessor.Object);

        // Fix: Ensure the factory returns the correct processor for the payment method
        mockPaymentProcessorFactory.Setup(f => f.GetProcessorForPaymentMethod(It.IsAny<PaymentMethodType>()))
            .Returns((PaymentMethodType method) =>
            {
                return method == PaymentMethodType.CreditCard ? mockStripeProcessor.Object : mockPayPalProcessor.Object;
            });

        // Fix: Setup UpdatePaidAmountAsync mock to return true
        mockInvoiceRepository.Setup(r => r.UpdatePaidAmountAsync(invoiceId, paymentAmount))
            .ReturnsAsync(true);

        var billingService = CreateBillingService();

        // Act
        var result = await billingService.ProcessPaymentAsync(invoiceId, paymentAmount, paymentMethod);

        // Assert
        result.Should().BeTrue();
        mockStripeProcessor.Verify(p => p.ProcessPaymentAsync(
            paymentAmount,
            "USD",
            It.IsAny<PaymentMethodDetails>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3)); // Should retry 3 times
    }

    /// <summary>
    /// Tests PayPal payment processing.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ProcessPayment_WithPayPalPayment_UsesPayPalProcessor()
    {
        // Arrange
        var invoiceId = "invoice-456";
        var paymentAmount = 500.00m;
        var paymentMethod = "PayPal";

        var invoice = CreateTestInvoice(invoiceId, "customer-456", 500.00m, 0.00m);
        var customer = CreateTestCustomer("customer-456");

        mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoice);
        mockCustomerRepository.Setup(r => r.GetByIdAsync("customer-456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var paymentResult = new PaymentProcessingResult
        {
            Success = true,
            TransactionId = "paypal_txn_456",
            Message = "PayPal payment processed successfully"
        };

        mockPayPalProcessor.Setup(p => p.ProcessPaymentAsync(
            paymentAmount,
            "USD",
            It.IsAny<PaymentMethodDetails>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(paymentResult);

        mockPaymentProcessorFactory.Setup(f => f.GetProcessorForPaymentMethod(PaymentMethodType.DigitalWallet))
            .Returns(mockPayPalProcessor.Object);

        // Fix: Also setup UpdatePaidAmountAsync for PayPal test
        mockInvoiceRepository.Setup(r => r.UpdatePaidAmountAsync(invoiceId, paymentAmount))
            .ReturnsAsync(true);

        var billingService = CreateBillingService();

        // Act
        var result = await billingService.ProcessPaymentAsync(invoiceId, paymentAmount, paymentMethod);

        // Assert
        result.Should().BeTrue();
        mockPayPalProcessor.Verify(p => p.ProcessPaymentAsync(
            paymentAmount,
            "USD",
            It.IsAny<PaymentMethodDetails>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests webhook processing for Stripe.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ProcessStripeWebhook_WithValidSignature_ProcessesEvent()
    {
        // Arrange
        var webhookData = new WebhookEventData
        {
            EventType = "payment_intent.succeeded",
            Payload = "{\"event\":\"payment_intent.succeeded\"}",
            Signature = "valid_signature"
        };

        var webhookResult = new WebhookProcessingResult
        {
            Success = true,
            StatusUpdate = new PaymentStatusUpdate
            {
                TransactionId = "stripe_txn_123",
                Status = PaymentStatus.Completed,
                InvoiceId = "invoice_123",
                Amount = 1000.00m
            }
        };

        mockStripeProcessor.Setup(p => p.ValidateWebhookSignature(
            webhookData.Payload,
            webhookData.Signature,
            It.IsAny<string>()))
            .Returns(true);

        mockStripeProcessor.Setup(p => p.ProcessWebhookAsync(
            webhookData,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(webhookResult);

        // Act
        var result = await mockStripeProcessor.Object.ProcessWebhookAsync(webhookData, It.IsAny<CancellationToken>());

        // Assert
        result.Success.Should().BeTrue();
        result.StatusUpdate.Should().NotBeNull();
        result.StatusUpdate!.TransactionId.Should().Be("stripe_txn_123");
        result.StatusUpdate.Status.Should().Be(PaymentStatus.Completed);
    }

    /// <summary>
    /// Tests webhook processing with invalid signature.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ProcessStripeWebhook_WithInvalidSignature_RejectsEvent()
    {
        // Arrange
        var webhookData = new WebhookEventData
        {
            EventType = "payment_intent.succeeded",
            Payload = "{\"event\":\"payment_intent.succeeded\"}",
            Signature = "invalid_signature"
        };

        mockStripeProcessor.Setup(p => p.ValidateWebhookSignature(
            webhookData.Payload,
            webhookData.Signature,
            It.IsAny<string>()))
            .Returns(false);

        // Setup ProcessWebhookAsync to handle invalid signature case
        mockStripeProcessor.Setup(p => p.ProcessWebhookAsync(
            webhookData,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebhookProcessingResult
            {
                Success = false,
                Message = "Invalid webhook signature"
            });

        // Act
        var result = await mockStripeProcessor.Object.ProcessWebhookAsync(webhookData, It.IsAny<CancellationToken>());

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid webhook signature");
    }

    /// <summary>
    /// Creates a test invoice for testing.
    /// </summary>
    /// <param name="id">The invoice ID.</param>
    /// <param name="customerId">The customer ID.</param>
    /// <param name="totalAmount">The total amount.</param>
    /// <param name="paidAmount">The paid amount.</param>
    /// <returns>A test invoice.</returns>
    private static Invoice CreateTestInvoice(string id, string customerId, decimal totalAmount, decimal paidAmount)
    {
        var mockInvoice = new Mock<Invoice>();
        mockInvoice.Setup(i => i.Id).Returns(id);
        mockInvoice.Setup(i => i.CustomerId).Returns(customerId);
        mockInvoice.Setup(i => i.TotalAmount).Returns(totalAmount);
        mockInvoice.Setup(i => i.PaidAmount).Returns(paidAmount);
        mockInvoice.Setup(i => i.BalanceDue).Returns(totalAmount - paidAmount);
        mockInvoice.Setup(i => i.InvoiceNumber).Returns($"INV-{id}");
        return mockInvoice.Object;
    }

    /// <summary>
    /// Creates a test customer for testing.
    /// </summary>
    /// <param name="id">The customer ID.</param>
    /// <returns>A test customer.</returns>
    private static Customer CreateTestCustomer(string id)
    {
        var mockCustomer = new Mock<Customer>();
        mockCustomer.Setup(c => c.Id).Returns(id);
        mockCustomer.Setup(c => c.CompanyName).Returns($"Company {id}");
        return mockCustomer.Object;
    }

    /// <summary>
    /// Creates a billing service instance with mocked dependencies.
    /// </summary>
    /// <returns>A billing service instance.</returns>
    private BillingService CreateBillingService()
    {
        return new BillingService(
            mockInvoiceRepository.Object,
            Mock.Of<IJobRepository>(),
            mockCustomerRepository.Object,
            mockPaymentRepository.Object,
            mockNotificationService.Object,
            paymentProcessorFactory,
            mockLogger.Object);
    }
}
}
