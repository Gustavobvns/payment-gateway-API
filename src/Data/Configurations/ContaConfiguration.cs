using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using payment_gateway_API.src.Models;

namespace payment_gateway_API.src.Data.Configurations;

public class ContasConfiguration : IEntityTypeConfiguration<Contas>
{
    public void Configure(EntityTypeBuilder<Contas> builder)
    {
        builder.ToTable("Contas");
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.UsuarioId).IsUnique();
        builder.Property(c => c.Version)
               .IsConcurrencyToken()
               .HasDefaultValue(1u);
        builder.Property(c => c.Saldo).IsRequired().HasPrecision(18, 2).HasDefaultValue(0);
        builder.HasOne(c => c.Usuario)
               .WithMany()
               .HasForeignKey(c => c.UsuarioId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}