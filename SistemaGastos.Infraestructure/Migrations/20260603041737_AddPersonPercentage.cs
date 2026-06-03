using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGastos.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonPercentage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PersonPercentage",
                table: "Transaction",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PersonPercentage",
                table: "FixedExpense",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PersonPercentage",
                table: "CreditCardTransaction",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersonPercentage",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "PersonPercentage",
                table: "FixedExpense");

            migrationBuilder.DropColumn(
                name: "PersonPercentage",
                table: "CreditCardTransaction");
        }
    }
}
