using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGastos.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyTcScenariosAndIncomeProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiptProgressYearMonth",
                table: "FixedIncome",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReceivedAmount",
                table: "FixedIncome",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReceivedDays",
                table: "FixedIncome",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CreditCardProjectionScenario",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    YearMonth = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    CustomAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardProjectionScenario", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CreditCardProjectionScenario_Account_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Account",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardProjectionScenario_AccountID_YearMonth",
                table: "CreditCardProjectionScenario",
                columns: new[] { "AccountID", "YearMonth" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditCardProjectionScenario");

            migrationBuilder.DropColumn(
                name: "ReceiptProgressYearMonth",
                table: "FixedIncome");

            migrationBuilder.DropColumn(
                name: "ReceivedAmount",
                table: "FixedIncome");

            migrationBuilder.DropColumn(
                name: "ReceivedDays",
                table: "FixedIncome");
        }
    }
}
