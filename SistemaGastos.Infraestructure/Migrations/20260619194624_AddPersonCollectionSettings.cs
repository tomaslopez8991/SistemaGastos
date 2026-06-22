using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGastos.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonCollectionSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CollectedMonths",
                table: "Person",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CollectionDay",
                table: "Person",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Person",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CollectedMonths",
                table: "Person");

            migrationBuilder.DropColumn(
                name: "CollectionDay",
                table: "Person");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Person");
        }
    }
}
