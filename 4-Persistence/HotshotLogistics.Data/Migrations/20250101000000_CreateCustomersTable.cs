using FluentMigrator;

#pragma warning disable SA1649 // File name should match first type name

namespace HotshotLogistics.Data.Migrations;

[Migration(20250101000000)]
public class CreateCustomersTable : Migration
{
    public override void Up()
    {
        Create.Table("Customers")
            .WithColumn("Id").AsString(50).PrimaryKey()
            .WithColumn("CompanyName").AsString(255).NotNullable()
            .WithColumn("TaxId").AsString(50).Nullable()
            .WithColumn("BillingAddress").AsString(500).Nullable()
            .WithColumn("City").AsString(100).Nullable()
            .WithColumn("State").AsString(50).Nullable()
            .WithColumn("ZipCode").AsString(20).Nullable()
            .WithColumn("Country").AsString(100).Nullable()
            .WithColumn("Latitude").AsDecimal(9, 6).Nullable()
            .WithColumn("Longitude").AsDecimal(9, 6).Nullable()
            .WithColumn("CreditLimit").AsDecimal(18, 2).WithDefaultValue(0)
            .WithColumn("IsActive").AsBoolean().WithDefaultValue(true)
            .WithColumn("CreatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("UpdatedAt").AsDateTime2().Nullable();
    }

    public override void Down()
    {
        Delete.Table("Customers");
    }
}
