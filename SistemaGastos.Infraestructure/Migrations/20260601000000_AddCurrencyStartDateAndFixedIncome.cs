using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGastos.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyStartDateAndFixedIncome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── TmpTransaction: agregar Currency ──────────────────────────
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "TmpTransaction",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "ARS");

            // ── FixedExpense: agregar Currency y StartDate ────────────────
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "FixedExpense",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "ARS");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "FixedExpense",
                type: "datetime2",
                nullable: true);

            // ── Transaction: agregar FixedIncomeID ───────────────────────
            migrationBuilder.AddColumn<int>(
                name: "FixedIncomeID",
                table: "Transaction",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_FixedIncomeID",
                table: "Transaction",
                column: "FixedIncomeID");

            // ── Nueva tabla FixedIncome ───────────────────────────────────
            migrationBuilder.CreateTable(
                name: "FixedIncome",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "ARS"),
                    ReceiptDay = table.Column<int>(type: "int", nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastGeneratedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedIncome", x => x.ID);
                    table.ForeignKey(
                        name: "FK_FixedIncome_Account_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Account",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedIncome_Category_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "Category",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedIncome_Login_UserID",
                        column: x => x.UserID,
                        principalTable: "Login",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FixedIncome_AccountID",
                table: "FixedIncome",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_FixedIncome_CategoryID",
                table: "FixedIncome",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_FixedIncome_UserID",
                table: "FixedIncome",
                column: "UserID");

            // FK: Transaction → FixedIncome
            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_FixedIncome_FixedIncomeID",
                table: "Transaction",
                column: "FixedIncomeID",
                principalTable: "FixedIncome",
                principalColumn: "ID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_FixedIncome_FixedIncomeID",
                table: "Transaction");

            migrationBuilder.DropTable(name: "FixedIncome");

            migrationBuilder.DropIndex(
                name: "IX_Transaction_FixedIncomeID",
                table: "Transaction");

            migrationBuilder.DropColumn(name: "FixedIncomeID", table: "Transaction");
            migrationBuilder.DropColumn(name: "StartDate", table: "FixedExpense");
            migrationBuilder.DropColumn(name: "Currency", table: "FixedExpense");
            migrationBuilder.DropColumn(name: "Currency", table: "TmpTransaction");
        }
    }
}
