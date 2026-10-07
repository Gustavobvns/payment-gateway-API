using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using payment_gateway_API.src.Auth.Contracts.Register;

namespace payment_gateway_API.src.Auth.Register;

public static class RegisterEndpoints
{
	public static IEndpointRouteBuilder MapRegisterEndpoints(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapPost("/auth/register", HandleAsync);
		return endpoints;
	}

	private static async Task<IResult> HandleAsync(
		RegisterRequest request,
		IRegisterService registerService,
		CancellationToken cancellationToken)
	{
		var response = await registerService.RegisterAsync(request, cancellationToken);
		return Results.Created("/users/me", response);
	}
}