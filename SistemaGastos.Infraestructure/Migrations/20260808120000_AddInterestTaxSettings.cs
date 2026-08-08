using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SistemaGastos.Data;

#nullable disable

namespace SistemaGastos.Infraestructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260808120000_AddInterestTaxSettings")]
public class AddInterestTaxSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "ApplyStampTax",
            table: "AccountInterestSetting",
            type: "bit",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "ApplyVat",
            table: "AccountInterestSetting",
            type: "bit",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<decimal>(
            name: "StampTaxAnnualRate",
            table: "AccountInterestSetting",
            type: "decimal(9,6)",
            precision: 9,
            scale: 6,
            nullable: false,
            defaultValue: 0.012m);

        migrationBuilder.AddColumn<decimal>(
            name: "VatRate",
            table: "AccountInterestSetting",
            type: "decimal(9,6)",
            precision: 9,
            scale: 6,
            nullable: false,
            defaultValue: 0.21m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ApplyStampTax", table: "AccountInterestSetting");
        migrationBuilder.DropColumn(name: "ApplyVat", table: "AccountInterestSetting");
        migrationBuilder.DropColumn(name: "StampTaxAnnualRate", table: "AccountInterestSetting");
        migrationBuilder.DropColumn(name: "VatRate", table: "AccountInterestSetting");
    }
}
