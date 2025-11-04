#pragma warning disable SA1649
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using FluentMigrator;

namespace HotshotLogistics.Data.Migrations;

[Migration(20251004120000)]
public class SeedLargeTestData : Migration
{
    public override void Up()
    {
        // This migration seeds 100 customers, a pool of drivers (200),
        // 10-50 jobs per customer, job assignments, invoices and invoice line items.
        // Intended for local/dev use only. Task orchestrator: create and run large local seed migration.
        // Use deterministic randomness (fixed seed) to keep results repeatable.
        Execute.WithConnection((connection, transaction) =>
        {
            var rnd = new Random(12345); // deterministic randomness

            // Insert customers cust-001 .. cust-100 idempotently
            for (int i = 1; i <= 100; i++)
            {
                string custId = $"cust-{i:000}";
                using var checkCmd = connection.CreateCommand();
                checkCmd.Transaction = transaction;
                checkCmd.CommandText = "SELECT COUNT(1) FROM Customers WHERE Id = @Id";
                checkCmd.Parameters.Add(CreateParam(checkCmd, "@Id", custId));
                var exists = Convert.ToInt32(checkCmd.ExecuteScalar() ?? 0) > 0;
                if (exists)
                {
                    continue;
                }

                using var insertCmd = connection.CreateCommand();
                insertCmd.Transaction = transaction;
                insertCmd.CommandText = @"
INSERT INTO Customers (Id, CompanyName, TaxId, Email, Phone, BillingAddress, City, State, ZipCode, Country, Latitude, Longitude, CreditLimit, PaymentTermsDays, CreditStatus, IsActive, CreatedAt)
VALUES (@Id, @CompanyName, @TaxId, @Email, @Phone, @BillingAddress, @City, @State, @ZipCode, @Country, @Latitude, @Longitude, @CreditLimit, @PaymentTermsDays, @CreditStatus, @IsActive, @CreatedAt)";

                insertCmd.Parameters.Add(CreateParam(insertCmd, "@Id", custId));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@CompanyName", $"Seed Customer {i:000}"));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@TaxId", $"TAX-{i:000}"));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@Email", $"customer{i:000}@seedtest.com"));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@Phone", $"555-{2000 + i:0000}"));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@BillingAddress", $"{i} Seed St"));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@City", "Testville"));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@State", "TS"));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@ZipCode", $"{10000 + i}"));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@Country", "USA"));
                // simple lat/long variation
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@Latitude", 40.0m + ((decimal)(i % 10) * 0.01m)));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@Longitude", -75.0m - ((decimal)(i % 10) * 0.01m)));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@CreditLimit", 5000m + (i * 10)));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@PaymentTermsDays", 30));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@CreditStatus", 1)); // Approved
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@IsActive", true));
                insertCmd.Parameters.Add(CreateParam(insertCmd, "@CreatedAt", DateTime.UtcNow));

                insertCmd.ExecuteNonQuery();
            }

            // Insert driver pool (200 drivers) idempotently by unique email
            for (int d = 1; d <= 200; d++)
            {
                string driverEmail = $"seed.driver{d:000}@local.test";
                using var checkDriver = connection.CreateCommand();
                checkDriver.Transaction = transaction;
                checkDriver.CommandText = "SELECT COUNT(1) FROM Drivers WHERE Email = @Email";
                checkDriver.Parameters.Add(CreateParam(checkDriver, "@Email", driverEmail));
                var existsDriver = Convert.ToInt32(checkDriver.ExecuteScalar() ?? 0) > 0;
                if (existsDriver)
                {
                    continue;
                }

                using var insertDriver = connection.CreateCommand();
                insertDriver.Transaction = transaction;
                insertDriver.CommandText = @"
INSERT INTO Drivers (FirstName, LastName, Email, PhoneNumber, LicenseNumber, LicenseState, LicenseExpiryDate, InsuranceExpiryDate, CurrentStatus, IsActive, CreatedAt)
VALUES (@FirstName, @LastName, @Email, @PhoneNumber, @LicenseNumber, @LicenseState, @LicenseExpiryDate, @InsuranceExpiryDate, @CurrentStatus, @IsActive, @CreatedAt)";

                insertDriver.Parameters.Add(CreateParam(insertDriver, "@FirstName", $"Driver{d:000}"));
                insertDriver.Parameters.Add(CreateParam(insertDriver, "@LastName", "Seed"));
                insertDriver.Parameters.Add(CreateParam(insertDriver, "@Email", driverEmail));
                insertDriver.Parameters.Add(CreateParam(insertDriver, "@PhoneNumber", $"555-{1000 + d:0000}"));
                insertDriver.Parameters.Add(CreateParam(insertDriver, "@LicenseNumber", $"LD-{d:000000}"));
                insertDriver.Parameters.Add(CreateParam(insertDriver, "@LicenseState", "TS"));
                insertDriver.Parameters.Add(CreateParam(insertDriver, "@LicenseExpiryDate", DateTime.UtcNow.AddYears(2).Date));
                insertDriver.Parameters.Add(CreateParam(insertDriver, "@InsuranceExpiryDate", DateTime.UtcNow.AddYears(1).Date));
                insertDriver.Parameters.Add(CreateParam(insertDriver, "@CurrentStatus", 0));
                insertDriver.Parameters.Add(CreateParam(insertDriver, "@IsActive", true));
                insertDriver.Parameters.Add(CreateParam(insertDriver, "@CreatedAt", DateTime.UtcNow));

                insertDriver.ExecuteNonQuery();
            }

            // Retrieve driver IDs into list for assignments
            var driverIds = new List<int>();
            using (var readDrivers = connection.CreateCommand())
            {
                readDrivers.Transaction = transaction;
                readDrivers.CommandText = "SELECT Id FROM Drivers WHERE Email LIKE 'seed.driver%'";
                using var reader = readDrivers.ExecuteReader();
                while (reader.Read())
                {
                    driverIds.Add(reader.GetInt32(0));
                }
            }

            if (driverIds.Count == 0)
            {
                throw new Exception("No seed drivers available after insert; aborting migration.");
            }

            // For each customer create between 10 and 50 jobs
            for (int i = 1; i <= 100; i++)
            {
                string custId = $"cust-{i:000}";
                int jobsForCustomer = rnd.Next(10, 51);

                for (int j = 1; j <= jobsForCustomer; j++)
                {
                    string jobId = $"job-{custId}-{j:000}";
                    // Check job exists
                    using var checkJob = connection.CreateCommand();
                    checkJob.Transaction = transaction;
                    checkJob.CommandText = "SELECT COUNT(1) FROM Jobs WHERE Id = @Id";
                    checkJob.Parameters.Add(CreateParam(checkJob, "@Id", jobId));
                    var jobExists = Convert.ToInt32(checkJob.ExecuteScalar() ?? 0) > 0;
                    if (jobExists)
                    {
                        continue;
                    }

                    decimal baseRate = 100m + (decimal)(rnd.NextDouble() * 400.0);
                    decimal totalAmount = Math.Round(baseRate + (decimal)(rnd.NextDouble() * 200.0), 2);
                    DateTime estimated = DateTime.UtcNow.AddHours(rnd.Next(2, 72));

                    // Choose a random driver
                    int assignedDriverId = driverIds[rnd.Next(driverIds.Count)];

                    using var insertJob = connection.CreateCommand();
                    insertJob.Transaction = transaction;
                    insertJob.CommandText = @"
INSERT INTO Jobs (Id, CustomerId, Title, PickupAddress, DeliveryAddress, Status, Priority, BaseRate, TotalAmount, ScheduledPickupTime, EstimatedDeliveryTime, AssignedDriverId, CreatedAt)
VALUES (@Id, @CustomerId, @Title, @PickupAddress, @DeliveryAddress, @Status, @Priority, @BaseRate, @TotalAmount, @ScheduledPickupTime, @EstimatedDeliveryTime, @AssignedDriverId, @CreatedAt)";

                    insertJob.Parameters.Add(CreateParam(insertJob, "@Id", jobId));
                    insertJob.Parameters.Add(CreateParam(insertJob, "@CustomerId", custId));
                    insertJob.Parameters.Add(CreateParam(insertJob, "@Title", $"Seed Job {j:000} for {custId}"));
                    insertJob.Parameters.Add(CreateParam(insertJob, "@PickupAddress", $"{i} Seed St"));
                    insertJob.Parameters.Add(CreateParam(insertJob, "@DeliveryAddress", $"{i + j} Destination Rd"));
                    insertJob.Parameters.Add(CreateParam(insertJob, "@Status", 0));
                    insertJob.Parameters.Add(CreateParam(insertJob, "@Priority", rnd.Next(1, 4)));
                    insertJob.Parameters.Add(CreateParam(insertJob, "@BaseRate", baseRate));
                    insertJob.Parameters.Add(CreateParam(insertJob, "@TotalAmount", totalAmount));
                    insertJob.Parameters.Add(CreateParam(insertJob, "@ScheduledPickupTime", DateTime.UtcNow));
                    insertJob.Parameters.Add(CreateParam(insertJob, "@EstimatedDeliveryTime", estimated));
                    insertJob.Parameters.Add(CreateParam(insertJob, "@AssignedDriverId", assignedDriverId));
                    insertJob.Parameters.Add(CreateParam(insertJob, "@CreatedAt", DateTime.UtcNow));

                    insertJob.ExecuteNonQuery();

                    // Insert JobAssignment
                    string jaId = $"ja-{jobId}";
                    using var insertJA = connection.CreateCommand();
                    insertJA.Transaction = transaction;
                    insertJA.CommandText = @"
INSERT INTO JobAssignments (Id, JobId, DriverId, AssignedAt, Status, CreatedAt)
VALUES (@Id, @JobId, @DriverId, @AssignedAt, @Status, @CreatedAt)";

                    insertJA.Parameters.Add(CreateParam(insertJA, "@Id", jaId));
                    insertJA.Parameters.Add(CreateParam(insertJA, "@JobId", jobId));
                    insertJA.Parameters.Add(CreateParam(insertJA, "@DriverId", assignedDriverId));
                    insertJA.Parameters.Add(CreateParam(insertJA, "@AssignedAt", DateTime.UtcNow));
                    insertJA.Parameters.Add(CreateParam(insertJA, "@Status", 1));
                    insertJA.Parameters.Add(CreateParam(insertJA, "@CreatedAt", DateTime.UtcNow));

                    insertJA.ExecuteNonQuery();

                    // Insert Invoice
                    string invoiceId = $"inv-{jobId}";
                    string invoiceNumber = $"INV-{i:000}-{j:000}";
                    DateTime invoiceDate = DateTime.UtcNow;
                    DateTime dueDate = invoiceDate.AddDays(30);

                    using var insertInv = connection.CreateCommand();
                    insertInv.Transaction = transaction;
                    insertInv.CommandText = @"
INSERT INTO Invoices (Id, InvoiceNumber, CustomerId, JobId, InvoiceDate, DueDate, Status, SubTotal, TaxRate, TaxAmount, DiscountAmount, TotalAmount, CreatedAt)
VALUES (@Id, @InvoiceNumber, @CustomerId, @JobId, @InvoiceDate, @DueDate, @Status, @SubTotal, @TaxRate, @TaxAmount, @DiscountAmount, @TotalAmount, @CreatedAt)";

                    decimal taxRate = 0.07m;
                    decimal taxAmount = Math.Round(totalAmount * taxRate, 2);
                    decimal subTotal = totalAmount;
                    decimal totalInvoice = Math.Round(subTotal + taxAmount, 2);

                    insertInv.Parameters.Add(CreateParam(insertInv, "@Id", invoiceId));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@InvoiceNumber", invoiceNumber));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@CustomerId", custId));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@JobId", jobId));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@InvoiceDate", invoiceDate));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@DueDate", dueDate));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@Status", 0));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@SubTotal", subTotal));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@TaxRate", taxRate));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@TaxAmount", taxAmount));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@DiscountAmount", 0m));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@TotalAmount", totalInvoice));
                    insertInv.Parameters.Add(CreateParam(insertInv, "@CreatedAt", DateTime.UtcNow));

                    insertInv.ExecuteNonQuery();

                    // Insert at least one InvoiceLineItem
                    using var insertLine = connection.CreateCommand();
                    insertLine.Transaction = transaction;
                    insertLine.CommandText = @"
INSERT INTO InvoiceLineItems (InvoiceId, Description, Quantity, UnitPrice, TaxApplicable, SortOrder)
VALUES (@InvoiceId, @Description, @Quantity, @UnitPrice, @TaxApplicable, @SortOrder)";

                    insertLine.Parameters.Add(CreateParam(insertLine, "@InvoiceId", invoiceId));
                    insertLine.Parameters.Add(CreateParam(insertLine, "@Description", $"Service for job {jobId}"));
                    insertLine.Parameters.Add(CreateParam(insertLine, "@Quantity", 1m));
                    insertLine.Parameters.Add(CreateParam(insertLine, "@UnitPrice", subTotal));
                    insertLine.Parameters.Add(CreateParam(insertLine, "@TaxApplicable", true));
                    insertLine.Parameters.Add(CreateParam(insertLine, "@SortOrder", 1));

                    insertLine.ExecuteNonQuery();
                }
            }
        });
    }

    public override void Down()
    {
        // Remove seeded data for cust-001..cust-100 and related objects
        Execute.WithConnection((connection, transaction) =>
        {
            using var delLines = connection.CreateCommand();
            delLines.Transaction = transaction;
            delLines.CommandText = "DELETE FROM InvoiceLineItems WHERE InvoiceId LIKE 'inv-%'";
            delLines.ExecuteNonQuery();

            using var delInv = connection.CreateCommand();
            delInv.Transaction = transaction;
            delInv.CommandText = "DELETE FROM Invoices WHERE Id LIKE 'inv-%' AND CustomerId LIKE 'cust-%'";
            delInv.ExecuteNonQuery();

            using var delJA = connection.CreateCommand();
            delJA.Transaction = transaction;
            delJA.CommandText = "DELETE FROM JobAssignments WHERE JobId LIKE 'job-cust-%'";
            delJA.ExecuteNonQuery();

            using var delJobs = connection.CreateCommand();
            delJobs.Transaction = transaction;
            delJobs.CommandText = "DELETE FROM Jobs WHERE CustomerId LIKE 'cust-%' AND Id LIKE 'job-%'";
            delJobs.ExecuteNonQuery();

            using var delDrivers = connection.CreateCommand();
            delDrivers.Transaction = transaction;
            delDrivers.CommandText = "DELETE FROM Drivers WHERE Email LIKE 'seed.driver%'";
            delDrivers.ExecuteNonQuery();

            using var delCustomers = connection.CreateCommand();
            delCustomers.Transaction = transaction;
            delCustomers.CommandText = "DELETE FROM Customers WHERE Id LIKE 'cust-%'";
            delCustomers.ExecuteNonQuery();
        });
    }

    /// <summary>
    /// Helper to create a parameter with null handling.
    /// Placed after public methods to satisfy SA1202.
    /// </summary>
    /// <param name="cmd">The command to add the parameter to.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <returns>The created parameter.</returns>
    private static IDbDataParameter CreateParam(IDbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        return p;
    }
}
