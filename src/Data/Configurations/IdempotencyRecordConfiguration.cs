using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using payment_gateway_API.src.Models;

namespace payment_gateway_API.src.Data.Configurations;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Key).IsRequired().HasMaxLength(200);
        builder.Property(item => item.Operation).IsRequired().HasMaxLength(100);
        builder.Property(item => item.ResponseJson).IsRequired();
        builder.Property(item => item.CreatedAt).IsRequired();
        builder.HasIndex(item => new { item.UserId, item.Operation, item.Key }).IsUnique();
    }
}
