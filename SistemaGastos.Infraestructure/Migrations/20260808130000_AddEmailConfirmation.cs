using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGastos.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmationEmailSentAt",
                table: "Login",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Login",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmationTokenExpiry",
                table: "Login",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailConfirmationTokenHash",
                table: "Login",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "Login",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_Login_EmailConfirmationTokenHash",
                table: "Login",
                column: "EmailConfirmationTokenHash",
                unique: true,
                filter: "[EmailConfirmationTokenHash] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Login_EmailConfirmationTokenHash",
                table: "Login");

            migrationBuilder.DropColumn(
                name: "ConfirmationEmailSentAt",
                table: "Login");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Login");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationTokenExpiry",
                table: "Login");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationTokenHash",
                table: "Login");

            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "Login");

        }
    }
}
