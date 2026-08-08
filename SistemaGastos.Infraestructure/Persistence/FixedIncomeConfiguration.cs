using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Infrastructure.Persistence.Configurations;

public class FixedIncomeConfiguration : IEntityTypeConfiguration<FixedIncome>
{
    public void Configure(EntityTypeBuilder<FixedIncome> builder)
    {
        builder.ToTable("FixedIncome");
        builder.HasKey(f => f.ID);
        builder.Property(f => f.ID).ValueGeneratedOnAdd();

        builder.Property(f => f.Name).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(f => f.Currency).HasMaxLength(3).IsRequired().HasDefaultValue("ARS");
        builder.Property(f => f.ReceiptDay).IsRequired();
        builder.Property(f => f.LogoUrl).HasMaxLength(500).IsRequired(false);
        builder.Property(f => f.Active).IsRequired();
        builder.Property(f => f.StartDate).IsRequired(false);
        builder.Property(f => f.LastGeneratedDate).IsRequired(false);
        builder.Property(f => f.ReceivedAmount).HasPrecision(18, 2);
        builder.Property(f => f.ReceivedDays).HasMaxLength(100).IsRequired(false);
        builder.Property(f => f.ReceiptProgressYearMonth).HasMaxLength(7).IsRequired(false);
        builder.Property(f => f.CollectionYearMonth).HasMaxLength(7).IsRequired(false);

        builder.HasOne(f => f.Category).WithMany().HasForeignKey(f => f.CategoryID).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(f => f.Account).WithMany().HasForeignKey(f => f.AccountID).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(f => f.User).WithMany().HasForeignKey(f => f.UserID).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(f => f.Person).WithMany(p => p.FixedIncomes).HasForeignKey(f => f.PersonID).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(f => new { f.PersonID, f.CollectionYearMonth })
            .IsUnique()
            .HasFilter("[PersonID] IS NOT NULL AND [CollectionYearMonth] IS NOT NULL");

        // La FK inversa Transaction → FixedIncome usa NoAction para evitar
        // ciclos de cascade (Transaction ya tiene Cascade desde Account y Category)
        // La nullificación se maneja manualmente en DeleteFixedIncomeHandler.
    }
}
