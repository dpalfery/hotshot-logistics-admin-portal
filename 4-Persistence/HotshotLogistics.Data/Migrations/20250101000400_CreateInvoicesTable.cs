#pragma warning disable SA1649
using FluentMigrator;

namespace HotshotLogistics.Data.Migrations;

[Migration(20250101000400)]
public class CreateInvoicesTable : Migration
{
    public override void Up()
    {
        Create.Table("Invoices")
            .WithColumn("Id").AsString(50).PrimaryKey()
            .WithColumn("InvoiceNumber").AsString(50).NotNullable().Unique()
            .WithColumn("CustomerId").AsString(50).NotNullable()
            .WithColumn("JobId").AsString(50).Nullable()
            .WithColumn("InvoiceDate").AsDateTime2().NotNullable()
            .WithColumn("DueDate").AsDateTime2().NotNullable()
            .WithColumn("Status").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("SubTotal").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
            .WithColumn("TaxRate").AsDecimal(5, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("TaxAmount").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
            .WithColumn("DiscountAmount").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
            .WithColumn("TotalAmount").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
            .WithColumn("PaidAmount").AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
            .WithColumn("TermsDays").AsInt32().NotNullable().WithDefaultValue(30)
            .WithColumn("EarlyPaymentDiscount").AsDecimal(5, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("EarlyPaymentDiscountDays").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("LatePaymentPenalty").AsDecimal(5, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("LatePaymentPenaltyDays").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("Notes").AsCustom("NVARCHAR(MAX)").Nullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("UpdatedAt").AsDateTime2().Nullable();

        Create.Index("IX_Invoices_CustomerId").OnTable("Invoices").OnColumn("CustomerId");
        Create.Index("IX_Invoices_JobId").OnTable("Invoices").OnColumn("JobId");
        Create.Index("IX_Invoices_Status").OnTable("Invoices").OnColumn("Status");
        Create.Index("IX_Invoices_InvoiceDate").OnTable("Invoices").OnColumn("InvoiceDate");
        Create.Index("IX_Invoices_DueDate").OnTable("Invoices").OnColumn("DueDate");
    }

    public override void Down()
    {
        Delete.Table("Invoices");
    }
}
