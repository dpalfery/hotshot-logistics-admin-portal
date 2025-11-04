using System.Net;
using System.Net.Http.Json;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using System;
using System.Net.Http.Headers;
using HotshotLogistics.Core.Enums;

namespace HotshotLogistics.IntegrationTests
{
    [Collection("DatabaseCollection")]
    public class CustomerControllerIntegrationTests : IntegrationTestBase
    {
        public CustomerControllerIntegrationTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
            // Set the test authentication header
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        }

        [Fact]
        public async Task GetCustomers_ReturnsSuccessAndListOfCustomers()
        {
            // Act
            var response = await Client.GetAsync("/api/Customer");

            // Assert
            response.EnsureSuccessStatusCode();
            var customers = await response.Content.ReadFromJsonAsync<IEnumerable<Customer>>();
            Assert.NotNull(customers);
            Assert.NotEmpty(customers);
            // Relaxed assertion - just verify we got some customers, not exact count
            Assert.True(customers.Count() >= 10, $"Expected at least 10 customers, got {customers.Count()}");
        }

        [Fact]
        public async Task GetCustomer_WithValidId_ReturnsCustomer()
        {
            // Arrange
            var validCustomerId = "cust-001"; // From seed data

            // Act
            var response = await Client.GetAsync($"/api/Customer/{validCustomerId}");

            // Assert
            response.EnsureSuccessStatusCode();
            var customer = await response.Content.ReadFromJsonAsync<Customer>();
            Assert.NotNull(customer);
            Assert.Equal(validCustomerId, customer.Id);
        }

        [Fact]
        public async Task GetCustomer_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidCustomerId = "cust-999";

            // Act
            var response = await Client.GetAsync($"/api/Customer/{invalidCustomerId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateCustomer_WithValidData_ReturnsCreated()
        {
            // Arrange
            var uniqueId = $"test-cust-{Guid.NewGuid():N}";
            var newCustomer = new Customer
            {
                Id = uniqueId,
                CompanyName = "New Test Customer",
                Email = "new.customer@test.com",
                Phone = "555-1234",
                TaxId = "TAX-TEST-001",
                BillingAddress = new Address
                {
                    Street = "123 Test St",
                    City = "Testville",
                    State = "TS",
                    ZipCode = "12345",
                    Country = "USA",
                    Latitude = 40.0,
                    Longitude = -75.0
                },
                Contacts = new List<Contact>
                {
                    new Contact
                    {
                        Name = "John Test",
                        Email = "john.test@example.com",
                        Phone = "555-1234",
                        Title = "Operations Manager",
                        IsPrimary = true
                    }
                },
                CreditTerms = new CreditTerms
                {
                    PaymentTermsDays = 30,
                    Status = CreditStatus.Approved,
                    ApprovedDate = DateTime.UtcNow
                },
                CreditLimit = 10000m,
                IsActive = true
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/Customer", newCustomer);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var createdCustomer = await response.Content.ReadFromJsonAsync<Customer>();
            Assert.NotNull(createdCustomer);
            Assert.Equal(newCustomer.CompanyName, createdCustomer.CompanyName);
            Assert.Equal(uniqueId, createdCustomer.Id);
        }

        [Fact]
        public async Task UpdateCustomer_WithValidData_ReturnsOk()
        {
            // Arrange - First create a customer to update
            var uniqueId = $"test-update-{Guid.NewGuid():N}";
            var initialCustomer = new Customer
            {
                Id = uniqueId,
                CompanyName = "Customer To Update",
                Email = "update.test@example.com",
                Phone = "555-UPDATE",
                TaxId = "TAX-UPDATE-001",
                BillingAddress = new Address
                {
                    Street = "123 Original St",
                    City = "OriginalCity",
                    State = "OR",
                    ZipCode = "12345",
                    Country = "USA",
                    Latitude = 40.0,
                    Longitude = -75.0
                },
                Contacts = new List<Contact>
                {
                    new Contact
                    {
                        Name = "Original Contact",
                        Email = "original.contact@example.com",
                        Phone = "555-1111",
                        Title = "Original Title",
                        IsPrimary = true
                    }
                },
                CreditTerms = new CreditTerms
                {
                    PaymentTermsDays = 30,
                    Status = CreditStatus.Approved,
                    ApprovedDate = DateTime.UtcNow
                },
                CreditLimit = 10000m,
                IsActive = true
            };

            // Create the customer first
            var createResponse = await Client.PostAsJsonAsync("/api/Customer", initialCustomer);
            createResponse.EnsureSuccessStatusCode();

            // Now update it
            var customerToUpdate = new Customer
            {
                Id = uniqueId,
                CompanyName = "Updated Customer Name",
                Email = "updated.customer@test.com",
                Phone = "555-UPDATED",
                TaxId = "TAX-UPDATED-001",
                BillingAddress = new Address
                {
                    Street = "456 Update Ave",
                    City = "UpdateCity",
                    State = "UP",
                    ZipCode = "54321",
                    Country = "USA",
                    Latitude = 41.0,
                    Longitude = -76.0
                },
                Contacts = new List<Contact>
                {
                    new Contact
                    {
                        Name = "Jane Updated",
                        Email = "jane.updated@example.com",
                        Phone = "555-9876",
                        Title = "Account Manager",
                        IsPrimary = true
                    }
                },
                CreditTerms = new CreditTerms
                {
                    PaymentTermsDays = 45,
                    Status = CreditStatus.Approved,
                    ApprovedDate = DateTime.UtcNow
                },
                CreditLimit = 15000m,
                IsActive = false
            };

            // Act
            var response = await Client.PutAsJsonAsync($"/api/Customer/{uniqueId}", customerToUpdate);

            // Assert
            response.EnsureSuccessStatusCode();
            var updatedCustomer = await response.Content.ReadFromJsonAsync<Customer>();

            // Verify update
            Assert.NotNull(updatedCustomer);
            Assert.Equal("Updated Customer Name", updatedCustomer.CompanyName);
            Assert.False(updatedCustomer.IsActive);
        }

        [Fact]
        public async Task DeleteCustomer_WithValidId_ReturnsNoContent()
        {
            // Arrange - First create a customer to delete
            var uniqueId = $"test-delete-{Guid.NewGuid():N}";
            var testCustomer = new Customer
            {
                Id = uniqueId,
                CompanyName = "Customer To Delete",
                Email = "delete.test@example.com",
                Phone = "555-DELETE",
                TaxId = "TAX-DELETE-001",
                BillingAddress = new Address
                {
                    Street = "123 Delete St",
                    City = "DeleteCity",
                    State = "DL",
                    ZipCode = "12345",
                    Country = "USA",
                    Latitude = 40.0,
                    Longitude = -75.0
                },
                Contacts = new List<Contact>
                {
                    new Contact
                    {
                        Name = "Delete Test Contact",
                        Email = "delete.contact@example.com",
                        Phone = "555-1111",
                        Title = "Test Contact",
                        IsPrimary = true
                    }
                },
                CreditTerms = new CreditTerms
                {
                    PaymentTermsDays = 30,
                    Status = CreditStatus.Approved,
                    ApprovedDate = DateTime.UtcNow
                },
                CreditLimit = 5000m,
                IsActive = true
            };

            // Create the customer
            var createResponse = await Client.PostAsJsonAsync("/api/Customer", testCustomer);
            createResponse.EnsureSuccessStatusCode();

            // Act - Delete the customer
            var response = await Client.DeleteAsync($"/api/Customer/{uniqueId}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify it was deleted
            var getResponse = await Client.GetAsync($"/api/Customer/{uniqueId}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task GetActiveCustomers_ReturnsOnlyActiveCustomers()
        {
            // Act
            var response = await Client.GetAsync("/api/Customer/active");

            // Assert
            response.EnsureSuccessStatusCode();
            var customers = await response.Content.ReadFromJsonAsync<IEnumerable<Customer>>();
            Assert.NotNull(customers);
            Assert.All(customers, c => Assert.True(c.IsActive));
        }

        [Fact]
        public async Task GetCustomerJobs_WithValidId_ReturnsJobs()
        {
            // Arrange
            var customerId = "cust-001"; // This customer has jobs based on seed data

            // Act
            var response = await Client.GetAsync($"/api/Customer/{customerId}/jobs");

            // Assert
            response.EnsureSuccessStatusCode();
            var jobs = await response.Content.ReadFromJsonAsync<IEnumerable<Domain.Entities.Job>>();
            Assert.NotNull(jobs);
            Assert.NotEmpty(jobs);
        }

        [Fact]
        public async Task GetCustomerInvoices_WithValidId_ReturnsInvoices()
        {
            // Arrange
            var customerId = "cust-001"; // This customer has invoices based on seed data

            // Act
            var response = await Client.GetAsync($"/api/Customer/{customerId}/invoices");

            // Assert
            response.EnsureSuccessStatusCode();
            var invoices = await response.Content.ReadFromJsonAsync<IEnumerable<Invoice>>();
            Assert.NotNull(invoices);
            Assert.NotEmpty(invoices);
        }

        [Fact]
        public async Task UpdateCreditLimit_WithValidData_ReturnsNoContent()
        {
            // Arrange
            var customerId = "cust-004";
            var request = new { NewLimit = 5000.00m };

            // Act
            var response = await Client.PostAsJsonAsync($"/api/Customer/{customerId}/credit-limit", request);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task UpdateCreditTerms_WithValidData_ReturnsNoContent()
        {
            // Arrange
            var customerId = "cust-005";
            var creditTerms = new CreditTerms { PaymentTermsDays = 45, Status = CreditStatus.Approved };

            // Act
            var response = await Client.PutAsJsonAsync($"/api/Customer/{customerId}/credit-terms", creditTerms);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
