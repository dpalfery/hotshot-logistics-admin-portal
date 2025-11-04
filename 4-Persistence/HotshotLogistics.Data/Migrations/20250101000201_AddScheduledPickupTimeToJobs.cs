#pragma warning disable SA1649
using FluentMigrator;

namespace HotshotLogistics.Data.Migrations;

[Migration(20250101000201)]
public class AddScheduledPickupTimeToJobs : Migration
{
    public override void Up()
    {
        Alter.Table("Jobs")
            .AddColumn("ScheduledPickupTime").AsDateTime2().Nullable();
    }

    public override void Down()
    {
        Delete.Column("ScheduledPickupTime").FromTable("Jobs");
    }
}
