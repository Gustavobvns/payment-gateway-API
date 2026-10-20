using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using payment_gateway_API.src.Data;
using payment_gateway_API.src.Features.Financial;
using payment_gateway_API.src.Infrastructure;
using payment_gateway_API.src.Models;

namespace payment_gateway_API.src.Features.Payments;

public sealed class PaymentService(AppDbContext dbContext, IdempotencyService idempotencyService) : IPaymentService
{
	public PaymentService(AppDbContext dbContext)
		: this(dbContext, new IdempotencyService(dbContext))
	{
	}

	public Task<PaymentResponse> CreateAsync(
		Guid userId,
		CreatePaymentRequest request,
		CancellationToken cancellationToken) =>
		CreateAsync(userId, request, Guid.NewGuid().ToString(), cancellationToken);

	public Task<PaymentResult> PayAsync(
		Guid userId,
		string code,
		CancellationToken cancellationToken) =>
		PayAsync(userId, code, Guid.NewGuid().ToString(), cancellationToken);

	public async Task<PaymentResponse> CreateAsync(
		Guid userId,
		CreatePaymentRequest request,
		string idempotencyKey,
		CancellationToken cancellationToken)
	{
		ValidateIdempotencyKey(idempotencyKey);
		var requestHash = IdempotencyService.ComputeRequestHash(request);
		var previousResult = await idempotencyService.GetAsync<PaymentResponse>(
			userId, "payment-create", idempotencyKey, requestHash, cancellationToken);
		if (previousResult is not null)
		{
			return previousResult;
		}

		InputValidation.ValidateMoney(request.ValorOriginal, "O valor");
		InputValidation.ValidateNonNegativeMoney(request.JurosDiario, "Os juros");

		if (request.DataVencimento < DateOnly.FromDateTime(DateTime.UtcNow))
		{
			throw new FinancialValidationException("A data de vencimento não pode estar no passado.");
		}

		var account = await dbContext.Contas
			.Include(item => item.Usuario)
			.SingleOrDefaultAsync(
				item => item.Id == request.ContaRecebedoraId && item.UsuarioId == userId,
				cancellationToken)
			?? throw new FinancialNotFoundException("A conta recebedora não foi encontrada.");

		if (!account.Usuario.Ativo)
		{
			throw new FinancialValidationException("A conta recebedora deve estar ativa.");
		}

		var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();
		var payment = new CodigosPagamento
		{
			CodigoPagamentoHash = Hash(code),
			ContaGeradoraId = account.Id,
			ValorOriginal = request.ValorOriginal,
			DataVencimento = request.DataVencimento,
			JurosDiario = request.JurosDiario,
			Status = false
		};

		dbContext.CodigosPagamento.Add(payment);
		var response = ToResponse(payment, code);
		idempotencyService.Add(userId, "payment-create", idempotencyKey, requestHash, response);
		await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
		try
		{
			await dbContext.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);
			return response;
		}
		catch (DbUpdateException exception)
		{
			await transaction.RollbackAsync(cancellationToken);
			dbContext.ChangeTracker.Clear();
			var previous = await idempotencyService.GetAsync<PaymentResponse>(
				userId, "payment-create", idempotencyKey, requestHash, cancellationToken);
			if (previous is not null)
			{
				return previous;
			}

			throw new InvalidOperationException(
				"A chave de idempotência entrou em conflito, mas o resultado original não foi encontrado.",
				exception);
		}
	}

	public async Task<PaymentResponse> GetAsync(
		string code,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(code))
		{
			throw new FinancialValidationException("O código da cobrança é obrigatório.");
		}

		var payment = await dbContext.CodigosPagamento
			.AsNoTracking()
			.SingleOrDefaultAsync(item => item.CodigoPagamentoHash == Hash(code), cancellationToken)
			?? throw new FinancialNotFoundException("A cobrança não foi encontrada.");

		decimal? settledAmount = payment.Status
			? await dbContext.Transacoes
				.AsNoTracking()
				.Where(item => item.CodigoPagamentoId == payment.Id)
				.Select(item => (decimal?)item.Valor)
				.SingleOrDefaultAsync(cancellationToken)
				?? throw new InvalidOperationException(
					"A cobrança está marcada como paga, mas o lançamento de liquidação não foi encontrado.")
			: null;

		return ToResponse(payment, code, settledAmount);
	}

	public async Task<PaymentResult> PayAsync(
		Guid userId,
		string code,
		string idempotencyKey,
		CancellationToken cancellationToken)
	{
		ValidateIdempotencyKey(idempotencyKey);
		var requestHash = IdempotencyService.ComputeRequestHash(new { code });
		var previousResult = await idempotencyService.GetAsync<PaymentResult>(
			userId, "payment-pay", idempotencyKey, requestHash, cancellationToken);
		if (previousResult is not null)
		{
			return previousResult;
		}

		if (string.IsNullOrWhiteSpace(code))
		{
			throw new FinancialValidationException("O código da cobrança é obrigatório.");
		}

		var payment = await dbContext.CodigosPagamento
			.SingleOrDefaultAsync(item => item.CodigoPagamentoHash == Hash(code), cancellationToken)
			?? throw new FinancialNotFoundException("A cobrança não foi encontrada.");

		if (payment.Status)
		{
			throw new PaymentAlreadyPaidException();
		}

		var receiver = await dbContext.Contas
			.Include(item => item.Usuario)
			.SingleAsync(item => item.Id == payment.ContaGeradoraId, cancellationToken);
		var payer = await dbContext.Contas
			.Include(item => item.Usuario)
			.SingleOrDefaultAsync(item => item.UsuarioId == userId, cancellationToken)
			?? throw new FinancialNotFoundException("A conta pagadora não foi encontrada.");

		if (!receiver.Usuario.Ativo || !payer.Usuario.Ativo)
		{
			throw new FinancialValidationException("As contas devem estar ativas.");
		}

		if (payer.Id == receiver.Id)
		{
			throw new FinancialValidationException("O emissor não pode pagar a própria cobrança.");
		}

		var amount = CalculateCurrentAmount(payment);
		if (payer.Saldo < amount)
		{
			throw new InsufficientBalanceException();
		}

		await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
		payer.Saldo -= amount;
		payer.Version++;
		receiver.Saldo += amount;
		receiver.Version++;
		payment.Status = true;
		payment.Version++;

		var ledgerEntry = new Transacoes
		{
			ContaOrigemId = payer.Id,
			ContaDestinoId = receiver.Id,
			Valor = amount,
			CodigoPagamentoId = payment.Id,
			DataTransacao = DateTime.UtcNow
		};
		dbContext.Transacoes.Add(ledgerEntry);
		var result = new PaymentResult(
			code,
			amount,
			ledgerEntry.Id,
			new DateTimeOffset(ledgerEntry.DataTransacao, TimeSpan.Zero));
		idempotencyService.Add(userId, "payment-pay", idempotencyKey, requestHash, result);
		try
		{
			await dbContext.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);
		}
		catch (DbUpdateConcurrencyException)
		{
			await transaction.RollbackAsync(cancellationToken);
			dbContext.ChangeTracker.Clear();
			var previous = await idempotencyService.GetAsync<PaymentResult>(
				userId, "payment-pay", idempotencyKey, requestHash, cancellationToken);
			if (previous is not null)
			{
				return previous;
			}

			throw new FinancialConcurrencyException();
		}
		catch (DbUpdateException exception)
		{
			await transaction.RollbackAsync(cancellationToken);
			dbContext.ChangeTracker.Clear();
			var previous = await idempotencyService.GetAsync<PaymentResult>(
				userId, "payment-pay", idempotencyKey, requestHash, cancellationToken);
			if (previous is not null)
			{
				return previous;
			}

			throw new InvalidOperationException(
				"A chave de idempotência entrou em conflito, mas o resultado original não foi encontrado.",
				exception);
		}

		return result;
	}

	private static void ValidateIdempotencyKey(string key)
	{
		if (string.IsNullOrWhiteSpace(key) || key.Length > 200)
		{
			throw new FinancialValidationException("O header Idempotency-Key é obrigatório e deve ter até 200 caracteres.");
		}
	}

	private static PaymentResponse ToResponse(
		CodigosPagamento payment,
		string code,
		decimal? settledAmount = null) =>
		new(
			code,
			payment.ContaGeradoraId,
			payment.ValorOriginal,
			settledAmount ?? CalculateCurrentAmount(payment),
			payment.DataVencimento,
			payment.JurosDiario,
			payment.Status);

	private static decimal CalculateCurrentAmount(CodigosPagamento payment)
	{
		var today = DateOnly.FromDateTime(DateTime.UtcNow);
		var overdueDays = Math.Max(0, today.DayNumber - payment.DataVencimento.DayNumber);
		return payment.ValorOriginal + payment.JurosDiario * overdueDays;
	}

	private static string Hash(string code) =>
		Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code))).ToLowerInvariant();
}
