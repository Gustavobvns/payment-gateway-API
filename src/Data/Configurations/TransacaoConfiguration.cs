using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using payment_Gateway_API.Models;

namespace payment_Gateway_API.Data.Configurations;

public class TransacoesConfiguration : IEntityTypeConfiguration<Transacoes>
{
    public void Configure(EntityTypeBuilder<Transacoes> builder)
    {
        builder.ToTable("Transacoes");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.ContaOrigemId).IsRequired();
        builder.Property(t => t.ContaDestinoId).IsRequired();
        builder.Property(t => t.Valor).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(t => t.DataTransacao).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(t => t.CodigoPagamentoId).IsUnique();
        // Configuração do relacionamento com a entidade Conta (ContaOrigem)
        builder.HasOne(t => t.ContaOrigem)
               .WithMany()
               .HasForeignKey(t => t.ContaOrigemId)
               .OnDelete(DeleteBehavior.Restrict);

        // Configuração do relacionamento com a entidade Conta (ContaDestino)
        builder.HasOne(t => t.ContaDestino)
               .WithMany()
               .HasForeignKey(t => t.ContaDestinoId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.CodigoPagamentoId)
               .WithOne()
               .HasForeignKey<Transacoes>(t => t.CodigoPagamentoId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict);
    } 
}