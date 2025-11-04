#pragma warning disable SA1649
using FluentMigrator;

namespace HotshotLogistics.Data.Migrations;

[Migration(20250101000100)]
public class CreateDriversTable : Migration
{
    public override void Up()
    {
        Create.Table("Drivers")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("FirstName").AsString(100).NotNullable()
            .WithColumn("LastName").AsString(100).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable().Unique()
            .WithColumn("PhoneNumber").AsString(20).Nullable()
            .WithColumn("LicenseNumber").AsString(50).NotNullable()
            .WithColumn("LicenseState").AsString(2).Nullable()
            .WithColumn("LicenseExpiryDate").AsDate().Nullable()
            .WithColumn("InsuranceExpiryDate").AsDate().Nullable()
            .WithColumn("CurrentStatus").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("IsActive").AsBoolean().WithDefaultValue(true)
            .WithColumn("CreatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("UpdatedAt").AsDateTime2().Nullable();
    }

    public override void Down()
    {
        Delete.Table("Drivers");
    }
}
