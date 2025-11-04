#pragma warning disable SA1649
using FluentMigrator;

namespace HotshotLogistics.Data.Migrations;

/// <summary>
/// Adds missing columns to the Jobs table to match the domain model and seed data expectations.
/// </summary>
[Migration(20250106030150)]
public class AddMissingJobColumns : Migration
{
    /// <summary>
    /// Applies the migration to add missing columns.
    /// </summary>
    public override void Up()
    {
        // Add description column
        Alter.Table("Jobs")
            .AddColumn("Description").AsString(1000).Nullable();

        // Add pickup location details
        Alter.Table("Jobs")
            .AddColumn("PickupCity").AsString(100).Nullable()
            .AddColumn("PickupState").AsString(50).Nullable()
            .AddColumn("PickupPostalCode").AsString(20).Nullable();

        // Add delivery location details
        Alter.Table("Jobs")
            .AddColumn("DeliveryCity").AsString(100).Nullable()
            .AddColumn("DeliveryState").AsString(50).Nullable()
            .AddColumn("DeliveryPostalCode").AsString(20).Nullable();

        // Add hazardous materials flag
        Alter.Table("Jobs")
            .AddColumn("IsHazardous").AsBoolean().NotNullable().WithDefaultValue(false);
    }

    /// <summary>
    /// Reverts the migration by removing the added columns.
    /// </summary>
    public override void Down()
    {
        Delete.Column("Description").FromTable("Jobs");
        Delete.Column("PickupCity").FromTable("Jobs");
        Delete.Column("PickupState").FromTable("Jobs");
        Delete.Column("PickupPostalCode").FromTable("Jobs");
        Delete.Column("DeliveryCity").FromTable("Jobs");
        Delete.Column("DeliveryState").FromTable("Jobs");
        Delete.Column("DeliveryPostalCode").FromTable("Jobs");
        Delete.Column("IsHazardous").FromTable("Jobs");
    }
}
