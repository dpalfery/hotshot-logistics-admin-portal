// <copyright file="JobAssignmentsControllerIntegrationTests.cs" company="PlaceholderCompany">
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
    /// Integration tests for the JobAssignmentsController.
    /// </summary>
    [Collection("DatabaseCollection")]
    public class JobAssignmentsControllerIntegrationTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobAssignmentsControllerIntegrationTests"/> class.
        /// </summary>
        /// <param name="factory">The web application factory.</param>
        public JobAssignmentsControllerIntegrationTests(CustomWebApplicationFactory<Program> factory)
            : base(factory)
        {
            // Set the test authentication header
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        }

        /// <summary>
        /// Tests that GetJobAssignmentById returns a specific job assignment when it exists.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetJobAssignmentById_WhenAssignmentExists_ReturnsOkWithAssignment()
        {
            // Arrange
            // This job assignment ID is known to exist from the SeedLargeTestData migration
            var assignmentId = "ja-job-cust-001-001";

            // Act
            var response = await Client.GetAsync($"/api/JobAssignments/{assignmentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var assignment = JsonSerializer.Deserialize<JobAssignment>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            assignment.Should().NotBeNull();
            assignment.Id.Should().Be(assignmentId);
        }

        /// <summary>
        /// Tests that GetJobAssignmentById returns NotFound for a non-existent job assignment.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetJobAssignmentById_WhenAssignmentDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var assignmentId = "assignment-that-does-not-exist";

            // Act
            var response = await Client.GetAsync($"/api/JobAssignments/{assignmentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
