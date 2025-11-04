// <copyright file="BillingControllerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HotshotLogistics.Api.Controllers;
using HotshotLogistics.Application.Services;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HotshotLogistics.Tests.Billing
{
    /// <summary>
    /// Integration tests for the BillingController.
    /// </summary>
    public class BillingControllerTests
    {
        private readonly Mock<IBillingService> mockBillingService;
        private readonly Mock<HotshotLogistics.Contracts.Services.IPaymentProcessorFactory> mockPaymentProcessorFactory;
        private readonly Mock<ILogger<BillingController>> mockLogger;
        private readonly BillingController controller;

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingControllerTests"/> class.
        /// </summary>
        public BillingControllerTests()
        {
            mockBillingService = new Mock<IBillingService>();
            mockPaymentProcessorFactory = new Mock<HotshotLogistics.Contracts.Services.IPaymentProcessorFactory>();
            mockLogger = new Mock<ILogger<BillingController>>();
            controller = new BillingController(mockBillingService.Object, mockPaymentProcessorFactory.Object, mockLogger.Object);
        }

        /// <summary>
        /// Tests that GenerateInvoice creates and returns an invoice.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GenerateInvoice_WithValidJobId_CreatesAndReturnsInvoice()
        {
            // Arrange
            var jobId = "test-job-id";
            var expectedInvoice = CreateTestInvoice("invoice-1", "customer-1");

            mockBillingService.Setup(s => s.GenerateInvoiceAsync(jobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedInvoice);

            // Act
            var result = await controller.GenerateInvoice(jobId);

            // Assert
            result.Should().NotBeNull();
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var returnedInvoice = createdResult.Value.Should().BeAssignableTo<Invoice>().Subject;
            returnedInvoice.Id.Should().Be("invoice-1");
        }

        /// <summary>
        /// Tests that GenerateInvoice returns BadRequest for invalid job ID.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GenerateInvoice_WithInvalidJobId_ReturnsBadRequest()
        {
            // Arrange
            var jobId = "invalid-job-id";

            mockBillingService.Setup(s => s.GenerateInvoiceAsync(jobId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Invalid job ID"));

            // Act
            var result = await controller.GenerateInvoice(jobId);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Tests that GenerateInvoice returns NotFound when job doesn't exist.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GenerateInvoice_WhenJobNotFound_ReturnsNotFound()
        {
            // Arrange
            var jobId = "non-existent-job";

            mockBillingService.Setup(s => s.GenerateInvoiceAsync(jobId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException("Job not found"));

            // Act
            var result = await controller.GenerateInvoice(jobId);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        /// <summary>
        /// Tests that GetCustomerInvoices returns invoices for the customer.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetCustomerInvoices_ReturnsCustomerInvoices()
        {
            // Arrange
            var customerId = "customer-1";
            var expectedInvoices = new List<Invoice>
            {
                CreateTestInvoice("invoice-1", customerId),
                CreateTestInvoice("invoice-2", customerId)
            };

            mockBillingService.Setup(s => s.GetCustomerInvoicesAsync(customerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedInvoices);

            // Act
            var result = await controller.GetCustomerInvoices(customerId);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedInvoices = okResult.Value.Should().BeAssignableTo<IEnumerable<Invoice>>().Subject;
            returnedInvoices.Should().HaveCount(2);
            returnedInvoices.All(i => i.CustomerId == customerId).Should().BeTrue();
        }

        /// <summary>
        /// Tests that GetOverdueInvoices returns overdue invoices.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetOverdueInvoices_ReturnsOverdueInvoices()
        {
            // Arrange
            var overdueInvoices = new List<Invoice>
            {
                CreateTestInvoice("overdue-1", "customer-1", InvoiceStatus.Overdue),
                CreateTestInvoice("overdue-2", "customer-2", InvoiceStatus.Overdue)
            };

            mockBillingService.Setup(s => s.GetOverdueInvoicesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(overdueInvoices);

            // Act
            var result = await controller.GetOverdueInvoices();

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedInvoices = okResult.Value.Should().BeAssignableTo<IEnumerable<Invoice>>().Subject;
            returnedInvoices.Should().HaveCount(2);
            returnedInvoices.All(i => i.Status == InvoiceStatus.Overdue).Should().BeTrue();
        }

        /// <summary>
        /// Tests that ProcessPayment processes payment successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ProcessPayment_WithValidRequest_ProcessesPaymentSuccessfully()
        {
            // Arrange
            var invoiceId = "invoice-1";
            var request = new ProcessPaymentRequest
            {
                Amount = 1000m,
                PaymentMethod = "Credit Card",
                Reference = "REF123"
            };

            mockBillingService.Setup(s => s.ProcessPaymentAsync(invoiceId, request.Amount, request.PaymentMethod, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await controller.ProcessPayment(invoiceId, request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var paymentResult = okResult.Value.Should().BeOfType<PaymentResult>().Subject;
            paymentResult.Success.Should().BeTrue();
            paymentResult.InvoiceId.Should().Be(invoiceId);
            paymentResult.Amount.Should().Be(request.Amount);
        }

        /// <summary>
        /// Tests that ProcessPayment returns BadRequest for null request.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ProcessPayment_WithNullRequest_ReturnsBadRequest()
        {
            // Arrange
            var invoiceId = "invoice-1";

            // Act
            var result = await controller.ProcessPayment(invoiceId, null!);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Tests that ProcessPayment returns BadRequest for zero amount.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ProcessPayment_WithZeroAmount_ReturnsBadRequest()
        {
            // Arrange
            var invoiceId = "invoice-1";
            var request = new ProcessPaymentRequest
            {
                Amount = 0m,
                PaymentMethod = "Credit Card"
            };

            // Act
            var result = await controller.ProcessPayment(invoiceId, request);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Tests that ProcessPayment returns BadRequest for empty payment method.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ProcessPayment_WithEmptyPaymentMethod_ReturnsBadRequest()
        {
            // Arrange
            var invoiceId = "invoice-1";
            var request = new ProcessPaymentRequest
            {
                Amount = 1000m,
                PaymentMethod = ""
            };

            // Act
            var result = await controller.ProcessPayment(invoiceId, request);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Tests that CalculateTax calculates tax correctly.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task CalculateTax_WithValidRequest_CalculatesTaxCorrectly()
        {
            // Arrange
            var request = new TaxCalculationRequest
            {
                Amount = 1000m,
                State = "CA"
            };
            var expectedTaxAmount = 87.5m; // 8.75% tax rate

            mockBillingService.Setup(s => s.CalculateTaxAsync(request.Amount, request.State, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTaxAmount);

            // Act
            var result = await controller.CalculateTax(request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var taxResult = okResult.Value.Should().BeOfType<TaxCalculationResult>().Subject;
            taxResult.Amount.Should().Be(request.Amount);
            taxResult.State.Should().Be(request.State);
            taxResult.TaxAmount.Should().Be(expectedTaxAmount);
            taxResult.TotalAmount.Should().Be(request.Amount + expectedTaxAmount);
        }

        /// <summary>
        /// Tests that CalculateTax returns BadRequest for null request.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task CalculateTax_WithNullRequest_ReturnsBadRequest()
        {
            // Act
            var result = await controller.CalculateTax(null!);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Tests that CalculateTax returns BadRequest for zero amount.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task CalculateTax_WithZeroAmount_ReturnsBadRequest()
        {
            // Arrange
            var request = new TaxCalculationRequest
            {
                Amount = 0m,
                State = "CA"
            };

            // Act
            var result = await controller.CalculateTax(request);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Tests that GetAccountsReceivableReport returns report with overdue invoices.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetAccountsReceivableReport_ReturnsReportWithOverdueInvoices()
        {
            // Arrange
            var overdueInvoices = new List<Invoice>
            {
                CreateTestInvoice("overdue-1", "customer-1", InvoiceStatus.Overdue, 1000m, 500m),
                CreateTestInvoice("overdue-2", "customer-2", InvoiceStatus.Overdue, 2000m, 1500m)
            };

            mockBillingService.Setup(s => s.GetOverdueInvoicesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(overdueInvoices);

            // Act
            var result = await controller.GetAccountsReceivableReport();

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var report = okResult.Value.Should().BeOfType<AccountsReceivableReport>().Subject;
            report.TotalOverdueAmount.Should().Be(2000m); // 500 + 1500
            report.OverdueInvoiceCount.Should().Be(2);
            report.OverdueInvoices.Should().HaveCount(2);
        }

        /// <summary>
        /// Creates a test invoice for testing purposes.
        /// </summary>
        /// <param name="id">The invoice ID.</param>
        /// <param name="customerId">The customer ID.</param>
        /// <param name="status">The invoice status.</param>
        /// <param name="totalAmount">The total amount.</param>
        /// <param name="balanceDue">The balance due.</param>
        /// <returns>A test invoice instance.</returns>
        private static Invoice CreateTestInvoice(string id, string customerId, InvoiceStatus status = InvoiceStatus.Sent, decimal totalAmount = 1000m, decimal balanceDue = 1000m)
        {
            var mockInvoice = new Mock<Invoice>();
            mockInvoice.Setup(i => i.Id).Returns(id);
            mockInvoice.Setup(i => i.CustomerId).Returns(customerId);
            mockInvoice.Setup(i => i.InvoiceNumber).Returns($"INV-{id}");
            mockInvoice.Setup(i => i.Status).Returns(status);
            mockInvoice.Setup(i => i.TotalAmount).Returns(totalAmount);
            mockInvoice.Setup(i => i.BalanceDue).Returns(balanceDue);
            mockInvoice.Setup(i => i.DueDate).Returns(DateTime.UtcNow.AddDays(-30));
            mockInvoice.Setup(i => i.CreatedAt).Returns(DateTime.UtcNow.AddDays(-35));
            mockInvoice.Setup(i => i.LineItems).Returns(new List<InvoiceLineItem>());
            return mockInvoice.Object;
        }
    }
}
