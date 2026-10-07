using Microsoft.EntityFrameworkCore;
using payment_gateway_API.src.Auth.Contracts.Login;
using payment_gateway_API.src.Auth.Infrastructure;
using payment_gateway_API.src.Auth.Services;
using payment_gateway_API.src.Data;

namespace payment_gateway_API.src.Auth.Login;

public sealed class LoginService(
	AppDbContext dbContext,
	IPasswordHasher passwordHasher,
	AuthResponseFactory responseFactory) : ILoginService
{
	public async Task<AuthResponse> LoginAsync(
		LoginRequest request,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Senha))
		{
			throw new AuthValidationException("Email e senha são obrigatórios.");
		}

		var email = request.Email.Trim().ToLowerInvariant();
		var usuario = await dbContext.Usuarios
			.SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

		// O mesmo erro externo evita revelar se o email está cadastrado.
		if (usuario is null || !usuario.Ativo || !passwordHasher.Verify(request.Senha, usuario.SenhaHash))
		{
			throw new InvalidCredentialsException();
		}

		return responseFactory.Create(usuario);
	}
}