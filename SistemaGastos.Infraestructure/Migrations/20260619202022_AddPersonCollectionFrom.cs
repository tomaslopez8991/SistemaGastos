using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGastos.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonCollectionFrom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CollectionFrom",
                table: "Person",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CollectionFrom",
                table: "Person");
        }
    }
}
