#pragma warning disable SA1649
using FluentMigrator;

namespace HotshotLogistics.Data.Migrations;

[Migration(20250101000200)]
public class CreateJobsTable : Migration
{
    public override void Up()
    {
        Create.Table("Jobs")
            .WithColumn("Id").AsString(50).PrimaryKey()
            .WithColumn("CustomerId").AsString(50).Nullable()
            .WithColumn("Title").AsString(200).NotNullable()
            .WithColumn("PickupAddress").AsString(500).Nullable()
            .WithColumn("PickupLatitude").AsDecimal(10, 7).Nullable()
            .WithColumn("PickupLongitude").AsDecimal(10, 7).Nullable()
            .WithColumn("DeliveryAddress").AsString(500).Nullable()
            .WithColumn("DeliveryLatitude").AsDecimal(10, 7).Nullable()
            .WithColumn("DeliveryLongitude").AsDecimal(10, 7).Nullable()
            .WithColumn("CargoDescription").AsString(1000).Nullable()
            .WithColumn("CargoWeight").AsDecimal(10, 2).Nullable()
            .WithColumn("CargoValue").AsDecimal(18, 2).Nullable()
            .WithColumn("Status").AsInt32().NotNullable()
            .WithColumn("Priority").AsInt32().NotNullable()
            .WithColumn("BaseRate").AsDecimal(18, 2).Nullable()
            .WithColumn("MileageRate").AsDecimal(10, 4).Nullable()
            .WithColumn("TotalAmount").AsDecimal(18, 2).Nullable()
            .WithColumn("EstimatedDeliveryTime").AsDateTime2().Nullable()
            .WithColumn("ActualPickupTime").AsDateTime2().Nullable()
            .WithColumn("ActualDeliveryTime").AsDateTime2().Nullable()
            .WithColumn("AssignedDriverId").AsInt32().Nullable()
            .WithColumn("SpecialInstructions").AsCustom("NVARCHAR(MAX)").Nullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("UpdatedAt").AsDateTime2().Nullable();

        Create.Index("IX_Jobs_Status").OnTable("Jobs").OnColumn("Status");
        Create.Index("IX_Jobs_CustomerId").OnTable("Jobs").OnColumn("CustomerId");
        Create.Index("IX_Jobs_AssignedDriverId").OnTable("Jobs").OnColumn("AssignedDriverId");
    }

    public override void Down()
    {
        Delete.Table("Jobs");
    }
}
