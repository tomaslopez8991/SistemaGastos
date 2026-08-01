using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGastos.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowZeroFixedExpenseAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM sys.check_constraints
                    WHERE [name] = N'CK_FixedExpense_Amount'
                      AND [parent_object_id] = OBJECT_ID(N'[dbo].[FixedExpense]'))
                    ALTER TABLE [dbo].[FixedExpense] DROP CONSTRAINT [CK_FixedExpense_Amount];

                ALTER TABLE [dbo].[FixedExpense] WITH CHECK
                    ADD CONSTRAINT [CK_FixedExpense_Amount] CHECK ([Amount] >= 0);

                IF NOT EXISTS (
                    SELECT 1 FROM sys.check_constraints
                    WHERE [name] = N'CK_FixedExpense_PaymentDay'
                      AND [parent_object_id] = OBJECT_ID(N'[dbo].[FixedExpense]'))
                    ALTER TABLE [dbo].[FixedExpense] WITH CHECK
                        ADD CONSTRAINT [CK_FixedExpense_PaymentDay]
                        CHECK ([PaymentDay] >= 1 AND [PaymentDay] <= 31);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM sys.check_constraints
                    WHERE [name] = N'CK_FixedExpense_Amount'
                      AND [parent_object_id] = OBJECT_ID(N'[dbo].[FixedExpense]'))
                    ALTER TABLE [dbo].[FixedExpense] DROP CONSTRAINT [CK_FixedExpense_Amount];

                ALTER TABLE [dbo].[FixedExpense] WITH CHECK
                    ADD CONSTRAINT [CK_FixedExpense_Amount] CHECK ([Amount] > 0);
                """);
        }
    }
}
