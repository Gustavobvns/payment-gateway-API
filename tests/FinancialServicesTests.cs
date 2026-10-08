using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using payment_gateway_API.src.Data;
using payment_gateway_API.src.Features.Financial;
using payment_gateway_API.src.Features.Ledger;
using payment_gateway_API.src.Features.Payments;
using payment_gateway_API.src.Features.Transfers;
using payment_gateway_API.src.Models;

namespace payment_gateway_API.Tests;

public sealed class FinancialServicesTests
{
	[Fact]
	public async Task TransferDeveDebitarCreditarERegistrarLancamento()
	{
		await using var database = CreateDatabase();
		var source = CreateUser("source@example.com");
		var destination = CreateUser("destination@example.com");
		database.Usuarios.AddRange(source.User, destination.User);
		database.Contas.AddRange(source.Account, destination.Account);
		source.Account.Saldo = 100m;
		await database.SaveChangesAsync();

		var result = await new TransferService(database).TransferAsync(
			source.User.Id,
			destination.Account.Id,
			new TransferRequest(35m),
			CancellationToken.None);

		source.Account.Saldo.Should().Be(65m);
		destination.Account.Saldo.Should().Be(35m);
		result.Valor.Should().Be(35m);
		(await database.Transacoes.CountAsync()).Should().Be(1);
	}

	[Fact]
	public async Task PaymentDeveAplicarJurosEImpedirPagamentoDuplicado()
	{
		await using var database = CreateDatabase();
		var receiver = CreateUser("receiver@example.com");
		var payer = CreateUser("payer@example.com");
		database.Usuarios.AddRange(receiver.User, payer.User);
		database.Contas.AddRange(receiver.Account, payer.Account);
		payer.Account.Saldo = 100m;
		await database.SaveChangesAsync();
		var service = new PaymentService(database);

		var created = await service.CreateAsync(
			receiver.User.Id,
			new CreatePaymentRequest(
				receiver.Account.Id,
				20m,
				DateOnly.FromDateTime(DateTime.UtcNow),
				0m),
			CancellationToken.None);

		var paid = await service.PayAsync(payer.User.Id, created.Codigo, CancellationToken.None);

		paid.ValorPago.Should().Be(20m);
		payer.Account.Saldo.Should().Be(80m);
		receiver.Account.Saldo.Should().Be(20m);
		var action = () => service.PayAsync(payer.User.Id, created.Codigo, CancellationToken.None);
		await action.Should().ThrowAsync<PaymentAlreadyPaidException>();
	}

	[Fact]
	public async Task LedgerDeveRetornarEntradaEOrdenarLancamentos()
	{
		await using var database = CreateDatabase();
		var source = CreateUser("source@example.com");
		var destination = CreateUser("destination@example.com");
		database.Usuarios.AddRange(source.User, destination.User);
		database.Contas.AddRange(source.Account, destination.Account);
		source.Account.Saldo = 100m;
		await database.SaveChangesAsync();
		await new TransferService(database).TransferAsync(
			source.User.Id,
			destination.Account.Id,
			new TransferRequest(15m),
			CancellationToken.None);

		var entries = await new LedgerService(database).GetAsync(
			destination.User.Id,
			1,
			20,
			CancellationToken.None);

		entries.Should().ContainSingle();
		entries[0].Tipo.Should().Be("Entrada");
		entries[0].Valor.Should().Be(15m);
	}

	private static (Usuarios User, Contas Account) CreateUser(string email)
	{
		var user = new Usuarios
		{
			Nome = email,
			Documento = Guid.NewGuid().ToString(),
			Email = email,
			SenhaHash = "hash",
			Ativo = true
		};
		return (user, new Contas { UsuarioId = user.Id });
	}

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
