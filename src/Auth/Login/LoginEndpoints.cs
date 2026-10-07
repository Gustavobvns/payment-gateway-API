using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using payment_gateway_API.src.Auth.Contracts.Login;

namespace payment_gateway_API.src.Auth.Login;

public static class LoginEndpoints
{
	public static IEndpointRouteBuilder MapLoginEndpoints(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapPost("/auth/login", HandleAsync);
		return endpoints;
	}

	private static async Task<IResult> HandleAsync(
		LoginRequest request,
		ILoginService loginService,
		CancellationToken cancellationToken)
	{
		var response = await loginService.LoginAsync(request, cancellationToken);
		return Results.Ok(response);
	}
}