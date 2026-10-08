using Microsoft.EntityFrameworkCore;
using payment_gateway_API.src.Data;
using payment_gateway_API.src.Features.Financial;
using payment_gateway_API.src.Models;

namespace payment_gateway_API.src.Features.Transfers;

public sealed class TransferService(AppDbContext dbContext) : ITransferService
{
	public async Task<TransferResponse> TransferAsync(
		Guid userId,
		Guid destinationAccountId,
		TransferRequest request,
		CancellationToken cancellationToken)
	{
		if (request.Valor <= 0)
		{
			throw new FinancialValidationException("O valor da transferência deve ser maior que zero.");
		}

		var sourceAccount = await dbContext.Contas
			.Include(account => account.Usuario)
			.SingleOrDefaultAsync(account => account.UsuarioId == userId, cancellationToken)
			?? throw new FinancialNotFoundException("A conta de origem não foi encontrada.");

		var destinationAccount = await dbContext.Contas
			.Include(account => account.Usuario)
			.SingleOrDefaultAsync(account => account.Id == destinationAccountId, cancellationToken)
			?? throw new FinancialNotFoundException("A conta de destino não foi encontrada.");

		if (!sourceAccount.Usuario.Ativo || !destinationAccount.Usuario.Ativo)
		{
			throw new FinancialValidationException("As contas de origem e destino devem estar ativas.");
		}

		if (sourceAccount.Id == destinationAccount.Id)
		{
			throw new FinancialValidationException("A conta de destino deve ser diferente da conta de origem.");
		}

		if (sourceAccount.Saldo < request.Valor)
		{
			throw new InsufficientBalanceException();
		}

		await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
		sourceAccount.Saldo -= request.Valor;
		destinationAccount.Saldo += request.Valor;

		var transfer = new Transacoes
		{
			ContaOrigemId = sourceAccount.Id,
			ContaDestinoId = destinationAccount.Id,
			Valor = request.Valor,
			DataTransacao = DateTime.UtcNow
		};

		dbContext.Transacoes.Add(transfer);
		await dbContext.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return new TransferResponse(
			transfer.Id,
			transfer.ContaOrigemId,
			transfer.ContaDestinoId,
			transfer.Valor,
			new DateTimeOffset(transfer.DataTransacao, TimeSpan.Zero));
	}
}
