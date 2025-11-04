// <copyright file="CustomerControllerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Tests.Customer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using HotshotLogistics.Api.Controllers;
    using HotshotLogistics.Domain.Entities;
    using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Tests.TestHelpers;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;

    /// <summary>
    /// Integration tests for the CustomerController.
    /// </summary>
    public class CustomerControllerTests
    {
        private readonly Mock<ICustomerService> mockCustomerService;
        private readonly Mock<ILogger<CustomerController>> mockLogger;
        private readonly CustomerController controller;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomerControllerTests"/> class.
        /// </summary>
        public CustomerControllerTests()
        {
            mockCustomerService = new Mock<ICustomerService>();
            mockLogger = new Mock<ILogger<CustomerController>>();
            controller = new CustomerController(mockCustomerService.Object, mockLogger.Object);
        }

        /// <summary>
        /// Tests that GetCustomers returns all customers.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetCustomers_ReturnsAllCustomers()
        {
            // Arrange
            var expectedCustomers = new List<Customer>
            {
                CreateTestCustomer("customer1", "Test Company 1"),
                CreateTestCustomer("customer2", "Test Company 2")
            };

            mockCustomerService.Setup(s => s.GetCustomersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedCustomers);

            // Act
            var result = await controller.GetCustomers();

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedCustomers = okResult.Value.Should().BeAssignableTo<IEnumerable<Customer>>().Subject;
            returnedCustomers.Should().HaveCount(2);
        }

        /// <summary>
        /// Tests that GetCustomerById returns the customer when found.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetCustomerById_WhenCustomerExists_ReturnsCustomer()
        {
            // Arrange
            var customerId = "test-customer-id";
            var expectedCustomer = CreateTestCustomer(customerId, "Test Company");

            mockCustomerService.Setup(s => s.GetCustomerByIdAsync(customerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedCustomer);

            // Act
            var result = await controller.GetCustomerById(customerId);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedCustomer = okResult.Value.Should().BeAssignableTo<Customer>().Subject;
            returnedCustomer.Id.Should().Be(customerId);
        }

        /// <summary>
        /// Tests that GetCustomerById returns NotFound when customer doesn't exist.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetCustomerById_WhenCustomerNotFound_ReturnsNotFound()
        {
            // Arrange
            var customerId = "non-existent-customer";

            mockCustomerService.Setup(s => s.GetCustomerByIdAsync(customerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Customer?)null);

            // Act
            var result = await controller.GetCustomerById(customerId);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        /// <summary>
        /// Tests that CreateCustomer creates and returns the new customer.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task CreateCustomer_WithValidData_CreatesAndReturnsCustomer()
        {
            // Arrange
            var customerData = CreateTestCustomer("new-customer-id", "New Test Company");
            var createdCustomer = CreateTestCustomer("new-customer-id", "New Test Company");

            mockCustomerService.Setup(s => s.CreateCustomerAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdCustomer);

            // Act
            var result = await controller.CreateCustomer(customerData);

            // Assert
            result.Should().NotBeNull();
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var returnedCustomer = createdResult.Value.Should().BeAssignableTo<Customer>().Subject;
            returnedCustomer.Id.Should().Be("new-customer-id");
        }

        /// <summary>
        /// Tests that CreateCustomer returns BadRequest when customer data is null.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task CreateCustomer_WithNullData_ReturnsBadRequest()
        {
            // Act
            var result = await controller.CreateCustomer(null!);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Tests that CreateCustomer returns BadRequest when validation fails.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task CreateCustomer_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            var invalidCustomer = CreateTestCustomer("", ""); // Invalid data

            mockCustomerService.Setup(s => s.CreateCustomerAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Customer validation failed"));

            // Act
            var result = await controller.CreateCustomer(invalidCustomer);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Tests that UpdateCustomer updates and returns the customer.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task UpdateCustomer_WithValidData_UpdatesAndReturnsCustomer()
        {
            // Arrange
            var customerId = "existing-customer-id";
            var customerData = CreateTestCustomer(customerId, "Updated Company Name");
            var updatedCustomer = CreateTestCustomer(customerId, "Updated Company Name");

            mockCustomerService.Setup(s => s.UpdateCustomerAsync(customerId, It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(updatedCustomer);

            // Act
            var result = await controller.UpdateCustomer(customerId, customerData);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedCustomer = okResult.Value.Should().BeAssignableTo<Customer>().Subject;
            returnedCustomer.Id.Should().Be(customerId);
        }

        /// <summary>
        /// Tests that UpdateCustomer returns NotFound when customer doesn't exist.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task UpdateCustomer_WhenCustomerNotFound_ReturnsNotFound()
        {
            // Arrange
            var customerId = "non-existent-customer";
            var customerData = CreateTestCustomer(customerId, "Test Company");

            mockCustomerService.Setup(s => s.UpdateCustomerAsync(customerId, It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Customer?)null);

            // Act
            var result = await controller.UpdateCustomer(customerId, customerData);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        /// <summary>
        /// Tests that DeleteCustomer deletes the customer and returns NoContent.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task DeleteCustomer_WhenCustomerExists_ReturnsNoContent()
        {
            // Arrange
            var customerId = "customer-to-delete";

            mockCustomerService.Setup(s => s.DeleteCustomerAsync(customerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await controller.DeleteCustomer(customerId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        /// <summary>
        /// Tests that DeleteCustomer returns NotFound when customer doesn't exist.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task DeleteCustomer_WhenCustomerNotFound_ReturnsNotFound()
        {
            // Arrange
            var customerId = "non-existent-customer";

            mockCustomerService.Setup(s => s.DeleteCustomerAsync(customerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await controller.DeleteCustomer(customerId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        /// <summary>
        /// Tests that GetActiveCustomers returns active customers.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetActiveCustomers_ReturnsActiveCustomers()
        {
            // Arrange
            var activeCustomers = new List<Customer>
            {
                CreateTestCustomer("active1", "Active Company 1", true),
                CreateTestCustomer("active2", "Active Company 2", true)
            };

            mockCustomerService.Setup(s => s.GetActiveCustomersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(activeCustomers);

            // Act
            var result = await controller.GetActiveCustomers();

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedCustomers = okResult.Value.Should().BeAssignableTo<IEnumerable<Customer>>().Subject;
            returnedCustomers.Should().HaveCount(2);
            returnedCustomers.All(c => c.IsActive).Should().BeTrue();
        }

        /// <summary>
        /// Tests that GetOverdueCustomers returns customers with overdue invoices.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetOverdueCustomers_ReturnsOverdueCustomers()
        {
            // Arrange
            var overdueCustomers = new List<Customer>
            {
                CreateTestCustomer("overdue1", "Overdue Company 1"),
                CreateTestCustomer("overdue2", "Overdue Company 2")
            };

            mockCustomerService.Setup(s => s.GetOverdueCustomersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(overdueCustomers);

            // Act
            var result = await controller.GetOverdueCustomers();

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedCustomers = okResult.Value.Should().BeAssignableTo<IEnumerable<Customer>>().Subject;
            returnedCustomers.Should().HaveCount(2);
        }

        /// <summary>
        /// Tests that GetCustomerJobs returns jobs for the customer.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetCustomerJobs_WhenCustomerExists_ReturnsJobs()
        {
            // Arrange
            var customerId = "customer-with-jobs";
            var customer = CreateTestCustomer(customerId, "Test Company");
            var jobs = new List<Job>
            {
                CreateTestJob("job1", customerId),
                CreateTestJob("job2", customerId)
            };

            mockCustomerService.Setup(s => s.GetCustomerByIdAsync(customerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);
            mockCustomerService.Setup(s => s.GetCustomerJobsAsync(customerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(jobs);

            // Act
            var result = await controller.GetCustomerJobs(customerId);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedJobs = okResult.Value.Should().BeAssignableTo<IEnumerable<Job>>().Subject;
            returnedJobs.Should().HaveCount(2);
            returnedJobs.All(j => j.CustomerId == customerId).Should().BeTrue();
        }

        /// <summary>
        /// Tests that GetCustomerInvoices returns invoices for the customer.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetCustomerInvoices_WhenCustomerExists_ReturnsInvoices()
        {
            // Arrange
            var customerId = "customer-with-invoices";
            var customer = CreateTestCustomer(customerId, "Test Company");
            var invoices = new List<Invoice>
            {
                CreateTestInvoice("invoice1", customerId),
                CreateTestInvoice("invoice2", customerId)
            };

            mockCustomerService.Setup(s => s.GetCustomerByIdAsync(customerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);
            mockCustomerService.Setup(s => s.GetCustomerInvoicesAsync(customerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invoices);

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
        /// Tests that UpdateCreditLimit updates the credit limit successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task UpdateCreditLimit_WithValidRequest_ReturnsNoContent()
        {
            // Arrange
            var customerId = "customer-to-update";
            var request = new UpdateCreditLimitRequest { NewLimit = 50000m };

            mockCustomerService.Setup(s => s.UpdateCreditLimitAsync(customerId, request.NewLimit, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await controller.UpdateCreditLimit(customerId, request);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        /// <summary>
        /// Tests that UpdateCreditLimit returns BadRequest for negative credit limit.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task UpdateCreditLimit_WithNegativeLimit_ReturnsBadRequest()
        {
            // Arrange
            var customerId = "customer-to-update";
            var request = new UpdateCreditLimitRequest { NewLimit = -1000m };

            // Act
            var result = await controller.UpdateCreditLimit(customerId, request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Tests that UpdateCreditTerms updates the credit terms successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task UpdateCreditTerms_WithValidTerms_ReturnsNoContent()
        {
            // Arrange
            var customerId = "customer-to-update";
            var creditTerms = new CreditTerms
            {
                PaymentTermsDays = 45,
                Status = CreditStatus.Approved,
                ApprovedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddYears(1)
            };

            mockCustomerService.Setup(s => s.UpdateCreditTermsAsync(customerId, creditTerms, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await controller.UpdateCreditTerms(customerId, creditTerms);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        /// <summary>
        /// Tests that UpdateCreditTerms returns BadRequest for null credit terms.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task UpdateCreditTerms_WithNullTerms_ReturnsBadRequest()
        {
            // Arrange
            var customerId = "customer-to-update";

            // Act
            var result = await controller.UpdateCreditTerms(customerId, null!);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Tests that UpdateCreditTerms returns BadRequest for invalid payment terms.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task UpdateCreditTerms_WithInvalidPaymentTerms_ReturnsBadRequest()
        {
            // Arrange
            var customerId = "customer-to-update";
            var creditTerms = new CreditTerms
            {
                PaymentTermsDays = 0, // Invalid
                Status = CreditStatus.Approved
            };

            // Act
            var result = await controller.UpdateCreditTerms(customerId, creditTerms);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// Tests that UpdateCreditTerms returns NotFound when customer doesn't exist.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task UpdateCreditTerms_WhenCustomerNotFound_ReturnsNotFound()
        {
            // Arrange
            var customerId = "non-existent-customer";
            var creditTerms = new CreditTerms
            {
                PaymentTermsDays = 30,
                Status = CreditStatus.Approved
            };

            mockCustomerService.Setup(s => s.UpdateCreditTermsAsync(customerId, creditTerms, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await controller.UpdateCreditTerms(customerId, creditTerms);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        /// <summary>
        /// Creates a test customer for testing purposes.
        /// </summary>
        /// <param name="id">The customer ID.</param>
        /// <param name="companyName">The company name.</param>
        /// <param name="isActive">Whether the customer is active.</param>
        /// <returns>A test customer instance.</returns>
        private static Customer CreateTestCustomer(string id, string companyName, bool isActive = true)
        {
            return CustomerBuilder.New()
                .WithId(id)
                .WithCompanyName(companyName)
                .WithIsActive(isActive)
                .WithCreditLimit(10000m)
                .WithContacts(new List<Contact> { new Contact { Email = $"{id}@test.com", Phone = "555-0123", IsPrimary = true } })
                .WithBillingAddress(new Address())
                .Build();
        }

        /// <summary>
        /// Creates a test job for testing purposes.
        /// </summary>
        /// <param name="id">The job ID.</param>
        /// <param name="customerId">The customer ID.</param>
        /// <returns>A test job instance.</returns>
        private static Job CreateTestJob(string id, string customerId)
        {
            var mockJob = new Mock<Job>();
            mockJob.Setup(j => j.Id).Returns(id);
            mockJob.Setup(j => j.CustomerId).Returns(customerId);
            mockJob.Setup(j => j.Title).Returns($"Test Job {id}");
            mockJob.Setup(j => j.Status).Returns(JobStatus.Pending);
            mockJob.Setup(j => j.CreatedAt).Returns(DateTime.UtcNow);
            return mockJob.Object;
        }

        /// <summary>
        /// Creates a test invoice for testing purposes.
        /// </summary>
        /// <param name="id">The invoice ID.</param>
        /// <param name="customerId">The customer ID.</param>
        /// <returns>A test invoice instance.</returns>
        private static Invoice CreateTestInvoice(string id, string customerId)
        {
            var mockInvoice = new Mock<Invoice>();
            mockInvoice.Setup(i => i.Id).Returns(id);
            mockInvoice.Setup(i => i.CustomerId).Returns(customerId);
            mockInvoice.Setup(i => i.InvoiceNumber).Returns($"INV-{id}");
            mockInvoice.Setup(i => i.TotalAmount).Returns(1000m);
            mockInvoice.Setup(i => i.Status).Returns(InvoiceStatus.Sent);
            mockInvoice.Setup(i => i.CreatedAt).Returns(DateTime.UtcNow);
            return mockInvoice.Object;
        }
    }
}
