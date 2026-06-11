using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Infrastructure.Persistence.Configurations;

public class DebtPlanSettingsConfiguration : IEntityTypeConfiguration<DebtPlanSettings>
{
    public void Configure(EntityTypeBuilder<DebtPlanSettings> builder)
    {
        builder.ToTable("DebtPlanSettings");
        builder.HasKey(d => d.ID);
        builder.Property(d => d.ID).ValueGeneratedOnAdd();

        builder.Property(d => d.GoalType).HasMaxLength(20).IsRequired().HasDefaultValue("maxNegative");
        builder.Property(d => d.GoalValue).HasPrecision(18, 2).IsRequired();
        builder.Property(d => d.ExtraMonthlyIncome).HasPrecision(18, 2).IsRequired();
        builder.Property(d => d.ScenariosMode).IsRequired().HasDefaultValue(false);
        builder.Property(d => d.ScenarioMin).HasPrecision(18, 2).IsRequired();
        builder.Property(d => d.ScenarioNormal).HasPrecision(18, 2).IsRequired();
        builder.Property(d => d.ScenarioMax).HasPrecision(18, 2).IsRequired();
        builder.Property(d => d.RemovedFixedExpenseIds).HasMaxLength(1000).IsRequired(false);
        builder.Property(d => d.UpdatedAt).IsRequired();

        builder.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserID).OnDelete(DeleteBehavior.Cascade);

        // Un único registro de configuración por usuario.
        builder.HasIndex(d => d.UserID).IsUnique();
    }
}
