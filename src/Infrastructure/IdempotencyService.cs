using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using payment_gateway_API.src.Data;
using payment_gateway_API.src.Models;

namespace payment_gateway_API.src.Infrastructure;

public sealed class IdempotencyService(AppDbContext dbContext)
{
    public async Task<T?> GetAsync<T>(
        Guid userId,
        string operation,
        string key,
        CancellationToken cancellationToken)
    {
        var record = await dbContext.IdempotencyRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.UserId == userId &&
                        item.Operation == operation &&
                        item.Key == key,
                cancellationToken);

        return record is null
            ? default
            : JsonSerializer.Deserialize<T>(record.ResponseJson);
    }

    public async Task<T> SaveOrGetAsync<T>(
        Guid userId,
        string operation,
        string key,
        T response,
        CancellationToken cancellationToken)
    {
        dbContext.IdempotencyRecords.Add(new IdempotencyRecord
        {
            UserId = userId,
            Operation = operation,
            Key = key,
            ResponseJson = JsonSerializer.Serialize(response),
            StatusCode = StatusCodes.Status200OK
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return response;
        }
        catch (DbUpdateException)
        {
            foreach (var entry in dbContext.ChangeTracker.Entries<IdempotencyRecord>()
                         .Where(entry => entry.State == EntityState.Added))
            {
                entry.State = EntityState.Detached;
            }

            var previous = await GetAsync<T>(userId, operation, key, cancellationToken);
            return previous ?? throw new InvalidOperationException(
                "A chave de idempotência entrou em conflito, mas o resultado original não foi encontrado.");
        }
    }
}
