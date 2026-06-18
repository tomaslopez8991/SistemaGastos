using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Infrastructure.Persistence.Configurations;

public class AccountInterestDailyLogConfiguration : IEntityTypeConfiguration<AccountInterestDailyLog>
{
    public void Configure(EntityTypeBuilder<AccountInterestDailyLog> builder)
    {
        builder.ToTable("AccountInterestDailyLog");
        builder.HasKey(l => l.ID);
        builder.Property(l => l.ID).ValueGeneratedOnAdd();

        builder.Property(l => l.Date).HasColumnType("date").IsRequired();
        builder.Property(l => l.Balance).HasPrecision(18, 2).IsRequired();
        builder.Property(l => l.DayCounter).IsRequired();
        builder.Property(l => l.DailyInterest).HasPrecision(18, 2).IsRequired();
        builder.Property(l => l.CumulativeInterest).HasPrecision(18, 2).IsRequired();
        builder.Property(l => l.CreatedAt).IsRequired();

        builder.HasOne(l => l.Account).WithMany().HasForeignKey(l => l.AccountID).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserID).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.AccountID, l.Date }).IsUnique();
    }
}
