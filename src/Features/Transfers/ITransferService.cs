namespace payment_gateway_API.src.Features.Transfers;

public interface ITransferService
{
	Task<TransferResponse> TransferAsync(
		Guid userId,
		Guid destinationAccountId,
		TransferRequest request,
		CancellationToken cancellationToken);
}
