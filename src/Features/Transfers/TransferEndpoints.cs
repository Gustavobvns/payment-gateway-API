using payment_gateway_API.src.Auth.CurrentUser;

namespace payment_gateway_API.src.Features.Transfers;

public static class TransferEndpoints
{
	public static IEndpointRouteBuilder MapTransferEndpoints(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapPost("/transfers/{destinationAccountId:guid}", TransferAsync)
			.RequireAuthorization()
			.WithName("CreateTransfer")
			.WithSummary("Transfere saldo para outra conta")
			.Produces<TransferResponse>(StatusCodes.Status201Created)
			.Produces(StatusCodes.Status400BadRequest)
			.Produces(StatusCodes.Status401Unauthorized)
			.Produces(StatusCodes.Status404NotFound)
			.Produces(StatusCodes.Status409Conflict);

		return endpoints;
	}

	private static async Task<IResult> TransferAsync(
		Guid destinationAccountId,
		TransferRequest request,
		ICurrentUser currentUser,
		ITransferService transferService,
		CancellationToken cancellationToken)
	{
		if (currentUser.UserId is not Guid userId)
		{
			return Results.Unauthorized();
		}

		var transfer = await transferService.TransferAsync(
			userId,
			destinationAccountId,
			request,
			cancellationToken);

		return Results.Created("/ledger", transfer);
	}
}
