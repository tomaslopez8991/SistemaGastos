using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGastos.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountInterestFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountInterestDailyLog",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DayCounter = table.Column<int>(type: "int", nullable: false),
                    DailyInterest = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountInterestDailyLog", x => x.ID);
                    table.ForeignKey(
                        name: "FK_AccountInterestDailyLog_Account_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Account",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountInterestDailyLog_Login_UserID",
                        column: x => x.UserID,
                        principalTable: "Login",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountInterestMonthlyCharge",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    TotalInterest = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionID = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountInterestMonthlyCharge", x => x.ID);
                    table.ForeignKey(
                        name: "FK_AccountInterestMonthlyCharge_Account_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Account",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountInterestMonthlyCharge_Login_UserID",
                        column: x => x.UserID,
                        principalTable: "Login",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountInterestMonthlyCharge_Transaction_TransactionID",
                        column: x => x.TransactionID,
                        principalTable: "Transaction",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountInterestSetting",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    InterestRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false, defaultValue: 1.55m),
                    Enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountInterestSetting", x => x.ID);
                    table.ForeignKey(
                        name: "FK_AccountInterestSetting_Account_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Account",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountInterestSetting_Login_UserID",
                        column: x => x.UserID,
                        principalTable: "Login",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountInterestDailyLog_AccountID_Date",
                table: "AccountInterestDailyLog",
                columns: new[] { "AccountID", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountInterestDailyLog_UserID",
                table: "AccountInterestDailyLog",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_AccountInterestMonthlyCharge_AccountID_Year_Month",
                table: "AccountInterestMonthlyCharge",
                columns: new[] { "AccountID", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountInterestMonthlyCharge_TransactionID",
                table: "AccountInterestMonthlyCharge",
                column: "TransactionID");

            migrationBuilder.CreateIndex(
                name: "IX_AccountInterestMonthlyCharge_UserID",
                table: "AccountInterestMonthlyCharge",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_AccountInterestSetting_AccountID",
                table: "AccountInterestSetting",
                column: "AccountID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountInterestSetting_UserID",
                table: "AccountInterestSetting",
                column: "UserID");

            // Habilita el cálculo de intereses para la cuenta "Banco Santander Débito" (AccountID 1, UserID 4)
            migrationBuilder.Sql(
                "INSERT INTO AccountInterestSetting (AccountID, UserID, InterestRate, Enabled, CreatedAt) " +
                "SELECT 1, 4, 1.55, 1, GETUTCDATE() " +
                "WHERE EXISTS (SELECT 1 FROM Account WHERE ID = 1 AND UserID = 4) " +
                "AND NOT EXISTS (SELECT 1 FROM AccountInterestSetting WHERE AccountID = 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountInterestDailyLog");

            migrationBuilder.DropTable(
                name: "AccountInterestMonthlyCharge");

            migrationBuilder.DropTable(
                name: "AccountInterestSetting");
        }
    }
}
