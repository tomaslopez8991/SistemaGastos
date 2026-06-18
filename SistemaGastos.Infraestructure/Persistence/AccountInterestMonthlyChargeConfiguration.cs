using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Infrastructure.Persistence.Configurations;

public class AccountInterestMonthlyChargeConfiguration : IEntityTypeConfiguration<AccountInterestMonthlyCharge>
{
    public void Configure(EntityTypeBuilder<AccountInterestMonthlyCharge> builder)
    {
        builder.ToTable("AccountInterestMonthlyCharge");
        builder.HasKey(c => c.ID);
        builder.Property(c => c.ID).ValueGeneratedOnAdd();

        builder.Property(c => c.Year).IsRequired();
        builder.Property(c => c.Month).IsRequired();
        builder.Property(c => c.TotalInterest).HasPrecision(18, 2).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasOne(c => c.Account).WithMany().HasForeignKey(c => c.AccountID).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserID).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.Transaction).WithMany().HasForeignKey(c => c.TransactionID).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.AccountID, c.Year, c.Month }).IsUnique();
    }
}
