#pragma warning disable SA1649
using FluentMigrator;

namespace HotshotLogistics.Data.Migrations;

[Migration(20250101000600)]
public class CreateLocationTrackingTable : Migration
{
    public override void Up()
    {
        Create.Table("LocationTracking")
            .WithColumn("Id").AsInt64().PrimaryKey().Identity()
            .WithColumn("JobId").AsString(50).NotNullable()
            .WithColumn("DriverId").AsInt32().NotNullable()
            .WithColumn("Latitude").AsDecimal(10, 7).NotNullable()
            .WithColumn("Longitude").AsDecimal(10, 7).NotNullable()
            .WithColumn("Speed").AsDecimal(6, 2).Nullable()
            .WithColumn("Heading").AsInt32().Nullable()
            .WithColumn("Accuracy").AsDecimal(8, 2).Nullable()
            .WithColumn("Timestamp").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentDateTime);

        Create.Index("IX_LocationTracking_JobId").OnTable("LocationTracking").OnColumn("JobId");
        Create.Index("IX_LocationTracking_DriverId").OnTable("LocationTracking").OnColumn("DriverId");
        Create.Index("IX_LocationTracking_Timestamp").OnTable("LocationTracking").OnColumn("Timestamp");
        Create.Index("IX_LocationTracking_JobId_Timestamp").OnTable("LocationTracking").OnColumn("JobId").Ascending().OnColumn("Timestamp").Ascending();
        Create.Index("IX_LocationTracking_DriverId_Timestamp").OnTable("LocationTracking").OnColumn("DriverId").Ascending().OnColumn("Timestamp").Ascending();
    }

    public override void Down()
    {
        Delete.Table("LocationTracking");
    }
}
