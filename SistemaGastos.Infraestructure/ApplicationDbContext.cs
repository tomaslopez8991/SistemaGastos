using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;
using SistemaGastos.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaGastos.Domain.Enums;

namespace SistemaGastos.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IApplicationDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new FixedIncomeConfiguration());
        modelBuilder.ApplyConfiguration(new AccountInterestSettingConfiguration());
        modelBuilder.ApplyConfiguration(new AccountInterestDailyLogConfiguration());
        modelBuilder.ApplyConfiguration(new AccountInterestMonthlyChargeConfiguration());
        modelBuilder.Entity<Login>(builder =>
        {
            builder.Property(x => x.EmailConfirmed).HasDefaultValue(true);
            builder.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            builder.HasIndex(x => x.EmailConfirmationTokenHash)
                .IsUnique()
                .HasFilter("[EmailConfirmationTokenHash] IS NOT NULL");
        });
        modelBuilder.Entity<CreditCardProjectionScenario>(builder =>
        {
            builder.Property(x => x.YearMonth).HasMaxLength(7).IsRequired();
            builder.Property(x => x.CustomAmount).HasPrecision(18, 2);
            builder.Property(x => x.DistributionStrategy).HasDefaultValue(TcDistributionStrategy.Weekdays);
            builder.HasIndex(x => new { x.AccountID, x.YearMonth }).IsUnique();
            builder.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountID).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ProjectionScheduleOverride>(builder =>
        {
            builder.Property(x => x.SourceType).HasMaxLength(32).IsRequired();
            builder.Property(x => x.YearMonth).HasMaxLength(7).IsRequired();
            builder.HasIndex(x => new { x.UserID, x.SourceType, x.SourceID, x.YearMonth, x.OriginalDay }).IsUnique();
        });

        // FixedExpense tiene dos FKs a Account: AccountID (cuenta de pago) y CreditCardAccountID (TC a saldar).
        // EF necesita configuración explícita para resolver la ambigüedad.
        modelBuilder.Entity<FixedExpense>()
            .HasOne(f => f.Account)
            .WithMany(a => a.FixedExpenses)
            .HasForeignKey(f => f.AccountID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FixedExpense>()
            .HasOne(f => f.CreditCardAccount)
            .WithMany()
            .HasForeignKey(f => f.CreditCardAccountID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FixedExpense>()
            .ToTable("FixedExpense", table =>
            {
                table.HasCheckConstraint("CK_FixedExpense_Amount", "[Amount] >= 0");
                table.HasCheckConstraint("CK_FixedExpense_PaymentDay", "[PaymentDay] >= 1 AND [PaymentDay] <= 31");
            });
    }

    public DbSet<Account> Account { get; set; }
    public DbSet<Transaction> Transaction { get; set; }
    public DbSet<TmpTransaction> TmpTransaction { get; set; }
    public DbSet<Category> Category { get; set; }
    public DbSet<TodoTask> TodoTask { get; set; }
    public DbSet<CreditCardTransaction> CreditCardTransaction { get; set; }
    public DbSet<CreditCardTransactionPerson> CreditCardTransactionPerson { get; set; }
    public DbSet<CreditCardTransactionCobro> CreditCardTransactionCobro { get; set; }
    public DbSet<CreditCardProjectionScenario> CreditCardProjectionScenario { get; set; }
    public DbSet<ProjectionScheduleOverride> ProjectionScheduleOverride { get; set; }
    public DbSet<Login> Login { get; set; }
    public DbSet<Budget> Budget { get; set; }
    public DbSet<FixedExpense> FixedExpense { get; set; }
    public DbSet<FixedExpenseHistory> FixedExpenseHistory { get; set; }
    public DbSet<Person> Person { get; set; }
    public DbSet<FixedIncome> FixedIncome { get; set; }
    public DbSet<AccountInterestSetting> AccountInterestSetting { get; set; }
    public DbSet<AccountInterestDailyLog> AccountInterestDailyLog { get; set; }
    public DbSet<AccountInterestMonthlyCharge> AccountInterestMonthlyCharge { get; set; }
    public DbSet<PerformanceLog> PerformanceLog { get; set; }
}
