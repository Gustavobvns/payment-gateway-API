using Microsoft.EntityFrameworkCore;
using payment_gateway_API.src.Data;
using payment_gateway_API.src.Features.Financial;
using payment_gateway_API.src.Infrastructure;
using payment_gateway_API.src.Models;

namespace payment_gateway_API.src.Features.Transfers;

public sealed class TransferService(AppDbContext dbContext, IdempotencyService idempotencyService) : ITransferService
{
	public TransferService(AppDbContext dbContext)
		: this(dbContext, new IdempotencyService(dbContext))
	{
	}

	public Task<TransferResponse> TransferAsync(
		Guid userId,
		Guid destinationAccountId,
		TransferRequest request,
		CancellationToken cancellationToken) =>
		TransferAsync(userId, destinationAccountId, request, Guid.NewGuid().ToString(), cancellationToken);

	public async Task<TransferResponse> TransferAsync(
		Guid userId,
		Guid destinationAccountId,
		TransferRequest request,
		string idempotencyKey,
		CancellationToken cancellationToken)
	{
		ValidateIdempotencyKey(idempotencyKey);
		var previous = await idempotencyService.GetAsync<TransferResponse>(
			userId, "transfer", idempotencyKey, cancellationToken);
		if (previous is not null)
		{
			return previous;
		}

		InputValidation.ValidateMoney(request.Valor, "O valor da transferência");

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
		sourceAccount.Version++;
		destinationAccount.Saldo += request.Valor;
		destinationAccount.Version++;

		var transfer = new Transacoes
		{
			ContaOrigemId = sourceAccount.Id,
			ContaDestinoId = destinationAccount.Id,
			Valor = request.Valor,
			DataTransacao = DateTime.UtcNow
		};

		dbContext.Transacoes.Add(transfer);
		try
		{
			await dbContext.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);
		}
		catch (DbUpdateConcurrencyException)
		{
			throw new FinancialConcurrencyException();
		}

		var response = new TransferResponse(
			transfer.Id,
			transfer.ContaOrigemId,
			transfer.ContaDestinoId,
			transfer.Valor,
			new DateTimeOffset(transfer.DataTransacao, TimeSpan.Zero));
		return await idempotencyService.SaveOrGetAsync(
			userId, "transfer", idempotencyKey, response, cancellationToken);
	}

	private static void ValidateIdempotencyKey(string key)
	{
		if (string.IsNullOrWhiteSpace(key) || key.Length > 200)
		{
			throw new FinancialValidationException("O header Idempotency-Key é obrigatório e deve ter até 200 caracteres.");
		}
	}
}
