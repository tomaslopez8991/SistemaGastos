using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SistemaGastos.Data;

#nullable disable

namespace SistemaGastos.Infraestructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260817190000_AddProjectionScheduleOverridesAndTcStrategy")]
public class AddProjectionScheduleOverridesAndTcStrategy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "DistributionStrategy",
            table: "CreditCardProjectionScenario",
            type: "int",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.CreateTable(
            name: "ProjectionScheduleOverride",
            columns: table => new
            {
                ID = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserID = table.Column<int>(type: "int", nullable: false),
                SourceType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                SourceID = table.Column<long>(type: "bigint", nullable: false),
                YearMonth = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                OriginalDay = table.Column<int>(type: "int", nullable: false),
                TargetDay = table.Column<int>(type: "int", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ProjectionScheduleOverride", x => x.ID));

        migrationBuilder.CreateIndex(
            name: "IX_ProjectionScheduleOverride_UserID_SourceType_SourceID_YearMonth_OriginalDay",
            table: "ProjectionScheduleOverride",
            columns: new[] { "UserID", "SourceType", "SourceID", "YearMonth", "OriginalDay" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ProjectionScheduleOverride");
        migrationBuilder.DropColumn(name: "DistributionStrategy", table: "CreditCardProjectionScenario");
    }
}
