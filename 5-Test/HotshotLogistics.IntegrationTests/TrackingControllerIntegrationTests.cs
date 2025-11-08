// <copyright file="TrackingControllerIntegrationTests.cs" company="PlaceholderCompany">
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

    /// <summary>
    /// Integration tests for the TrackingController.
    /// </summary>
    [Collection("DatabaseCollection")]
    public class TrackingControllerIntegrationTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrackingControllerIntegrationTests"/> class.
        /// </summary>
        /// <param name="factory">The web application factory.</param>
        public TrackingControllerIntegrationTests(CustomWebApplicationFactory<Program> factory)
            : base(factory)
        {
            // Set the test authentication header
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        }

        /// <summary>
        /// Tests that GetCurrentLocation returns NotFound for a job that is not being tracked.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetCurrentLocation_WhenJobNotTracked_ReturnsNotFound()
        {
            // Arrange
            var jobId = "job-cust-001-001"; // A job that exists but we assume is not tracked yet

            // Act
            var response = await Client.GetAsync($"/api/Tracking/location/{jobId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
