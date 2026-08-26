using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using payment_Gateway_API.Models;

namespace payment_Gateway_API.Data.Configurations;

public class CodigosPagamentoConfiguration : IEntityTypeConfiguration<CodigosPagamento>
{
    public void Configure(EntityTypeBuilder<CodigosPagamento> builder)
    {
        builder.ToTable("CodigosPagamento");
        builder.HasKey(cp => cp.Id);
        builder.HasIndex(cp => cp.CodigoPagamentoHash).IsUnique();
        builder.Property(cp => cp.ContaGeradoraId).IsRequired();
        builder.Property(cp => cp.ValorOriginal).IsRequired().HasPrecision(18, 2);
        builder.Property(cp => cp.DataVencimento).IsRequired();
        builder.Property(cp => cp.JurosDiario).IsRequired().HasPrecision(18, 2);
        builder.Property(cp => cp.Status).IsRequired().HasDefaultValue(false);

        // Configuração do relacionamento com a entidade Conta
        builder.HasOne(cp => cp.ContaGeradora)
       .WithMany()
       .HasForeignKey(cp => cp.ContaGeradoraId)
       .OnDelete(DeleteBehavior.Restrict);
    }
}