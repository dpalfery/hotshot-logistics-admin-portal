#pragma warning disable SA1649
using FluentMigrator;

namespace HotshotLogistics.Data.Migrations;

[Migration(20250101000001)]
public class AddCreditTermsToCustomers : Migration
{
    public override void Up()
    {
        Alter.Table("Customers")
            .AddColumn("PaymentTermsDays").AsInt32().WithDefaultValue(30)
            .AddColumn("CreditStatus").AsInt32().WithDefaultValue(0) // Pending
            .AddColumn("CreditApprovedDate").AsDateTime2().Nullable()
            .AddColumn("CreditExpiryDate").AsDateTime2().Nullable();
    }

    public override void Down()
    {
        Delete.Column("PaymentTermsDays").FromTable("Customers");
        Delete.Column("CreditStatus").FromTable("Customers");
        Delete.Column("CreditApprovedDate").FromTable("Customers");
        Delete.Column("CreditExpiryDate").FromTable("Customers");
    }
}
