#pragma warning disable SA1649
using FluentMigrator;

namespace HotshotLogistics.Data.Migrations;

[Migration(20250101000300)]
public class CreateJobAssignmentsTable : Migration
{
    public override void Up()
    {
        Create.Table("JobAssignments")
            .WithColumn("Id").AsString(50).PrimaryKey()
            .WithColumn("JobId").AsString(50).NotNullable()
            .WithColumn("DriverId").AsInt32().Nullable()
            .WithColumn("AssignedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("Status").AsInt32().NotNullable().WithDefaultValue(1) // 1 = Active
            .WithColumn("CreatedAt").AsDateTime2().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("UpdatedAt").AsDateTime2().Nullable();

        Create.ForeignKey("FK_JobAssignments_Jobs")
            .FromTable("JobAssignments").ForeignColumn("JobId")
            .ToTable("Jobs").PrimaryColumn("Id");

        Create.ForeignKey("FK_JobAssignments_Drivers")
            .FromTable("JobAssignments").ForeignColumn("DriverId")
            .ToTable("Drivers").PrimaryColumn("Id");

        Create.Index("IX_JobAssignments_DriverId").OnTable("JobAssignments").OnColumn("DriverId");
        Create.Index("IX_JobAssignments_JobId").OnTable("JobAssignments").OnColumn("JobId");
    }

    public override void Down()
    {
        Delete.Table("JobAssignments");
    }
}
