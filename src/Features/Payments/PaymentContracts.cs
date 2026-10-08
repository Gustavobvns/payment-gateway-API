namespace payment_gateway_API.src.Features.Payments;

/// <summary>Dados para criar uma cobrança.</summary>
public sealed record CreatePaymentRequest(
	Guid ContaRecebedoraId,
	decimal ValorOriginal,
	DateOnly DataVencimento,
	decimal JurosDiario);

/// <summary>Dados públicos de uma cobrança e seu valor atualizado.</summary>
public sealed record PaymentResponse(
	string Codigo,
	Guid ContaRecebedoraId,
	decimal ValorOriginal,
	decimal ValorAtualizado,
	DateOnly DataVencimento,
	decimal JurosDiario,
	bool Paga);

/// <summary>Resultado do pagamento de uma cobrança.</summary>
public sealed record PaymentResult(
	string Codigo,
	decimal ValorPago,
	Guid TransacaoId,
	DateTimeOffset DataPagamento);
