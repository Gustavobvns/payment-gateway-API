using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using payment_gateway_API.src.Data;
using payment_gateway_API.src.Features.Financial;
using payment_gateway_API.src.Models;

namespace payment_gateway_API.src.Features.Payments;

public sealed class PaymentService(AppDbContext dbContext) : IPaymentService
{
	public async Task<PaymentResponse> CreateAsync(
		Guid userId,
		CreatePaymentRequest request,
		CancellationToken cancellationToken)
	{
		if (request.ValorOriginal <= 0 || request.JurosDiario < 0)
		{
			throw new FinancialValidationException("O valor deve ser maior que zero e os juros não podem ser negativos.");
		}

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
		await dbContext.SaveChangesAsync(cancellationToken);

		return ToResponse(payment, code);
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

		return ToResponse(payment, code);
	}

	public async Task<PaymentResult> PayAsync(
		Guid userId,
		string code,
		CancellationToken cancellationToken)
	{
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
		receiver.Saldo += amount;
		payment.Status = true;

		var ledgerEntry = new Transacoes
		{
			ContaOrigemId = payer.Id,
			ContaDestinoId = receiver.Id,
			Valor = amount,
			CodigoPagamentoId = payment.Id,
			DataTransacao = DateTime.UtcNow
		};
		dbContext.Transacoes.Add(ledgerEntry);
		await dbContext.SaveChangesAsync(cancellationToken);
		await transaction.CommitAsync(cancellationToken);

		return new PaymentResult(
			code,
			amount,
			ledgerEntry.Id,
			new DateTimeOffset(ledgerEntry.DataTransacao, TimeSpan.Zero));
	}

	private static PaymentResponse ToResponse(CodigosPagamento payment, string code) =>
		new(
			code,
			payment.ContaGeradoraId,
			payment.ValorOriginal,
			CalculateCurrentAmount(payment),
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
