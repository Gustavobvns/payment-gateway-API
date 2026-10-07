using Microsoft.Extensions.Options;
using payment_gateway_API.src.Auth.Configuration;
using payment_gateway_API.src.Auth.Contracts.Login;
using payment_gateway_API.src.Auth.Infrastructure;
using payment_gateway_API.src.Models;

namespace payment_gateway_API.src.Auth.Services;

/// <summary>
/// Centraliza a montagem da resposta que contém o JWT e sua data de expiração.
/// </summary>
public sealed class AuthResponseFactory(
	ITokenService tokenService,
	IOptions<JwtOptions> jwtOptions)
{
	public AuthResponse Create(Usuarios usuario)
	{
		var token = tokenService.GenerateToken(usuario);
		var expiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.Value.ExpirationMinutes);

		return new AuthResponse(token, expiresAt);
	}
}