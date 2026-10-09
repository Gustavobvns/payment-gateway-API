using payment_gateway_API.src.Auth.CurrentUser;
using Microsoft.AspNetCore.Mvc;

namespace payment_gateway_API.src.Features.Payments;

public static class PaymentEndpoints
{
	public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/payments");
		group.MapPost("", CreateAsync)
			.RequireAuthorization()
			.WithName("CreatePayment")
			.WithSummary("Cria uma cobrança")
			.Produces<PaymentResponse>(StatusCodes.Status201Created);
		group.MapGet("/{code}", GetAsync)
			.WithName("GetPayment")
			.WithSummary("Consulta uma cobrança")
			.Produces<PaymentResponse>(StatusCodes.Status200OK);
		group.MapPost("/{code}/pay", PayAsync)
			.RequireAuthorization()
			.WithName("PayPayment")
			.WithSummary("Paga uma cobrança")
			.Produces<PaymentResult>(StatusCodes.Status200OK);
		return endpoints;
	}

	private static async Task<IResult> CreateAsync(
		CreatePaymentRequest request,
		[FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
		ICurrentUser currentUser,
		IPaymentService service,
		CancellationToken cancellationToken)
	{
		if (currentUser.UserId is not Guid userId)
		{
			return Results.Unauthorized();
		}

		var payment = await service.CreateAsync(
			userId, request, idempotencyKey ?? string.Empty, cancellationToken);
		return Results.Created($"/payments/{payment.Codigo}", payment);
	}

	private static async Task<IResult> GetAsync(
		string code,
		IPaymentService service,
		CancellationToken cancellationToken) =>
		Results.Ok(await service.GetAsync(code, cancellationToken));

	private static async Task<IResult> PayAsync(
		string code,
		[FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
		ICurrentUser currentUser,
		IPaymentService service,
		CancellationToken cancellationToken)
	{
		if (currentUser.UserId is not Guid userId)
		{
			return Results.Unauthorized();
		}

		return Results.Ok(await service.PayAsync(
			userId, code, idempotencyKey ?? string.Empty, cancellationToken));
	}
}
