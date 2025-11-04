#pragma warning disable SA1649
using FluentMigrator;

namespace HotshotLogistics.Data.Migrations;

/// <summary>
/// Creates the Contacts table with proper foreign key relationship to Customers.
/// </summary>
[Migration(20250106030000)]
public class CreateContactsTable : Migration
{
    /// <summary>
    /// Applies the migration to create the Contacts table.
    /// </summary>
    public override void Up()
    {
        Create.Table("Contacts")
            .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("CustomerId").AsString(50).NotNullable()
            .WithColumn("Name").AsString(100).NotNullable()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("Phone").AsString(20).Nullable()
            .WithColumn("Title").AsString(50).Nullable()
            .WithColumn("IsPrimary").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("CreatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("UpdatedAt").AsDateTime2().Nullable();

        Create.ForeignKey("FK_Contacts_Customers_CustomerId")
            .FromTable("Contacts").ForeignColumn("CustomerId")
            .ToTable("Customers").PrimaryColumn("Id")
            .OnDeleteOrUpdate(System.Data.Rule.Cascade);

        Create.Index("IX_Contacts_CustomerId")
            .OnTable("Contacts")
            .OnColumn("CustomerId");

        Create.Index("IX_Contacts_IsPrimary")
            .OnTable("Contacts")
            .OnColumn("IsPrimary");
    }

    /// <summary>
    /// Reverts the migration by dropping the Contacts table.
    /// </summary>
    public override void Down()
    {
        Delete.Table("Contacts");
    }
}
