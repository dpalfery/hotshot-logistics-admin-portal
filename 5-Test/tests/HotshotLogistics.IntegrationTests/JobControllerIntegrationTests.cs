using System.Net;
using System.Net.Http.Json;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;
using HotshotLogistics.Core.Enums;
using System.Net.Http.Headers;
using HotshotLogistics.Contracts;
using HotshotLogistics.Domain.DTOs;

namespace HotshotLogistics.IntegrationTests
{
    /// <summary>
    /// Integration tests for the JobController.
    /// </summary>
    [Collection("DatabaseCollection")]
    public class JobControllerIntegrationTests : IntegrationTestBase
    {
        public JobControllerIntegrationTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
            // Set the test authentication header
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        }

        [Fact]
        public async Task GetJobs_ReturnsSuccessAndListOfJobs()
        {
            // Act
            var response = await Client.GetAsync("/api/Job");

            // Assert
            response.EnsureSuccessStatusCode();
            var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<Domain.Entities.Job>>();
            Assert.NotNull(pagedResult);
            Assert.NotNull(pagedResult.Items);
            Assert.NotEmpty(pagedResult.Items);
        }

        [Fact]
        public async Task GetJobs_WithPaging_ReturnsCorrectPage()
        {
            // Act
            var response = await Client.GetAsync("/api/Job?pageNumber=1&pageSize=10");

            // Assert
            response.EnsureSuccessStatusCode();
            var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<Domain.Entities.Job>>();
            Assert.NotNull(pagedResult);
            Assert.NotNull(pagedResult.Items);
            Assert.True(pagedResult.Items.Count() <= 10);
            Assert.Equal(1, pagedResult.PageNumber);
            Assert.Equal(10, pagedResult.PageSize);
        }


        [Fact]
        public async Task GetJob_WithValidId_ReturnsJob()
        {
            // Arrange
            var jobsResponse = await Client.GetAsync("/api/Job");
            var pagedResult = await jobsResponse.Content.ReadFromJsonAsync<PagedResult<Domain.Entities.Job>>();
            var validJobId = pagedResult.Items.First().Id;

            // Act
            var response = await Client.GetAsync($"/api/Job/{validJobId}");

            // Assert
            response.EnsureSuccessStatusCode();
            var job = await response.Content.ReadFromJsonAsync<Domain.Entities.Job>();
            Assert.NotNull(job);
            Assert.Equal(validJobId, job.Id);
        }

        [Fact]
        public async Task GetJob_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidJobId = Guid.NewGuid().ToString();

            // Act
            var response = await Client.GetAsync($"/api/Job/{invalidJobId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateJob_WithValidData_ReturnsCreated()
        {
            // Arrange
            var uniqueId = $"test-job-{Guid.NewGuid():N}";
            var baseTime = DateTime.UtcNow.AddMinutes(10); // Start 10 minutes from now to avoid timing issues
            var pickupTime = new DateTime(baseTime.Year, baseTime.Month, baseTime.Day, baseTime.Hour, baseTime.Minute, 0, DateTimeKind.Utc);
            var deliveryTime = pickupTime.AddHours(4); // Ensure 4-hour gap between pickup and delivery

            // Use Job with all required fields properly set
            var newJob = new Job
            {
                Id = uniqueId,
                CustomerId = "cust-001", // Seeded customer
                Title = "Urgent Tech Parts Delivery",
                PickupLocation = new Location { Address = "100 Tech Park", City = "Innovate", State = "CA", PostalCode = "94043", Country = "USA" },
                DeliveryLocation = new Location { Address = "200 Consumer Ave", City = "Market", State = "CA", PostalCode = "94041", Country = "USA" },
                Amount = 300m, // Required field
                Cargo = new CargoDetails { Description = "Sensitive electronics", Weight = 50, IsHazardous = false, Quantity = 1, Value = 1000 },
                Status = JobStatus.Pending,
                Priority = JobPriority.High,
                Pricing = new PricingDetails { BaseRate = 250, MileageRate = 1.75m, TotalAmount = 300, Currency = "USD" },
                ScheduledPickupTime = pickupTime,
                EstimatedDeliveryTime = deliveryTime,
                SpecialInstructions = "Handle with extreme care."
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/Job", newJob);

            // Debug: Check actual error response
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var statusCode = response.StatusCode;
                var reasonPhrase = response.ReasonPhrase;
                
                // Log detailed error information
                Console.WriteLine($"Status Code: {statusCode}");
                Console.WriteLine($"Reason Phrase: {reasonPhrase}");
                Console.WriteLine($"Error Content: {errorContent}");
                
                // Try to get more details from headers
                foreach (var header in response.Headers)
                {
                    Console.WriteLine($"Header {header.Key}: {string.Join(", ", header.Value)}");
                }
                foreach (var contentHeader in response.Content.Headers)
                {
                    Console.WriteLine($"Content Header {contentHeader.Key}: {string.Join(", ", contentHeader.Value)}");
                }
                
                throw new Exception($"Job create failed with status {response.StatusCode}: {errorContent}");
            }

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode); // Should be Created for successful creation
            var createdJob = await response.Content.ReadFromJsonAsync<Job>();
            Assert.NotNull(createdJob);
            Assert.Equal(newJob.Title, createdJob.Title);
            Assert.Equal(uniqueId, createdJob.Id);
        }

        [Fact]
        public async Task UpdateJob_WithValidData_ReturnsNoContent()
        {
            // Arrange
            var jobsResponse = await Client.GetAsync("/api/Job");
            var pagedResult = await jobsResponse.Content.ReadFromJsonAsync<PagedResult<Job>>();
            var jobToUpdate = pagedResult.Items.Skip(1).First(); // Get second job

            // Create a simple Job with minimal valid data for update
            var pickupTime = DateTime.UtcNow.AddHours(2);
            var jobDto = new Job
            {
                Id = jobToUpdate.Id,
                CustomerId = jobToUpdate.CustomerId,
                Title = "Updated Super Urgent Delivery", // Updated title
                PickupLocation = new Location { Address = "Updated 100 Tech Park", City = "Innovate", State = "CA", PostalCode = "94043", Country = "USA" },
                DeliveryLocation = new Location { Address = "Updated 200 Consumer Ave", City = "Market", State = "CA", PostalCode = "94041", Country = "USA" },
                Amount = 350m, // Updated amount
                Status = JobStatus.Assigned, // Updated status
                Priority = JobPriority.High,
                ScheduledPickupTime = pickupTime,
                // Use simple, reliable nested objects
                Cargo = new CargoDetails { Description = "Updated sensitive electronics", Weight = 60, IsHazardous = false, Quantity = 1, Value = 1200 },
                Pricing = new PricingDetails { BaseRate = 300, MileageRate = 2.0m, TotalAmount = 350, Currency = "USD" },
                SpecialInstructions = "Updated special handling instructions"
            };

            // Set EstimatedDeliveryTime to be 4 hours after pickup
            jobDto.EstimatedDeliveryTime = pickupTime.AddHours(4);

            // Act
            var response = await Client.PutAsJsonAsync($"/api/Job/{jobToUpdate.Id}", jobDto);

            // Debug: Check actual error response
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Job update failed with status {response.StatusCode}: {errorContent}");
            }

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode); // Changed from NoContent to OK since API returns the updated job

            // Verify update
            var getResponse = await Client.GetAsync($"/api/Job/{jobToUpdate.Id}");
            getResponse.EnsureSuccessStatusCode();
            var updatedJob = await getResponse.Content.ReadFromJsonAsync<Job>();
            Assert.Equal("Updated Super Urgent Delivery", updatedJob.Title);
            Assert.Equal(JobStatus.Assigned, updatedJob.Status);
        }

        [Fact]
        public async Task DeleteJob_WithValidId_ReturnsNoContent()
        {
            // Arrange - First create a job to delete using Job
            var uniqueId = $"delete-job-{Guid.NewGuid():N}";
            var pickupTime = DateTime.UtcNow.AddHours(2);
            var testJob = new Job
            {
                Id = uniqueId,
                CustomerId = "cust-001", // Seeded customer
                Title = "Job To Delete",
                PickupLocation = new Location { Address = "123 Delete St", City = "DeleteCity", State = "DL", PostalCode = "12345", Country = "USA" },
                DeliveryLocation = new Location { Address = "456 Destination Ave", City = "DestCity", State = "DS", PostalCode = "54321", Country = "USA" },
                Amount = 150m, // Required field
                Cargo = new CargoDetails { Description = "Test cargo for deletion", Weight = 25, IsHazardous = false, Quantity = 1, Value = 100 },
                Status = JobStatus.Pending,
                Priority = JobPriority.Medium,
                Pricing = new PricingDetails { BaseRate = 100, MileageRate = 1.5m, TotalAmount = 150, Currency = "USD" },
                ScheduledPickupTime = pickupTime,
                EstimatedDeliveryTime = pickupTime.AddHours(8), // Set EstimatedDeliveryTime 8 hours after pickup
                SpecialInstructions = "Job created for delete test"
            };

            // Create the job
            var createResponse = await Client.PostAsJsonAsync("/api/Job", testJob);

            // Debug: Check creation error
            if (!createResponse.IsSuccessStatusCode)
            {
                var createErrorContent = await createResponse.Content.ReadAsStringAsync();
                throw new Exception($"Job creation for delete test failed with status {createResponse.StatusCode}: {createErrorContent}");
            }
            createResponse.EnsureSuccessStatusCode();
            var createdJob = await createResponse.Content.ReadFromJsonAsync<Job>();
            Assert.NotNull(createdJob);
            var jobIdToDelete = createdJob.Id;

            // Act - Delete the job
            var response = await Client.DeleteAsync($"/api/Job/{jobIdToDelete}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify it was deleted
            var getResponse = await Client.GetAsync($"/api/Job/{jobIdToDelete}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }
    }
}
