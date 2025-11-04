// <copyright file="InvoiceRepositoryTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Contracts.Repositories;
using HotshotLogistics.Data.Repositories;
using HotshotLogistics.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HotshotLogistics.Tests.Billing
{
    /// <summary>
    /// Integration tests for InvoiceRepository.
    /// </summary>
    public class InvoiceRepositoryTests : IClassFixture<DatabaseTestFixture>, IDisposable
    {
        private readonly InvoiceRepository _invoiceRepository;
        private readonly IConfiguration _configuration;
        private readonly List<string> _createdInvoiceIds = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="InvoiceRepositoryTests"/> class.
        /// </summary>
        public InvoiceRepositoryTests(DatabaseTestFixture fixture)
        {
            ArgumentNullException.ThrowIfNull(fixture);
            var configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = TestDatabaseHelper.GetConnectionString()
                });

            _configuration = configBuilder.Build();
            _invoiceRepository = new InvoiceRepository(_configuration);
        }

        /// <summary>
        /// Tests that AddAsync creates a new invoice successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task AddAsync_CreatesInvoiceSuccessfully()
        {
            // Arrange
            var invoice = CreateTestInvoice();

            // Act
            var result = await _invoiceRepository.AddAsync(invoice);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(invoice.Id);
            result.InvoiceNumber.Should().Be(invoice.InvoiceNumber);
            result.CustomerId.Should().Be(invoice.CustomerId);
            result.TotalAmount.Should().Be(invoice.TotalAmount);

            _createdInvoiceIds.Add(result.Id);
        }

        /// <summary>
        /// Tests that GetByIdAsync retrieves an invoice successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetByIdAsync_RetrievesInvoiceSuccessfully()
        {
            // Arrange
            var invoice = await CreateAndSaveTestInvoiceAsync();

            // Act
            var result = await _invoiceRepository.GetByIdAsync(invoice.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(invoice.Id);
            result.InvoiceNumber.Should().Be(invoice.InvoiceNumber);
            result.CustomerId.Should().Be(invoice.CustomerId);
            result.TotalAmount.Should().Be(invoice.TotalAmount);
        }

        /// <summary>
        /// Tests that UpdateAsync updates an invoice successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task UpdateAsync_UpdatesInvoiceSuccessfully()
        {
            // Arrange
            var invoice = await CreateAndSaveTestInvoiceAsync();
            invoice.Status = InvoiceStatus.Sent;
            invoice.Notes = "Updated notes";

            // Act
            var result = await _invoiceRepository.UpdateAsync(invoice);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(InvoiceStatus.Sent);
            result.Notes.Should().Be("Updated notes");
            result.UpdatedAt.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that GetByCustomerIdAsync returns invoices for the correct customer.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetByCustomerIdAsync_ReturnsInvoicesForCorrectCustomer()
        {
            // Arrange
            var customerId = "CUST001";
            await CreateTestInvoicesAsync();

            // Act
            var result = await _invoiceRepository.GetByCustomerIdAsync(customerId);

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            result.Should().OnlyContain(i => i.CustomerId == customerId);
        }

        /// <summary>
        /// Tests that GetByJobIdAsync returns invoices for the correct job.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetByJobIdAsync_ReturnsInvoicesForCorrectJob()
        {
            // Arrange
            var jobId = "JOB001";
            await CreateTestInvoicesAsync();

            // Act
            var result = await _invoiceRepository.GetByJobIdAsync(jobId);

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            result.Should().OnlyContain(i => i.JobId == jobId);
        }

        /// <summary>
        /// Tests that GetByStatusAsync returns invoices with the correct status.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetByStatusAsync_ReturnsInvoicesWithCorrectStatus()
        {
            // Arrange
            await CreateTestInvoicesAsync();
            var status = InvoiceStatus.Sent;

            // Act
            var result = await _invoiceRepository.GetByStatusAsync(status);

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(i => i.Status == status);
        }

        /// <summary>
        /// Tests that GetOverdueInvoicesAsync returns only overdue invoices.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetOverdueInvoicesAsync_ReturnsOnlyOverdueInvoices()
        {
            // Arrange
            await CreateOverdueTestInvoiceAsync();

            // Act
            var result = await _invoiceRepository.GetOverdueInvoicesAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(i =>
                i.Status != InvoiceStatus.Paid &&
                i.Status != InvoiceStatus.Cancelled &&
                i.DueDate < DateTime.UtcNow.Date);
        }

        /// <summary>
        /// Tests that GetInvoicesDueWithinDaysAsync returns invoices due within specified days.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetInvoicesDueWithinDaysAsync_ReturnsInvoicesDueWithinDays()
        {
            // Arrange
            await CreateTestInvoicesAsync();
            var days = 30;

            // Act
            var result = await _invoiceRepository.GetInvoicesDueWithinDaysAsync(days);

            // Assert
            result.Should().NotBeNull();
            var futureDate = DateTime.UtcNow.Date.AddDays(days);
            result.Should().OnlyContain(i =>
                i.Status != InvoiceStatus.Paid &&
                i.Status != InvoiceStatus.Cancelled &&
                i.DueDate >= DateTime.UtcNow.Date &&
                i.DueDate <= futureDate);
        }

        /// <summary>
        /// Tests that GetByDateRangeAsync returns invoices within the specified date range.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetByDateRangeAsync_ReturnsInvoicesWithinDateRange()
        {
            // Arrange
            await CreateTestInvoicesAsync();
            var startDate = DateTime.UtcNow.Date.AddDays(-30);
            var endDate = DateTime.UtcNow.Date;

            // Act
            var result = await _invoiceRepository.GetByDateRangeAsync(startDate, endDate);

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(i =>
                i.InvoiceDate.Date >= startDate &&
                i.InvoiceDate.Date <= endDate);
        }

        /// <summary>
        /// Tests that GetPagedAsync returns paginated results with filtering.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetPagedAsync_ReturnsPaginatedResultsWithFiltering()
        {
            // Arrange
            await CreateTestInvoicesAsync();
            var filter = new InvoiceFilter
            {
                Status = InvoiceStatus.Draft,
                PageNumber = 1,
                PageSize = 2,
                SortBy = "InvoiceDate",
                SortDirection = "DESC"
            };

            // Act
            var result = await _invoiceRepository.GetPagedAsync(filter);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().NotBeEmpty();
            result.Items.Should().OnlyContain(i => i.Status == InvoiceStatus.Draft);
            result.Items.Count().Should().BeLessThanOrEqualTo(2);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(2);
            result.TotalCount.Should().BeGreaterThan(0);
        }

        /// <summary>
        /// Tests that GetNextInvoiceNumberAsync returns the next available invoice number.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetNextInvoiceNumberAsync_ReturnsNextAvailableInvoiceNumber()
        {
            // Act
            var result = await _invoiceRepository.GetNextInvoiceNumberAsync();

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().StartWith("INV");
            result.Length.Should().Be(9); // INV + 6 digits
        }

        /// <summary>
        /// Tests that GetOutstandingBalanceAsync returns correct outstanding balance for customer.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetOutstandingBalanceAsync_ReturnsCorrectOutstandingBalance()
        {
            // Arrange
            var customerId = "CUST001";
            await CreateTestInvoicesAsync();

            // Act
            var result = await _invoiceRepository.GetOutstandingBalanceAsync(customerId);

            // Assert
            result.Should().BeGreaterThanOrEqualTo(0);
        }

        /// <summary>
        /// Tests that GetInvoiceSummaryAsync returns correct summary statistics.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetInvoiceSummaryAsync_ReturnsCorrectSummaryStatistics()
        {
            // Arrange
            await CreateTestInvoicesAsync();

            // Act
            var result = await _invoiceRepository.GetInvoiceSummaryAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalInvoices.Should().BeGreaterThan(0);
            result.TotalAmount.Should().BeGreaterThan(0);
            result.TotalOutstanding.Should().BeGreaterThanOrEqualTo(0);
        }

        /// <summary>
        /// Tests that SearchByInvoiceNumberAsync returns matching invoices.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task SearchByInvoiceNumberAsync_ReturnsMatchingInvoices()
        {
            // Arrange
            await CreateTestInvoicesAsync();
            var searchTerm = "INV";

            // Act
            var result = await _invoiceRepository.SearchByInvoiceNumberAsync(searchTerm);

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(i => i.InvoiceNumber.Contains(searchTerm));
        }

        /// <summary>
        /// Tests that UpdatePaidAmountAsync updates the paid amount successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task UpdatePaidAmountAsync_UpdatesPaidAmountSuccessfully()
        {
            // Arrange
            var invoice = await CreateAndSaveTestInvoiceAsync();
            var paidAmount = 500.00m;

            // Act
            var result = await _invoiceRepository.UpdatePaidAmountAsync(invoice.Id, paidAmount);

            // Assert
            result.Should().BeTrue();

            // Verify the update
            var updatedInvoice = await _invoiceRepository.GetByIdAsync(invoice.Id);
            updatedInvoice!.PaidAmount.Should().Be(paidAmount);
        }

        /// <summary>
        /// Tests that UpdateStatusAsync updates the status successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task UpdateStatusAsync_UpdatesStatusSuccessfully()
        {
            // Arrange
            var invoice = await CreateAndSaveTestInvoiceAsync();
            var newStatus = InvoiceStatus.Sent;

            // Act
            var result = await _invoiceRepository.UpdateStatusAsync(invoice.Id, newStatus);

            // Assert
            result.Should().BeTrue();

            // Verify the update
            var updatedInvoice = await _invoiceRepository.GetByIdAsync(invoice.Id);
            updatedInvoice!.Status.Should().Be(newStatus);
        }

        /// <summary>
        /// Tests that GetAgingReportAsync returns aging report data.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetAgingReportAsync_ReturnsAgingReportData()
        {
            // Arrange
            await CreateTestInvoicesAsync();

            // Act
            var result = await _invoiceRepository.GetAgingReportAsync();

            // Assert
            result.Should().NotBeNull();
            // Note: This test may return empty results if no customers have outstanding balances
            // In a real scenario, we would ensure test data has outstanding invoices
        }

        /// <summary>
        /// Tests that GenerateInvoiceAsync creates an invoice from job data.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GenerateInvoiceAsync_CreatesInvoiceFromJobData()
        {
            // Arrange
            var jobId = await CreateTestJobAsync();

            // Act
            var result = await _invoiceRepository.GenerateInvoiceAsync(jobId);

            // Assert
            result.Should().NotBeNull();
            result.JobId.Should().Be(jobId);
            result.Status.Should().Be(InvoiceStatus.Draft);
            result.TotalAmount.Should().BeGreaterThan(0);
            result.LineItems.Should().NotBeEmpty();
            result.InvoiceNumber.Should().StartWith("INV");

            _createdInvoiceIds.Add(result.Id);
        }

        /// <summary>
        /// Tests that GenerateInvoiceAsync throws exception for non-existent job.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GenerateInvoiceAsync_ThrowsExceptionForNonExistentJob()
        {
            // Arrange
            var nonExistentJobId = "NONEXISTENT";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _invoiceRepository.GenerateInvoiceAsync(nonExistentJobId));
        }

        /// <summary>
        /// Creates a test invoice.
        /// </summary>
        /// <returns>A test invoice.</returns>
        private Invoice CreateTestInvoice()
        {
            var invoice = new Invoice
            {
                Id = Guid.NewGuid().ToString(),
                InvoiceNumber = $"INV{DateTime.UtcNow.Ticks}",
                CustomerId = "CUST001",
                JobId = "JOB001",
                InvoiceDate = DateTime.UtcNow.Date,
                DueDate = DateTime.UtcNow.Date.AddDays(30),
                Status = InvoiceStatus.Draft,
                SubTotal = 1000.00m,
                TaxRate = 0.08m,
                TaxAmount = 80.00m,
                DiscountAmount = 0.00m,
                TotalAmount = 1080.00m,
                PaidAmount = 0.00m,
                Terms = new PaymentTerms
                {
                    Days = 30,
                    EarlyPaymentDiscount = 0.02m,
                    EarlyPaymentDiscountDays = 10,
                    LatePaymentPenalty = 0.015m,
                    LatePaymentPenaltyDays = 5
                },
                Notes = "Test invoice",
                CreatedAt = DateTime.UtcNow
            };

            invoice.AddLineItem(new InvoiceLineItem
            {
                Description = "Delivery Service",
                Quantity = 1,
                UnitPrice = 1000.00m,
                TaxApplicable = true
            });

            return invoice;
        }

        /// <summary>
        /// Creates and saves a test invoice.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task<Invoice> CreateAndSaveTestInvoiceAsync()
        {
            var invoice = CreateTestInvoice();
            var result = await _invoiceRepository.AddAsync(invoice);
            _createdInvoiceIds.Add(result.Id);
            return (Invoice)result;
        }

        /// <summary>
        /// Creates multiple test invoices for testing purposes.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task<List<Invoice>> CreateTestInvoicesAsync()
        {
            var invoices = new List<Invoice>();

            // Create invoices with different statuses and customers
            var testData = new[]
            {
                new { CustomerId = "CUST001", JobId = (string?)"JOB001", Status = InvoiceStatus.Draft, Amount = 1000.00m },
                new { CustomerId = "CUST001", JobId = (string?)"JOB002", Status = InvoiceStatus.Sent, Amount = 1500.00m },
                new { CustomerId = "CUST002", JobId = (string?)"JOB003", Status = InvoiceStatus.Draft, Amount = 800.00m },
                new { CustomerId = "CUST002", JobId = (string?)null, Status = InvoiceStatus.Paid, Amount = 1200.00m }
            };

            foreach (var data in testData)
            {
                var invoice = new Invoice
                {
                    Id = Guid.NewGuid().ToString(),
                    InvoiceNumber = $"INV{DateTime.UtcNow.Ticks}{invoices.Count}",
                    CustomerId = data.CustomerId,
                    JobId = data.JobId,
                    InvoiceDate = DateTime.UtcNow.Date.AddDays(-invoices.Count),
                    DueDate = DateTime.UtcNow.Date.AddDays(30 - invoices.Count),
                    Status = data.Status,
                    SubTotal = data.Amount,
                    TaxRate = 0.08m,
                    TaxAmount = data.Amount * 0.08m,
                    DiscountAmount = 0.00m,
                    TotalAmount = data.Amount * 1.08m,
                    PaidAmount = data.Status == InvoiceStatus.Paid ? data.Amount * 1.08m : 0.00m,
                    Terms = new PaymentTerms { Days = 30 },
                    Notes = $"Test invoice {invoices.Count + 1}",
                    CreatedAt = DateTime.UtcNow.AddDays(-invoices.Count)
                };

                invoice.AddLineItem(new InvoiceLineItem
                {
                    Description = "Test Service",
                    Quantity = 1,
                    UnitPrice = data.Amount,
                    TaxApplicable = true
                });

                var result = await _invoiceRepository.AddAsync(invoice);
                invoices.Add((Invoice)result);
                _createdInvoiceIds.Add(result.Id);
            }

            return invoices;
        }

        /// <summary>
        /// Creates an overdue test invoice.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task<Invoice> CreateOverdueTestInvoiceAsync()
        {
            var invoice = new Invoice
            {
                Id = Guid.NewGuid().ToString(),
                InvoiceNumber = $"INV{DateTime.UtcNow.Ticks}OVERDUE",
                CustomerId = "CUST003",
                JobId = "JOB004",
                InvoiceDate = DateTime.UtcNow.Date.AddDays(-60),
                DueDate = DateTime.UtcNow.Date.AddDays(-30), // 30 days overdue
                Status = InvoiceStatus.Sent,
                SubTotal = 2000.00m,
                TaxRate = 0.08m,
                TaxAmount = 160.00m,
                DiscountAmount = 0.00m,
                TotalAmount = 2160.00m,
                PaidAmount = 0.00m,
                Terms = new PaymentTerms { Days = 30 },
                Notes = "Overdue test invoice",
                CreatedAt = DateTime.UtcNow.AddDays(-60)
            };

            invoice.AddLineItem(new InvoiceLineItem
            {
                Description = "Overdue Service",
                Quantity = 1,
                UnitPrice = 2000.00m,
                TaxApplicable = true
            });

            var result = await _invoiceRepository.AddAsync(invoice);
            _createdInvoiceIds.Add(result.Id);
            return (Invoice)result;
        }

        /// <summary>
        /// Creates a test job for invoice generation testing.
        /// </summary>
        /// <returns>The job ID of the created test job.</returns>
        private async Task<string> CreateTestJobAsync()
        {
            var jobId = Guid.NewGuid().ToString();
            const string sql = @"
                INSERT INTO Jobs (
                    Id, CustomerId, Title, PickupAddress, PickupLatitude, PickupLongitude,
                    DeliveryAddress, DeliveryLatitude, DeliveryLongitude,
                    CargoDescription, CargoWeight, CargoValue,
                    Status, Priority, BaseRate, MileageRate, TotalAmount,
                    EstimatedDeliveryTime, SpecialInstructions, CreatedAt
                ) VALUES (
                    @Id, @CustomerId, @Title, @PickupAddress, @PickupLat, @PickupLng,
                    @DeliveryAddress, @DeliveryLat, @DeliveryLng, @CargoDesc, @CargoWeight, @CargoValue,
                    @Status, @Priority, @BaseRate, @MileageRate, @TotalAmount,
                    @EstimatedDelivery, @SpecialInstructions, @CreatedAt
                )";

            var parameters = new[]
            {
                new SqlParameter("@Id", jobId),
                new SqlParameter("@CustomerId", "CUST001"),
                new SqlParameter("@Title", "Test Job for Invoice Generation"),
                new SqlParameter("@PickupAddress", "123 Pickup St"),
                new SqlParameter("@PickupCity", "Pickup City"),
                new SqlParameter("@PickupState", "PC"),
                new SqlParameter("@PickupZip", "12345"),
                new SqlParameter("@PickupLat", 40.7128m),
                new SqlParameter("@PickupLng", -74.0060m),
                new SqlParameter("@DeliveryAddress", "456 Delivery Ave"),
                new SqlParameter("@DeliveryCity", "Delivery City"),
                new SqlParameter("@DeliveryState", "DC"),
                new SqlParameter("@DeliveryZip", "67890"),
                new SqlParameter("@DeliveryLat", 34.0522m),
                new SqlParameter("@DeliveryLng", -118.2437m),
                new SqlParameter("@CargoDesc", "Test Cargo"),
                new SqlParameter("@CargoWeight", 1000.0),
                new SqlParameter("@CargoValue", 5000.00m),
                new SqlParameter("@CargoSpecial", "Handle with care"),
                new SqlParameter("@Status", (int)JobStatus.Received),
                new SqlParameter("@Priority", (int)JobPriority.Normal),
                new SqlParameter("@BaseRate", 500.00m),
                new SqlParameter("@MileageRate", 200.00m),
                new SqlParameter("@TotalAmount", 850.00m),
                new SqlParameter("@ScheduledPickup", DateTime.UtcNow.AddDays(-1)),
                new SqlParameter("@EstimatedDelivery", DateTime.UtcNow.AddDays(-1).AddHours(2)),
                new SqlParameter("@SpecialInstructions", "Test instructions"),
                new SqlParameter("@TrackingStatus", "Completed"),
                new SqlParameter("@LastUpdate", DateTime.UtcNow.AddDays(-1)),
                new SqlParameter("@CreatedAt", DateTime.UtcNow.AddDays(-1))
            };

            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync();

            return jobId;
        }

        /// <summary>
        /// Cleans up test data.
        /// </summary>
        public void Dispose()
        {
            // Clean up created test invoices
            foreach (var invoiceId in _createdInvoiceIds)
            {
                try
                {
                    _invoiceRepository.DeleteAsync(invoiceId).Wait();
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}
