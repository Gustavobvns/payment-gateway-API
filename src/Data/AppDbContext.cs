using Microsoft.EntityFrameworkCore;
using payment_Gateway_API.Models;

namespace payment_Gateway_API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuarios> Usuarios => Set<Usuarios>();
    public DbSet<Contas> Contas => Set<Contas>();
    public DbSet<CodigosPagamento> CodigosPagamento => Set<CodigosPagamento>();
    public DbSet<Transacoes> Transacoes => Set<Transacoes>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}