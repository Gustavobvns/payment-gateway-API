using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using payment_gateway_API.src.Data;
using payment_gateway_API.src.Features.Financial;
using payment_gateway_API.src.Models;

namespace payment_gateway_API.src.Infrastructure;

public sealed class IdempotencyService(AppDbContext dbContext)
{
    public async Task<T?> GetAsync<T>(
        Guid userId,
        string operation,
        string key,
        string requestHash,
        CancellationToken cancellationToken)
    {
        var record = await dbContext.IdempotencyRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.UserId == userId &&
                        item.Operation == operation &&
                        item.Key == key,
                cancellationToken);

        if (record is null)
        {
            return default;
        }

        if (!string.Equals(record.RequestHash, requestHash, StringComparison.Ordinal))
        {
            throw new FinancialValidationException(
                "A chave de idempotência já foi utilizada com parâmetros diferentes.");
        }

        return JsonSerializer.Deserialize<T>(record.ResponseJson)
            ?? throw new InvalidOperationException("O resultado de idempotência persistido é inválido.");
    }

    public void Add<T>(
        Guid userId,
        string operation,
        string key,
        string requestHash,
        T response,
        int statusCode = StatusCodes.Status200OK)
    {
        dbContext.IdempotencyRecords.Add(new IdempotencyRecord
        {
            UserId = userId,
            Operation = operation,
            Key = key,
            RequestHash = requestHash,
            ResponseJson = JsonSerializer.Serialize(response),
            StatusCode = statusCode
        });
    }

    public static string ComputeRequestHash<T>(T request) =>
        Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request)))).ToLowerInvariant();
}
