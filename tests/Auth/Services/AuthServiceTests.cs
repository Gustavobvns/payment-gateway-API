using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using payment_gateway_API.src.Auth.Configuration;
using payment_gateway_API.src.Auth.Contracts.Login;
using payment_gateway_API.src.Auth.Contracts.Register;
using payment_gateway_API.src.Auth.Contracts.Users;
using payment_gateway_API.src.Auth.Infrastructure;
using payment_gateway_API.src.Auth.Login;
using payment_gateway_API.src.Auth.Register;
using payment_gateway_API.src.Auth.Services;
using payment_gateway_API.src.Auth.Users;
using payment_gateway_API.src.Data;
using payment_gateway_API.src.Models;

namespace payment_gateway_API.Tests.Auth.Services;

public class AuthServiceTests
{
	private static readonly JwtOptions JwtConfiguration = new()
	{
		Key = "development-only-change-this-key-before-production-123456",
		Issuer = "payment-gateway-api",
		Audience = "payment-gateway-client",
		ExpirationMinutes = 60
	};

	[Fact]
	public async Task RegisterDeveCriarUsuarioEContaComSaldoZero()
	{
		await using var database = CreateDatabase();
		var tokenService = CreateTokenService();
		var service = CreateRegisterService(database, tokenService.Object);

		var response = await service.RegisterAsync(
			new RegisterRequest("Gustavo", "12345678900", "GUSTAVO@EXAMPLE.COM", "Senha-Forte-123"),
			CancellationToken.None);

		var usuario = await database.Usuarios.SingleAsync();
		var conta = await database.Contas.SingleAsync();

		response.AccessToken.Should().Be("token-gerado");
		usuario.Email.Should().Be("gustavo@example.com");
		usuario.SenhaHash.Should().NotBe("Senha-Forte-123");
		conta.UsuarioId.Should().Be(usuario.Id);
		conta.Saldo.Should().Be(0m);
	}

	[Fact]
	public async Task RegisterDeveRejeitarEmailDuplicado()
	{
		await using var database = CreateDatabase();
		var service = CreateRegisterService(database, CreateTokenService().Object);
		var request = new RegisterRequest("Gustavo", "12345678900", "gustavo@example.com", "Senha-Forte-123");

		await service.RegisterAsync(request, CancellationToken.None);

		var action = () => service.RegisterAsync(
			request with { Documento = "98765432100" },
			CancellationToken.None);

		await action.Should().ThrowAsync<DuplicateUserException>();
	}

	[Fact]
	public async Task LoginDeveGerarTokenComCredenciaisValidas()
	{
		await using var database = CreateDatabase();
		var hasher = new PasswordHasher();
		var usuario = CreateUser(hasher.Hash("Senha-Forte-123"));
		database.Usuarios.Add(usuario);
		await database.SaveChangesAsync();
		var tokenService = CreateTokenService();
		var service = CreateLoginService(database, tokenService.Object, hasher);

		var response = await service.LoginAsync(
			new LoginRequest(" GUSTAVO@EXAMPLE.COM ", "Senha-Forte-123"),
			CancellationToken.None);

		response.AccessToken.Should().Be("token-gerado");
		tokenService.Verify(token => token.GenerateToken(It.Is<Usuarios>(user => user.Id == usuario.Id)), Times.Once);
	}

	[Fact]
	public async Task LoginDeveRejeitarSenhaIncorreta()
	{
		await using var database = CreateDatabase();
		var hasher = new PasswordHasher();
		database.Usuarios.Add(CreateUser(hasher.Hash("Senha-Forte-123")));
		await database.SaveChangesAsync();
		var service = CreateLoginService(database, CreateTokenService().Object, hasher);

		var action = () => service.LoginAsync(
			new LoginRequest("gustavo@example.com", "senha-incorreta"),
			CancellationToken.None);

		await action.Should().ThrowAsync<InvalidCredentialsException>();
	}

	[Fact]
	public async Task GetProfileDeveRetornarDadosEContaDoUsuario()
	{
		await using var database = CreateDatabase();
		var usuario = CreateUser("hash");
		database.Usuarios.Add(usuario);
		database.Contas.Add(new Contas { UsuarioId = usuario.Id, Saldo = 125.50m });
		await database.SaveChangesAsync();
		var service = new UserProfileService(database);

		var profile = await service.GetProfileAsync(usuario.Id, CancellationToken.None);

		profile.Should().BeEquivalentTo(new
		{
			Id = usuario.Id,
			Nome = "Gustavo",
			Documento = "12345678900",
			Email = "gustavo@example.com",
			Saldo = 125.50m,
			Ativo = true
		});
	}

	[Fact]
	public async Task ChangePasswordDeveAtualizarHash()
	{
		await using var database = CreateDatabase();
		var hasher = new PasswordHasher();
		var usuario = CreateUser(hasher.Hash("Senha-Forte-123"));
		database.Usuarios.Add(usuario);
		await database.SaveChangesAsync();
		var service = new ChangePasswordService(database, hasher);

		await service.ChangePasswordAsync(
			usuario.Id,
			new ChangePasswordRequest("Senha-Forte-123", "Nova-Senha-456"),
			CancellationToken.None);

		var updated = await database.Usuarios.SingleAsync();
		hasher.Verify("Nova-Senha-456", updated.SenhaHash).Should().BeTrue();
		hasher.Verify("Senha-Forte-123", updated.SenhaHash).Should().BeFalse();
	}

	[Fact]
	public async Task UpdateProfileDeveAtualizarDadosNormalizados()
	{
		await using var database = CreateDatabase();
		var usuario = CreateUser("hash");
		database.Usuarios.Add(usuario);
		database.Contas.Add(new Contas { UsuarioId = usuario.Id, Saldo = 50m });
		await database.SaveChangesAsync();
		var service = new UserProfileService(database);

		var profile = await service.UpdateProfileAsync(
			usuario.Id,
			new UpdateUserRequest(" Gustavo Atualizado ", " 98765432100 ", " GUSTAVO.NOVO@EXAMPLE.COM "),
			CancellationToken.None);

		profile.Nome.Should().Be("Gustavo Atualizado");
		profile.Documento.Should().Be("98765432100");
		profile.Email.Should().Be("gustavo.novo@example.com");
		profile.Saldo.Should().Be(50m);
	}

	[Fact]
	public async Task UpdateProfileDeveRejeitarEmailDuplicado()
	{
		await using var database = CreateDatabase();
		var usuario = CreateUser("hash");
		database.Usuarios.Add(usuario);
		database.Usuarios.Add(new Usuarios
		{
			Nome = "Outro",
			Documento = "98765432100",
			Email = "outro@example.com",
			SenhaHash = "hash",
			Ativo = true
		});
		await database.SaveChangesAsync();
		var service = new UserProfileService(database);

		var action = () => service.UpdateProfileAsync(
			usuario.Id,
			new UpdateUserRequest("Gustavo", "12345678900", "OUTRO@EXAMPLE.COM"),
			CancellationToken.None);

		await action.Should().ThrowAsync<DuplicateUserException>();
	}

	[Fact]
	public async Task DeleteProfileDeveDesativarUsuarioSemRemoverHistorico()
	{
		await using var database = CreateDatabase();
		var usuario = CreateUser("hash");
		database.Usuarios.Add(usuario);
		database.Contas.Add(new Contas { UsuarioId = usuario.Id, Saldo = 0m });
		await database.SaveChangesAsync();
		var service = new UserProfileService(database);

		await service.DeleteProfileAsync(usuario.Id, CancellationToken.None);

		var deletedUser = await database.Usuarios.SingleAsync();
		deletedUser.Ativo.Should().BeFalse();
		(await database.Contas.CountAsync()).Should().Be(1);
	}

	private static RegisterService CreateRegisterService(
		AppDbContext database,
		ITokenService tokenService,
		IPasswordHasher? passwordHasher = null) =>
		new(database, passwordHasher ?? new PasswordHasher(), CreateResponseFactory(tokenService));

	private static LoginService CreateLoginService(
		AppDbContext database,
		ITokenService tokenService,
		IPasswordHasher? passwordHasher = null) =>
		new(database, passwordHasher ?? new PasswordHasher(), CreateResponseFactory(tokenService));

	private static AuthResponseFactory CreateResponseFactory(ITokenService tokenService) =>
		new(tokenService, Options.Create(JwtConfiguration));

	private static Mock<ITokenService> CreateTokenService()
	{
		var tokenService = new Mock<ITokenService>();
		tokenService.Setup(service => service.GenerateToken(It.IsAny<Usuarios>())).Returns("token-gerado");
		return tokenService;
	}

	private static Usuarios CreateUser(string passwordHash) => new()
	{
		Nome = "Gustavo",
		Documento = "12345678900",
		Email = "gustavo@example.com",
		SenhaHash = passwordHash,
		Ativo = true
	};

	private static AppDbContext CreateDatabase()
	{
		var connection = new SqliteConnection("Data Source=:memory:");
		connection.Open();
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseSqlite(connection)
			.Options;
		var database = new AppDbContext(options);
		database.Database.EnsureCreated();
		return database;
	}
}