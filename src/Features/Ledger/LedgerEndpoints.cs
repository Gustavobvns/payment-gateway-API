using payment_gateway_API.src.Auth.CurrentUser;

namespace payment_gateway_API.src.Features.Ledger;

public static class LedgerEndpoints
{
	public static IEndpointRouteBuilder MapLedgerEndpoints(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapGet("/ledger", GetAsync)
			.RequireAuthorization()
			.WithName("GetLedger")
			.WithSummary("Consulta o extrato da conta autenticada")
			.Produces<IReadOnlyList<LedgerEntryResponse>>(StatusCodes.Status200OK);
		return endpoints;
	}

	private static async Task<IResult> GetAsync(
		ICurrentUser currentUser,
		ILedgerService service,
		CancellationToken cancellationToken = default)
	{
		if (currentUser.UserId is not Guid userId)
		{
			return Results.Unauthorized();
		}

		return Results.Ok(await service.GetAsync(userId, cancellationToken));
	}
}
