using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGastos.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCardPaymentToFixedExpense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreditCardAccountID",
                table: "FixedExpense",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentYearMonth",
                table: "FixedExpense",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FixedExpense_CreditCardAccountID",
                table: "FixedExpense",
                column: "CreditCardAccountID");

            migrationBuilder.AddForeignKey(
                name: "FK_FixedExpense_Account_CreditCardAccountID",
                table: "FixedExpense",
                column: "CreditCardAccountID",
                principalTable: "Account",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FixedExpense_Account_CreditCardAccountID",
                table: "FixedExpense");

            migrationBuilder.DropIndex(
                name: "IX_FixedExpense_CreditCardAccountID",
                table: "FixedExpense");

            migrationBuilder.DropColumn(
                name: "CreditCardAccountID",
                table: "FixedExpense");

            migrationBuilder.DropColumn(
                name: "PaymentYearMonth",
                table: "FixedExpense");
        }
    }
}
