namespace payment_gateway_API.src.Features.Payments;

public interface IPaymentService
{
	Task<PaymentResponse> CreateAsync(
		Guid userId,
		CreatePaymentRequest request,
	string idempotencyKey,
	CancellationToken cancellationToken);

	Task<PaymentResponse> GetAsync(
		string code,
		CancellationToken cancellationToken);

	Task<PaymentResult> PayAsync(
		Guid userId,
		string code,
	string idempotencyKey,
	CancellationToken cancellationToken);
}
