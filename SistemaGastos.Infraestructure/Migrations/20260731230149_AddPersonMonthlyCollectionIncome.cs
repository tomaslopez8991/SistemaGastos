using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGastos.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonMonthlyCollectionIncome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CollectionYearMonth",
                table: "FixedIncome",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PersonID",
                table: "FixedIncome",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FixedIncome_PersonID_CollectionYearMonth",
                table: "FixedIncome",
                columns: new[] { "PersonID", "CollectionYearMonth" },
                unique: true,
                filter: "[PersonID] IS NOT NULL AND [CollectionYearMonth] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_FixedIncome_Person_PersonID",
                table: "FixedIncome",
                column: "PersonID",
                principalTable: "Person",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FixedIncome_Person_PersonID",
                table: "FixedIncome");

            migrationBuilder.DropIndex(
                name: "IX_FixedIncome_PersonID_CollectionYearMonth",
                table: "FixedIncome");

            migrationBuilder.DropColumn(
                name: "CollectionYearMonth",
                table: "FixedIncome");

            migrationBuilder.DropColumn(
                name: "PersonID",
                table: "FixedIncome");
        }
    }
}
