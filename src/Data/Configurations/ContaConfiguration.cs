using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using payment_Gateway_API.Models;

namespace payment_Gateway_API.Data.Configurations;

public class ContasConfiguration : IEntityTypeConfiguration<Contas>
{
    public void Configure(EntityTypeBuilder<Contas> builder)
    {
        builder.ToTable("Contas");
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.UsuarioId);
        builder.Property(c => c.Saldo).IsRequired().HasPrecision(18, 2).HasDefaultValue(0);
        builder.HasOne(c => c.Usuario)
               .WithMany()
               .HasForeignKey(c => c.UsuarioId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}