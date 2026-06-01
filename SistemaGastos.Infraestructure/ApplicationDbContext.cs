using Microsoft.EntityFrameworkCore;
using SistemaGastos.Application.Interfaces;
using SistemaGastos.Domain.Models;

namespace SistemaGastos.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IApplicationDbContext
{
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
}
