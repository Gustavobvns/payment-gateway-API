namespace payment_gateway_API.src.Features.Ledger;

public interface ILedgerService
{
	Task<IReadOnlyList<LedgerEntryResponse>> GetAsync(
		Guid userId,
		CancellationToken cancellationToken);
}
