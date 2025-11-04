// <copyright file="BillingControllerIntegrationTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.IntegrationTests
{
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using HotshotLogistics.Api;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Xunit;
    using FluentAssertions;
    using HotshotLogistics.Domain.Entities;
    using System.Text.Json;
using HotshotLogistics.Domain.Entities;

    /// <summary>
    /// Integration tests for the BillingController.
    /// </summary>
    [Collection("DatabaseCollection")]
    public class BillingControllerIntegrationTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BillingControllerIntegrationTests"/> class.
        /// </summary>
        /// <param name="factory">The web application factory.</param>
        public BillingControllerIntegrationTests(CustomWebApplicationFactory<Program> factory)
            : base(factory)
        {
            // Set up test authentication
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        }

        /// <summary>
        /// Tests that GetInvoiceById returns a specific invoice when it exists.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetInvoiceById_WhenInvoiceExists_ReturnsOkWithInvoice()
        {
            // Arrange
            // This invoice ID is known to exist from the SeedLargeTestData migration
            var invoiceId = "inv-job-cust-001-001";

            // Act
            var response = await Client.GetAsync($"/api/Billing/invoices/{invoiceId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var invoice = JsonSerializer.Deserialize<Invoice>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            invoice.Should().NotBeNull();
            invoice.Id.Should().Be(invoiceId);
        }

        /// <summary>
        /// Tests that GetInvoiceById returns NotFound for a non-existent invoice.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetInvoiceById_WhenInvoiceDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var invoiceId = "invoice-that-does-not-exist";

            // Act
            var response = await Client.GetAsync($"/api/Billing/invoices/{invoiceId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
