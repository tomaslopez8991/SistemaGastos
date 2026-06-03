using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGastos.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonAttributionFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PersonID",
                table: "Transaction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PersonID",
                table: "FixedExpense",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PersonID",
                table: "CreditCardTransaction",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Person",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Person", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Person_Login_UserID",
                        column: x => x.UserID,
                        principalTable: "Login",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_PersonID",
                table: "Transaction",
                column: "PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_FixedExpense_PersonID",
                table: "FixedExpense",
                column: "PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardTransaction_PersonID",
                table: "CreditCardTransaction",
                column: "PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_Person_UserID",
                table: "Person",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditCardTransaction_Person_PersonID",
                table: "CreditCardTransaction",
                column: "PersonID",
                principalTable: "Person",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_FixedExpense_Person_PersonID",
                table: "FixedExpense",
                column: "PersonID",
                principalTable: "Person",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_Person_PersonID",
                table: "Transaction",
                column: "PersonID",
                principalTable: "Person",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditCardTransaction_Person_PersonID",
                table: "CreditCardTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_FixedExpense_Person_PersonID",
                table: "FixedExpense");

            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_Person_PersonID",
                table: "Transaction");

            migrationBuilder.DropTable(
                name: "Person");

            migrationBuilder.DropIndex(
                name: "IX_Transaction_PersonID",
                table: "Transaction");

            migrationBuilder.DropIndex(
                name: "IX_FixedExpense_PersonID",
                table: "FixedExpense");

            migrationBuilder.DropIndex(
                name: "IX_CreditCardTransaction_PersonID",
                table: "CreditCardTransaction");

            migrationBuilder.DropColumn(
                name: "PersonID",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "PersonID",
                table: "FixedExpense");

            migrationBuilder.DropColumn(
                name: "PersonID",
                table: "CreditCardTransaction");
        }
    }
}
