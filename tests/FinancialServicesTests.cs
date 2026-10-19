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
	public async Task TransferenciaComSaldoInsuficienteNaoDeveAlterarAsContas()
	{
		await using var database = CreateDatabase();
		var source = CreateUser("source-insufficient@example.com");
		var destination = CreateUser("destination-insufficient@example.com");
		database.Usuarios.AddRange(source.User, destination.User);
		database.Contas.AddRange(source.Account, destination.Account);
		source.Account.Saldo = 20m;
		await database.SaveChangesAsync();

		var action = () => new TransferService(database).TransferAsync(
			source.User.Id,
			destination.Account.Id,
			new TransferRequest(35m),
			"insufficient-key",
			CancellationToken.None);

		await action.Should().ThrowAsync<InsufficientBalanceException>();
		source.Account.Saldo.Should().Be(20m);
		destination.Account.Saldo.Should().Be(0m);
		(await database.Transacoes.CountAsync()).Should().Be(0);
	}

	[Fact]
	public async Task TransferRepetidaComMesmaChaveDeveRetornarResultadoOriginalSemNovoLancamento()
	{
		await using var database = CreateDatabase();
		var source = CreateUser("source-idempotency@example.com");
		var destination = CreateUser("destination-idempotency@example.com");
		database.Usuarios.AddRange(source.User, destination.User);
		database.Contas.AddRange(source.Account, destination.Account);
		source.Account.Saldo = 100m;
		await database.SaveChangesAsync();

		var service = new TransferService(database);
		var request = new TransferRequest(35m);
		var first = await service.TransferAsync(
			source.User.Id, destination.Account.Id, request, "transfer-key", CancellationToken.None);
		var repeated = await service.TransferAsync(
			source.User.Id, destination.Account.Id, request, "transfer-key", CancellationToken.None);

		repeated.Should().Be(first);
		source.Account.Saldo.Should().Be(65m);
		(await database.Transacoes.CountAsync()).Should().Be(1);
	}

	[Fact]
	public async Task ChaveDeIdempotenciaNaoPodeSerReutilizadaComOutraTransferencia()
	{
		await using var database = CreateDatabase();
		var source = CreateUser("source-conflict@example.com");
		var destination = CreateUser("destination-conflict@example.com");
		database.Usuarios.AddRange(source.User, destination.User);
		database.Contas.AddRange(source.Account, destination.Account);
		source.Account.Saldo = 100m;
		await database.SaveChangesAsync();

		var service = new TransferService(database);
		await service.TransferAsync(
			source.User.Id,
			destination.Account.Id,
			new TransferRequest(35m),
			"transfer-key",
			CancellationToken.None);

		var action = () => service.TransferAsync(
			source.User.Id,
			destination.Account.Id,
			new TransferRequest(40m),
			"transfer-key",
			CancellationToken.None);

		await action.Should().ThrowAsync<FinancialValidationException>();
		source.Account.Saldo.Should().Be(65m);
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
	public async Task PagamentoRepetidoComMesmaChaveDeveRetornarResultadoOriginal()
	{
		await using var database = CreateDatabase();
		var receiver = CreateUser("receiver-retry@example.com");
		var payer = CreateUser("payer-retry@example.com");
		database.Usuarios.AddRange(receiver.User, payer.User);
		database.Contas.AddRange(receiver.Account, payer.Account);
		payer.Account.Saldo = 100m;
		await database.SaveChangesAsync();
		var service = new PaymentService(database);
		var payment = await service.CreateAsync(
			receiver.User.Id,
			new CreatePaymentRequest(
				receiver.Account.Id,
				20m,
				DateOnly.FromDateTime(DateTime.UtcNow),
				0m),
			"create-key",
			CancellationToken.None);

		var first = await service.PayAsync(payer.User.Id, payment.Codigo, "pay-key", CancellationToken.None);
		var repeated = await service.PayAsync(payer.User.Id, payment.Codigo, "pay-key", CancellationToken.None);

		repeated.Should().Be(first);
		payer.Account.Saldo.Should().Be(80m);
		(await database.Transacoes.CountAsync()).Should().Be(1);
	}

	[Fact]
	public async Task ConsultaDeCobrancaPagaDeveManterOValorLiquidado()
	{
		await using var database = CreateDatabase();
		var receiver = CreateUser("receiver-settled@example.com");
		var payer = CreateUser("payer-settled@example.com");
		database.Usuarios.AddRange(receiver.User, payer.User);
		database.Contas.AddRange(receiver.Account, payer.Account);
		payer.Account.Saldo = 100m;
		await database.SaveChangesAsync();
		var service = new PaymentService(database);
		var payment = await service.CreateAsync(
			receiver.User.Id,
			new CreatePaymentRequest(
				receiver.Account.Id,
				20m,
				DateOnly.FromDateTime(DateTime.UtcNow),
				1m),
			"create-settled-key",
			CancellationToken.None);

		var paymentEntity = await database.CodigosPagamento.SingleAsync();
		paymentEntity.DataVencimento = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
		await database.SaveChangesAsync();

		var paid = await service.PayAsync(
			payer.User.Id, payment.Codigo, "pay-settled-key", CancellationToken.None);

		paymentEntity.DataVencimento = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30);
		await database.SaveChangesAsync();

		var queried = await service.GetAsync(payment.Codigo, CancellationToken.None);

		paid.ValorPago.Should().Be(21m);
		queried.ValorAtualizado.Should().Be(21m);
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
