using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGastos.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTodoTaskPriorityAndReminder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Priority: 1=Alta, 2=Media, 3=Baja — default 2 para registros existentes
            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "TodoTask",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderDate",
                table: "TodoTask",
                type: "datetime2",
                nullable: true);

            // Description pasa a ser opcional
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "TodoTask",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "TodoTask");

            migrationBuilder.DropColumn(
                name: "ReminderDate",
                table: "TodoTask");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "TodoTask",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldNullable: true);
        }
    }
}
