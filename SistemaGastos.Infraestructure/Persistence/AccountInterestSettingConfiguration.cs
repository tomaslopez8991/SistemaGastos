using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Infrastructure.Persistence.Configurations;

public class AccountInterestSettingConfiguration : IEntityTypeConfiguration<AccountInterestSetting>
{
    public void Configure(EntityTypeBuilder<AccountInterestSetting> builder)
    {
        builder.ToTable("AccountInterestSetting");
        builder.HasKey(s => s.ID);
        builder.Property(s => s.ID).ValueGeneratedOnAdd();

        builder.Property(s => s.InterestRate).HasPrecision(9, 4).IsRequired().HasDefaultValue(1.55m);
        builder.Property(s => s.Enabled).IsRequired().HasDefaultValue(true);
        builder.Property(s => s.CreatedAt).IsRequired();

        builder.HasOne(s => s.Account).WithMany().HasForeignKey(s => s.AccountID).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserID).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.AccountID).IsUnique();
    }
}
