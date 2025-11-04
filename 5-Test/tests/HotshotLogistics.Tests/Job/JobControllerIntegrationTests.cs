// <copyright file="JobControllerIntegrationTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Tests.Job
{
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using System.Threading.Tasks;
    using HotshotLogistics.Api;
using HotshotLogistics.Domain.Entities;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Xunit;
    using FluentAssertions;
    using HotshotLogistics.Domain.Entities;

    /// <summary>
    /// Integration tests for the JobController.
    /// </summary>
    [Collection("DatabaseCollection")]
    public class JobControllerIntegrationTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobControllerIntegrationTests"/> class.
        /// </summary>
        /// <param name="factory">The web application factory.</param>
        public JobControllerIntegrationTests(WebApplicationFactory<Program> factory)
            : base(factory)
        {
            // Set up test authentication
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        }

        /// <summary>
        /// Tests that GetJobById returns a specific job when it exists.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetJobById_WhenJobExists_ReturnsOkWithJob()
        {
            // Arrange
            // This job ID is known to exist from the SeedLargeTestData migration
            var jobId = "job-cust-001-001";

            // Act
            var response = await Client.GetAsync($"/api/Job/{jobId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var job = JsonSerializer.Deserialize<Domain.Entities.Job>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            job.Should().NotBeNull();
            job.Id.Should().Be(jobId);
            job.CustomerId.Should().Be("cust-001");
        }

        /// <summary>
        /// Tests that GetJobById returns NotFound for a non-existent job.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetJobById_WhenJobDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var jobId = "job-that-does-not-exist";

            // Act
            var response = await Client.GetAsync($"/api/Job/{jobId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
