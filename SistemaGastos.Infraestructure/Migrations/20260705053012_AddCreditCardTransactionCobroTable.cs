using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGastos.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCardTransactionCobroTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditCardTransactionCobro",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonID = table.Column<int>(type: "int", nullable: false),
                    CreditCardTransactionID = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardTransactionCobro", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CreditCardTransactionCobro_CreditCardTransaction_CreditCardTransactionID",
                        column: x => x.CreditCardTransactionID,
                        principalTable: "CreditCardTransaction",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditCardTransactionCobro_Person_PersonID",
                        column: x => x.PersonID,
                        principalTable: "Person",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardTransactionCobro_CreditCardTransactionID",
                table: "CreditCardTransactionCobro",
                column: "CreditCardTransactionID");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardTransactionCobro_PersonID",
                table: "CreditCardTransactionCobro",
                column: "PersonID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditCardTransactionCobro");
        }
    }
}
