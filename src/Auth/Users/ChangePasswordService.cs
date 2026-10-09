using Microsoft.EntityFrameworkCore;
using payment_gateway_API.src.Auth.Contracts.Users;
using payment_gateway_API.src.Auth.Infrastructure;
using payment_gateway_API.src.Auth.Services;
using payment_gateway_API.src.Data;
using payment_gateway_API.src.Infrastructure;

namespace payment_gateway_API.src.Auth.Users;

public sealed class ChangePasswordService(
	AppDbContext dbContext,
	IPasswordHasher passwordHasher) : IChangePasswordService
{
	public async Task ChangePasswordAsync(
		Guid userId,
		ChangePasswordRequest request,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(request.SenhaAtual) || string.IsNullOrWhiteSpace(request.NovaSenha))
		{
			throw new AuthValidationException("A senha atual e a nova senha são obrigatórias.");
		}

		InputValidation.ValidatePassword(request.NovaSenha);

		var usuario = await dbContext.Usuarios
			.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken)
			?? throw new UserNotFoundException();

		if (!usuario.Ativo || !passwordHasher.Verify(request.SenhaAtual, usuario.SenhaHash))
		{
			throw new InvalidCredentialsException();
		}

		usuario.SenhaHash = passwordHasher.Hash(request.NovaSenha);
		await dbContext.SaveChangesAsync(cancellationToken);
	}
}