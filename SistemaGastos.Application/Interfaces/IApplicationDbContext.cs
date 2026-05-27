using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure; // Para DatabaseFacade
using SistemaGastos.Domain.Models;
using System.Collections.Generic;

namespace SistemaGastos.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Login> Login { get; }
    // Listamos SOLO las tablas que necesitamos tocar desde la lógica
    DbSet<CreditCardTransaction> CreditCardTransaction { get; }
    DbSet<Transaction> Transaction { get; }
    DbSet<TmpTransaction> TmpTransaction { get; }
    DbSet<Account> Account { get; }
    DbSet<Category> Category { get; }
    DbSet<FixedExpense> FixedExpense { get; set; }
    DbSet<FixedExpenseHistory> FixedExpenseHistory { get; set; }
    DbSet<TodoTask> TodoTask { get; set; }
    DbSet<Budget> Budget { get; set; }

    // Métodos esenciales
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    // Para poder usar transacciones (BeginTransactionAsync)
    DatabaseFacade Database { get; }
}