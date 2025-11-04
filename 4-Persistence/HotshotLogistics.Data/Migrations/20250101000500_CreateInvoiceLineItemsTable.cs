#pragma warning disable SA1649
using FluentMigrator;

namespace HotshotLogistics.Data.Migrations;

[Migration(20250101000500)]
public class CreateInvoiceLineItemsTable : Migration
{
    public override void Up()
    {
        Create.Table("InvoiceLineItems")
            .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            .WithColumn("InvoiceId").AsString(50).NotNullable()
            .WithColumn("Description").AsString(500).NotNullable()
            .WithColumn("Quantity").AsDecimal(10, 2).NotNullable()
            .WithColumn("UnitPrice").AsDecimal(18, 2).NotNullable()
            .WithColumn("TaxApplicable").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("SortOrder").AsInt32().NotNullable().WithDefaultValue(1);

        Create.ForeignKey("FK_InvoiceLineItems_Invoices")
            .FromTable("InvoiceLineItems").ForeignColumn("InvoiceId")
            .ToTable("Invoices").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Index("IX_InvoiceLineItems_InvoiceId").OnTable("InvoiceLineItems").OnColumn("InvoiceId");
    }

    public override void Down()
    {
        Delete.Table("InvoiceLineItems");
    }
}
