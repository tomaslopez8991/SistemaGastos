using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SistemaGastos.Data;

#nullable disable

namespace SistemaGastos.Infraestructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260806120000_RepairAccountBalanceColumn")]
public class RepairAccountBalanceColumn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH('dbo.Account', 'Balance') IS NULL
               AND COL_LENGTH('dbo.Account', 'CurrentBalance') IS NOT NULL
                EXEC sp_rename 'dbo.Account.CurrentBalance', 'Balance', 'COLUMN';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH('dbo.Account', 'CurrentBalance') IS NULL
               AND COL_LENGTH('dbo.Account', 'Balance') IS NOT NULL
                EXEC sp_rename 'dbo.Account.Balance', 'CurrentBalance', 'COLUMN';
            """);
    }
}
