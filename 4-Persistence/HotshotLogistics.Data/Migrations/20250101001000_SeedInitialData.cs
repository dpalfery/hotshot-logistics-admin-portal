#pragma warning disable SA1649
using FluentMigrator;

namespace HotshotLogistics.Data.Migrations;

[Migration(20250101001000)]
public class SeedInitialData : Migration
{
    public override void Up()
    {
        // Seed Customers (string PKs) with Email and Phone
        Execute.Sql(@"
INSERT INTO Customers (Id, CompanyName, TaxId, Email, Phone, BillingAddress, City, State, ZipCode, Country, Latitude, Longitude, CreditLimit, PaymentTermsDays, CreditStatus, IsActive, CreatedAt)
VALUES
('cust-1', 'Acme Logistics', 'TAX-001', 'contact@acmelogistics.com', '555-0001', '100 Logistics Way', 'Springfield', 'IL', '62701', 'USA', 39.7817, -89.6501, 10000.00, 30, 1, 1, GETUTCDATE()),
('cust-2', 'QuickShip Co', 'TAX-002', 'info@quickship.com', '555-0002', '200 Express Blvd', 'Chicago', 'IL', '60601', 'USA', 41.8781, -87.6298, 5000.00, 30, 1, 1, GETUTCDATE());
");

        // Seed Drivers (explicit identity insert so we control IDs for seeds)
        Execute.Sql(@"
SET IDENTITY_INSERT Drivers ON;

INSERT INTO Drivers (Id, FirstName, LastName, Email, PhoneNumber, LicenseNumber, LicenseExpiryDate, IsActive, CreatedAt)
VALUES
(1, 'Alice', 'Smith', 'alice.smith@example.com', '555-0101', 'ALICE-L-001', '2030-01-01', 1, GETUTCDATE()),
(2, 'Bob', 'Johnson', 'bob.johnson@example.com', '555-0102', 'BOB-L-002', '2030-01-01', 1, GETUTCDATE());

SET IDENTITY_INSERT Drivers OFF;
");

        // Seed Jobs (string PKs) - need ScheduledPickupTime column
        Execute.Sql(@"
INSERT INTO Jobs (Id, CustomerId, Title, PickupAddress, DeliveryAddress, Status, Priority, TotalAmount, ScheduledPickupTime, EstimatedDeliveryTime, AssignedDriverId, CreatedAt)
VALUES
('job-1', 'cust-1', 'Deliver Package A', '100 Logistics Way', '400 Market St', 0, 1, 150.00, GETUTCDATE(), DATEADD(hour, 4, GETUTCDATE()), 1, GETUTCDATE()),
('job-2', 'cust-2', 'Deliver Package B', '200 Express Blvd', '500 Lake Ave', 0, 2, 200.00, GETUTCDATE(), DATEADD(hour, 6, GETUTCDATE()), 2, GETUTCDATE());
");

        // Seed JobAssignments (string PKs)
        Execute.Sql(@"
INSERT INTO JobAssignments (Id, JobId, DriverId, AssignedAt, Status, CreatedAt)
VALUES
('ja-1', 'job-1', 1, GETUTCDATE(), 1, GETUTCDATE()),
('ja-2', 'job-2', 2, GETUTCDATE(), 1, GETUTCDATE());
");
    }

    public override void Down()
    {
        // Remove seeded job assignments
        Execute.Sql(@"
DELETE FROM JobAssignments WHERE Id IN ('ja-1','ja-2');
");

        // Remove seeded jobs
        Execute.Sql(@"
DELETE FROM Jobs WHERE Id IN ('job-1','job-2');
");

        // Remove seeded drivers (explicit delete; IDs were set)
        Execute.Sql(@"
DELETE FROM Drivers WHERE Id IN (1,2);
");

        // Remove seeded customers
        Execute.Sql(@"
DELETE FROM Customers WHERE Id IN ('cust-1','cust-2');
");
    }
}
