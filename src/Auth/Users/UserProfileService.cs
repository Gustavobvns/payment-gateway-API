using Microsoft.EntityFrameworkCore;
using payment_gateway_API.src.Auth.Contracts.Users;
using payment_gateway_API.src.Auth.Services;
using payment_gateway_API.src.Data;

namespace payment_gateway_API.src.Auth.Users;

public sealed class UserProfileService(AppDbContext dbContext) : IUserProfileService
{
	public async Task<UserProfileResponse?> GetProfileAsync(
		Guid userId,
		CancellationToken cancellationToken)
	{
		return await dbContext.Usuarios
			.AsNoTracking()
			.Where(user => user.Id == userId)
			.Join(
				dbContext.Contas,
				user => user.Id,
				account => account.UsuarioId,
				(user, account) => new UserProfileResponse(
					user.Id,
					user.Nome,
					user.Documento,
					user.Email,
					account.Saldo,
					user.Ativo))
			.FirstOrDefaultAsync(cancellationToken);
	}

	public async Task<UserProfileResponse> UpdateProfileAsync(
		Guid userId,
		UpdateUserRequest request,
		CancellationToken cancellationToken)
	{
		ValidateProfile(request);

		var user = await dbContext.Usuarios
			.SingleOrDefaultAsync(currentUser => currentUser.Id == userId, cancellationToken)
			?? throw new UserNotFoundException();

		var email = request.Email.Trim().ToLowerInvariant();
		var documento = request.Documento.Trim();

		if (await dbContext.Usuarios.AnyAsync(
			currentUser => currentUser.Id != userId && currentUser.Email == email,
			cancellationToken))
		{
			throw new DuplicateUserException("email");
		}

		if (await dbContext.Usuarios.AnyAsync(
			currentUser => currentUser.Id != userId && currentUser.Documento == documento,
			cancellationToken))
		{
			throw new DuplicateUserException("documento");
		}

		user.Nome = request.Nome.Trim();
		user.Documento = documento;
		user.Email = email;

		try
		{
			await dbContext.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException)
		{
			throw new DuplicateUserException("email ou documento");
		}

		return await GetProfileAsync(userId, cancellationToken)
			?? throw new UserNotFoundException();
	}

	public async Task DeleteProfileAsync(Guid userId, CancellationToken cancellationToken)
	{
		var user = await dbContext.Usuarios
			.SingleOrDefaultAsync(currentUser => currentUser.Id == userId, cancellationToken)
			?? throw new UserNotFoundException();

		user.Ativo = false;
		await dbContext.SaveChangesAsync(cancellationToken);
	}

	private static void ValidateProfile(UpdateUserRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.Nome) ||
			string.IsNullOrWhiteSpace(request.Documento) ||
			string.IsNullOrWhiteSpace(request.Email))
		{
			throw new AuthValidationException("Nome, documento e email são obrigatórios.");
		}
	}
}