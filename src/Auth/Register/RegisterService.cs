using Microsoft.EntityFrameworkCore;
using payment_gateway_API.src.Auth.Contracts.Login;
using payment_gateway_API.src.Auth.Contracts.Register;
using payment_gateway_API.src.Auth.Infrastructure;
using payment_gateway_API.src.Auth.Services;
using payment_gateway_API.src.Data;
using payment_gateway_API.src.Models;
using payment_gateway_API.src.Infrastructure;

namespace payment_gateway_API.src.Auth.Register;

public sealed class RegisterService(
	AppDbContext dbContext,
	IPasswordHasher passwordHasher,
	AuthResponseFactory responseFactory) : IRegisterService
{
	public async Task<AuthResponse> RegisterAsync(
		RegisterRequest request,
		CancellationToken cancellationToken)
	{
		ValidateRegistration(request);

		var email = request.Email.Trim().ToLowerInvariant();
		var documento = request.Documento.Trim();

		if (await dbContext.Usuarios.AnyAsync(user => user.Email == email, cancellationToken))
		{
			throw new DuplicateUserException("email");
		}

		if (await dbContext.Usuarios.AnyAsync(user => user.Documento == documento, cancellationToken))
		{
			throw new DuplicateUserException("documento");
		}

		var usuario = new Usuarios
		{
			Nome = request.Nome.Trim(),
			Documento = documento,
			Email = email,
			SenhaHash = passwordHasher.Hash(request.Senha),
			Ativo = true
		};
		var conta = new Contas
		{
			UsuarioId = usuario.Id,
			Saldo = 0m
		};

		// Usuário e conta formam uma única unidade de persistência do cadastro.
		await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
		try
		{
			dbContext.Usuarios.Add(usuario);
			dbContext.Contas.Add(conta);
			await dbContext.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);
		}
		catch (DbUpdateException)
		{
			await transaction.RollbackAsync(cancellationToken);
			throw new DuplicateUserException("email ou documento");
		}
		catch
		{
			await transaction.RollbackAsync(cancellationToken);
			throw;
		}

		return responseFactory.Create(usuario);
	}

	private static void ValidateRegistration(RegisterRequest request)
	{
		InputValidation.ValidatePerson(request.Nome, request.Documento, request.Email);
		InputValidation.ValidatePassword(request.Senha);
	}
}