namespace payment_gateway_API.src.Features.Ledger;

public sealed record LedgerEntryResponse(
	Guid Id,
	string Tipo,
	Guid ContaOrigemId,
	Guid ContaDestinoId,
	decimal Valor,
	DateTimeOffset DataTransacao,
	Guid? CodigoPagamentoId);
