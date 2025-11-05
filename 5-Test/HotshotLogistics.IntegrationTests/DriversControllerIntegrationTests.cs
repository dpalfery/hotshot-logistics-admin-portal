using System.Net;
using System.Net.Http.Json;
using HotshotLogistics.Domain.Entities;
using System.Net.Http.Headers;
using FluentAssertions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using System;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Api;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HotshotLogistics.IntegrationTests
{
    // <copyright file="DriversControllerIntegrationTests.cs" company="PlaceholderCompany">
    // Copyright (c) PlaceholderCompany. All rights reserved.
    // </copyright>

    [Collection("DatabaseCollection")]
    public class DriversControllerIntegrationTests : IntegrationTestBase, IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient client;
        private readonly CustomWebApplicationFactory<Program> factory;

        public DriversControllerIntegrationTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
            // Set the test authentication header
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

            this.factory = factory;
            this.client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
        }

        [Fact]
        public async Task GetDrivers_ReturnsSuccessAndListOfDrivers()
        {
            // Act
            var response = await Client.GetAsync("/api/Drivers");

            // Assert
            response.EnsureSuccessStatusCode();
            var drivers = await response.Content.ReadFromJsonAsync<List<DriverDto>>();
            drivers.Should().NotBeNull();
            drivers.Should().HaveCountGreaterThan(100); // Should have plenty of seed drivers
        }

        [Fact]
        public async Task GetDriver_WithValidId_ReturnsDriver()
        {
            // Arrange
            // The seed data is deterministic, so we can rely on the first driver's ID.
            var driverId = 1010; // Updated to match actual seed data starting ID

            // Act
            var response = await Client.GetAsync($"/api/Drivers/{driverId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var driver = await response.Content.ReadFromJsonAsync<DriverDto>();
            driver.Should().NotBeNull();
            driver.Id.Should().Be(driverId);
            driver.Email.Should().Be("seed.driver001@local.test");
        }

        [Fact]
        public async Task GetDriver_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidDriverId = 9999;

            // Act
            var response = await Client.GetAsync($"/api/Drivers/{invalidDriverId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CreateDriver_WithValidData_ReturnsCreated()
        {
            // Arrange
            var uniqueEmail = $"test.driver.{Guid.NewGuid():N}@test.com";
            var newDriver = new DriverDto
            {
                FirstName = "Test",
                LastName = "Driver",
                Email = uniqueEmail,
                PhoneNumber = "(555) 123-4567", // Valid US phone format
                LicenseNumber = "DRV123456", // Valid format: uppercase letters, numbers, hyphens
                LicenseExpiryDate = System.DateTime.UtcNow.AddYears(3), // Must be valid for at least 2 years (for age validation) and 6 months (for registration)
                IsActive = true
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/Drivers", newDriver);

            // Debug: Check actual error response if not successful
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Driver creation failed with status {response.StatusCode}: {errorContent}");
            }

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdDriver = await response.Content.ReadFromJsonAsync<DriverDto>();
            createdDriver.Should().NotBeNull();
            createdDriver.Email.Should().Be(uniqueEmail);
            createdDriver.Id.Should().BeGreaterThan(1210); // Should be after the seeded drivers (1010-1210)
        }

        [Fact]
        public async Task UpdateDriver_WithValidData_ReturnsOk()
        {
            // Arrange
            var driverIdToUpdate = 1011; // from seed data - second driver
            var uniqueUpdateEmail = $"updated.driver.{Guid.NewGuid():N}@test.com";
            var driverToUpdate = new DriverDto
            {
                Id = driverIdToUpdate,
                FirstName = "Updated",
                LastName = "DriverTwo",
                Email = uniqueUpdateEmail,
                PhoneNumber = "(*************", // Valid US phone format
                LicenseNumber = "DRV654321", // Valid format: uppercase letters, numbers, hyphens
                LicenseExpiryDate = System.DateTime.UtcNow.AddYears(3), // Must meet validation requirements
                IsActive = false,
            };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/Drivers/{driverIdToUpdate}", driverToUpdate);

        // Assert
        response.EnsureSuccessStatusCode();
            var updatedDriver = await response.Content.ReadFromJsonAsync<DriverDto>();

            updatedDriver.Should().NotBeNull();
            updatedDriver.FirstName.Should().Be("Updated");
            updatedDriver.IsActive.Should().BeFalse();
            updatedDriver.Email.Should().Be(uniqueUpdateEmail);
        }

        [Fact]
        public async Task DeleteDriver_WithValidId_ReturnsNoContent()
        {
            // Arrange - First create a driver to delete
            var uniqueDeleteEmail = $"delete.test.driver.{Guid.NewGuid():N}@example.com";
            var testDriver = new DriverDto
            {
                FirstName = "Delete",
                LastName = "TestDriver",
                Email = uniqueDeleteEmail,
                PhoneNumber = "(555) 123-9999", // Valid US phone format
                LicenseNumber = "DRV-DELETE", // Valid format: uppercase letters, numbers, hyphens
                LicenseExpiryDate = System.DateTime.UtcNow.AddYears(3), // Must meet validation requirements
                IsActive = true
            };

            // Create the driver
            var createResponse = await Client.PostAsJsonAsync("/api/Drivers", testDriver);
            createResponse.EnsureSuccessStatusCode();
            var createdDriver = await createResponse.Content.ReadFromJsonAsync<DriverDto>();
            createdDriver.Should().NotBeNull();
            var driverIdToDelete = createdDriver.Id;

            // Act - Delete the driver
            var response = await Client.DeleteAsync($"/api/Drivers/{driverIdToDelete}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify it was soft-deleted (should still exist but IsActive = false)
            var getResponse = await Client.GetAsync($"/api/Drivers/{driverIdToDelete}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var deletedDriver = await getResponse.Content.ReadFromJsonAsync<DriverDto>();
            deletedDriver.Should().NotBeNull();
            deletedDriver.IsActive.Should().BeFalse(); // Should be soft-deleted (inactive)
        }
    }
}
