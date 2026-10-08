namespace payment_gateway_API.src.Features.Transfers;

public sealed record TransferRequest(decimal Valor);

public sealed record TransferResponse(
	Guid Id,
	Guid ContaOrigemId,
	Guid ContaDestinoId,
	decimal Valor,
	DateTimeOffset DataTransacao);
