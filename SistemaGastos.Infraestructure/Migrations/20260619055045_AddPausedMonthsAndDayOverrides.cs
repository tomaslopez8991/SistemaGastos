using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGastos.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPausedMonthsAndDayOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DayAmountOverrides",
                table: "TmpTransaction",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PausedMonths",
                table: "FixedIncome",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PausedMonths",
                table: "FixedExpense",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DayAmountOverrides",
                table: "TmpTransaction");

            migrationBuilder.DropColumn(
                name: "PausedMonths",
                table: "FixedIncome");

            migrationBuilder.DropColumn(
                name: "PausedMonths",
                table: "FixedExpense");
        }
    }
}
