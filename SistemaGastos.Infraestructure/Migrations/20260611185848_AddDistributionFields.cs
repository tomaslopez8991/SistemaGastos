using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGastos.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDistributionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DistributionEndDay",
                table: "TmpTransaction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExcludedDays",
                table: "TmpTransaction",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DistributionEndDay",
                table: "FixedIncome",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExcludedDays",
                table: "FixedIncome",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DistributionEndDay",
                table: "FixedExpense",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExcludedDays",
                table: "FixedExpense",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistributionEndDay",
                table: "TmpTransaction");

            migrationBuilder.DropColumn(
                name: "ExcludedDays",
                table: "TmpTransaction");

            migrationBuilder.DropColumn(
                name: "DistributionEndDay",
                table: "FixedIncome");

            migrationBuilder.DropColumn(
                name: "ExcludedDays",
                table: "FixedIncome");

            migrationBuilder.DropColumn(
                name: "DistributionEndDay",
                table: "FixedExpense");

            migrationBuilder.DropColumn(
                name: "ExcludedDays",
                table: "FixedExpense");
        }
    }
}
