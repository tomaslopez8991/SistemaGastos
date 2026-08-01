using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SistemaGastos.Data;

#nullable disable

namespace SistemaGastos.Infraestructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260801090000_RepairLoginResetColumns")]
public class RepairLoginResetColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH('dbo.Login', 'ResetToken') IS NULL
                ALTER TABLE [dbo].[Login] ADD [ResetToken] nvarchar(max) NULL;

            IF COL_LENGTH('dbo.Login', 'ResetTokenExpiry') IS NULL
                ALTER TABLE [dbo].[Login] ADD [ResetTokenExpiry] datetime2 NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH('dbo.Login', 'ResetTokenExpiry') IS NOT NULL
                ALTER TABLE [dbo].[Login] DROP COLUMN [ResetTokenExpiry];

            IF COL_LENGTH('dbo.Login', 'ResetToken') IS NOT NULL
                ALTER TABLE [dbo].[Login] DROP COLUMN [ResetToken];
            """);
    }
}
