using Microsoft.EntityFrameworkCore;
using payment_gateway_API.src.Models;

namespace payment_gateway_API.src.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuarios> Usuarios => Set<Usuarios>();
    public DbSet<Contas> Contas => Set<Contas>();
    public DbSet<CodigosPagamento> CodigosPagamento => Set<CodigosPagamento>();
    public DbSet<Transacoes> Transacoes => Set<Transacoes>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}