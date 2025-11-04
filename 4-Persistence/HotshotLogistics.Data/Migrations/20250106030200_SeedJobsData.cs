#pragma warning disable SA1649
using FluentMigrator;

namespace HotshotLogistics.Data.Migrations;

/// <summary>
/// Seeds initial jobs data for testing.
/// </summary>
[Migration(20250106030200)]
public class SeedJobsData : Migration
{
    /// <summary>
    /// Applies the migration to seed jobs data.
    /// </summary>
    public override void Up()
    {
        // Seed 200 jobs with various statuses and priorities
        for (int i = 1; i <= 200; i++)
        {
            var jobId = $"job-{i:D3}";
            var customerId = $"cust-{(i % 10 == 0 ? 10 : i % 10):D3}"; // Cycle through first 10 customers

            Insert.IntoTable("Jobs")
                .Row(new
                {
                    Id = jobId,
                    CustomerId = customerId,
                    Title = $"Seed Job {i:D3} - Test Delivery",
                    Description = $"Test job {i} description for integration testing",
                    PickupAddress = $"{100 + i} Pickup St",
                    PickupCity = "TestCity",
                    PickupState = "TS",
                    PickupPostalCode = $"{10000 + i}",
                    PickupLatitude = 40.0m + (i * 0.01m),
                    PickupLongitude = -74.0m + (i * 0.01m),
                    DeliveryAddress = $"{200 + i} Delivery Ave",
                    DeliveryCity = "TestTown",
                    DeliveryState = "TT",
                    DeliveryPostalCode = $"{20000 + i}",
                    DeliveryLatitude = 41.0m + (i * 0.01m),
                    DeliveryLongitude = -75.0m + (i * 0.01m),
                    CargoDescription = $"Test cargo {i}",
                    CargoWeight = 50.0m + i,
                    IsHazardous = i % 5 == 0, // Every 5th job is hazardous
                    Status = (i % 4) + 1, // Cycle through JobStatus values (1-4)
                    Priority = (i % 3) + 1, // Cycle through JobPriority values (1-3)
                    BaseRate = 100.0m + (i * 10),
                    MileageRate = 1.5m + (i * 0.1m),
                    TotalAmount = 150.0m + (i * 15),
                    ScheduledPickupTime = DateTime.UtcNow.AddDays(i),
                    EstimatedDeliveryTime = DateTime.UtcNow.AddDays(i + 1),
                    SpecialInstructions = i % 3 == 0 ? $"Special handling required for job {i}" : null,
                    CreatedAt = DateTime.UtcNow.AddDays(-i),
                    UpdatedAt = (DateTime?)null,
                });
        }
    }

    /// <summary>
    /// Reverts the migration by removing seeded jobs data.
    /// </summary>
    public override void Down()
    {
        Delete.FromTable("Jobs").AllRows();
    }
}
