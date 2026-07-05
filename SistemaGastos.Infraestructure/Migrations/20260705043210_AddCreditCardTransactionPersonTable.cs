using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGastos.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCardTransactionPersonTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FixedExpense_Account_CreditCardAccountID",
                table: "FixedExpense");

            migrationBuilder.DropColumn(
                name: "PersonPercentage",
                table: "CreditCardTransaction");

            migrationBuilder.CreateTable(
                name: "CreditCardTransactionPerson",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreditCardTransactionID = table.Column<int>(type: "int", nullable: false),
                    PersonID = table.Column<int>(type: "int", nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardTransactionPerson", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CreditCardTransactionPerson_CreditCardTransaction_CreditCardTransactionID",
                        column: x => x.CreditCardTransactionID,
                        principalTable: "CreditCardTransaction",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditCardTransactionPerson_Person_PersonID",
                        column: x => x.PersonID,
                        principalTable: "Person",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardTransactionPerson_CreditCardTransactionID",
                table: "CreditCardTransactionPerson",
                column: "CreditCardTransactionID");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardTransactionPerson_PersonID",
                table: "CreditCardTransactionPerson",
                column: "PersonID");

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

            migrationBuilder.DropTable(
                name: "CreditCardTransactionPerson");

            migrationBuilder.AddColumn<decimal>(
                name: "PersonPercentage",
                table: "CreditCardTransaction",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FixedExpense_Account_CreditCardAccountID",
                table: "FixedExpense",
                column: "CreditCardAccountID",
                principalTable: "Account",
                principalColumn: "ID",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
