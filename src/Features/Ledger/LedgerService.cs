using Microsoft.EntityFrameworkCore;
using payment_gateway_API.src.Data;
using payment_gateway_API.src.Features.Financial;

namespace payment_gateway_API.src.Features.Ledger;

public sealed class LedgerService(AppDbContext dbContext) : ILedgerService
{
	public async Task<IReadOnlyList<LedgerEntryResponse>> GetAsync(
		Guid userId,
		CancellationToken cancellationToken)
	{
		var accountId = await dbContext.Contas
			.Where(account => account.UsuarioId == userId)
			.Select(account => (Guid?)account.Id)
			.SingleOrDefaultAsync(cancellationToken)
			?? throw new FinancialNotFoundException("A conta do usuário não foi encontrada.");

		return await dbContext.Transacoes
			.AsNoTracking()
			.Where(transaction =>
				transaction.ContaOrigemId == accountId ||
				transaction.ContaDestinoId == accountId)
			.OrderByDescending(transaction => transaction.DataTransacao)
			.ThenByDescending(transaction => transaction.Id)
			.Select(transaction => new LedgerEntryResponse(
				transaction.Id,
				transaction.ContaDestinoId == accountId ? "Entrada" : "Saida",
				transaction.ContaOrigemId,
				transaction.ContaDestinoId,
				transaction.Valor,
				new DateTimeOffset(transaction.DataTransacao, TimeSpan.Zero),
				transaction.CodigoPagamentoId))
			.ToListAsync(cancellationToken);
	}
}
