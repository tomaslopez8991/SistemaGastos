using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGastos.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMinimumPaymentToAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MinimumPaymentManualOverride",
                table: "Account",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumPaymentPercentage",
                table: "Account",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinimumPaymentManualOverride",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "MinimumPaymentPercentage",
                table: "Account");
        }
    }
}
