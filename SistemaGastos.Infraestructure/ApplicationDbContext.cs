using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;
using SistemaGastos.Infrastructure.Persistence.Configurations;

namespace SistemaGastos.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IApplicationDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new FixedIncomeConfiguration());
        modelBuilder.ApplyConfiguration(new DebtPlanSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new AccountInterestSettingConfiguration());
        modelBuilder.ApplyConfiguration(new AccountInterestDailyLogConfiguration());
        modelBuilder.ApplyConfiguration(new AccountInterestMonthlyChargeConfiguration());
    }

    public DbSet<Account> Account { get; set; }
    public DbSet<Transaction> Transaction { get; set; }
    public DbSet<TmpTransaction> TmpTransaction { get; set; }
    public DbSet<Category> Category { get; set; }
    public DbSet<TodoTask> TodoTask { get; set; }
    public DbSet<CreditCardTransaction> CreditCardTransaction { get; set; }
    public DbSet<Login> Login { get; set; }
    public DbSet<Budget> Budget { get; set; }
    public DbSet<FixedExpense> FixedExpense { get; set; }
    public DbSet<FixedExpenseHistory> FixedExpenseHistory { get; set; }
    public DbSet<Person> Person { get; set; }
    public DbSet<FixedIncome> FixedIncome { get; set; }
    public DbSet<DebtPlanSettings> DebtPlanSettings { get; set; }
    public DbSet<AccountInterestSetting> AccountInterestSetting { get; set; }
    public DbSet<AccountInterestDailyLog> AccountInterestDailyLog { get; set; }
    public DbSet<AccountInterestMonthlyCharge> AccountInterestMonthlyCharge { get; set; }
}
