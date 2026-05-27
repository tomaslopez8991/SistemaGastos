using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Infrastructure.Persistence.Configurations;

public class FixedExpenseConfiguration : IEntityTypeConfiguration<FixedExpense>
{
    public void Configure(EntityTypeBuilder<FixedExpense> builder)
    {
        builder.ToTable("FixedExpense");
        builder.HasKey(f => f.ID);

        builder.Property(f => f.ID)
            .ValueGeneratedOnAdd();

        builder.Property(f => f.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(f => f.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(f => f.PaymentDay)
            .IsRequired();

        builder.Property(f => f.LogoUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(f => f.Active)
            .IsRequired();

        builder.Property(f => f.LastGeneratedDate)
            .IsRequired(false);

        // Relaciones
        builder.HasOne(f => f.Category)
            .WithMany()
            .HasForeignKey(f => f.CategoryID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Account)
            .WithMany()
            .HasForeignKey(f => f.AccountID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserID)
            .OnDelete(DeleteBehavior.Cascade);
    }
}