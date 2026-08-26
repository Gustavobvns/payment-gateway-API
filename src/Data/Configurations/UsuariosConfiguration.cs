using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using payment_Gateway_API.Models;

namespace payment_Gateway_API.Data.Configurations;

public class UsuariosConfiguration : IEntityTypeConfiguration<Usuarios>
{
    public void Configure(EntityTypeBuilder<Usuarios> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Nome).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Documento).IsRequired().HasMaxLength(20);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(100);
        builder.Property(u => u.SenhaHash).IsRequired().HasMaxLength(255);
        builder.Property(u => u.Ativo).IsRequired().HasDefaultValue(true);

        builder.HasIndex(u => u.Documento).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}