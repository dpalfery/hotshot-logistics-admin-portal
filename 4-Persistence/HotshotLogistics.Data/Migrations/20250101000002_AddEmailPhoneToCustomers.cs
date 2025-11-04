#pragma warning disable SA1649
using FluentMigrator;

namespace HotshotLogistics.Data.Migrations;

/// <summary>
/// Migration to add Email and Phone columns to Customers table.
/// These columns support direct customer contact information independent of contacts list.
/// </summary>
[Migration(20250101000002)]
public class AddEmailPhoneToCustomers : Migration
{
    /// <inheritdoc/>
    public override void Up()
    {
        // Add Email and Phone columns if they don't exist
        if (!Schema.Table("Customers").Column("Email").Exists())
        {
            Alter.Table("Customers")
                .AddColumn("Email").AsString(255).Nullable();
        }

        if (!Schema.Table("Customers").Column("Phone").Exists())
        {
            Alter.Table("Customers")
                .AddColumn("Phone").AsString(20).Nullable();
        }
    }

    /// <inheritdoc/>
    public override void Down()
    {
        Delete.Column("Email").FromTable("Customers");
        Delete.Column("Phone").FromTable("Customers");
    }
}
