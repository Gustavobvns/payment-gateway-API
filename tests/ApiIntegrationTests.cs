using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Xunit;
using FluentAssertions;

namespace payment_gateway_API.Tests;

public static class IntegrationTestConditions
{
	public static bool DockerEnabled =>
		string.Equals(
			Environment.GetEnvironmentVariable("RUN_POSTGRES_INTEGRATION_TESTS"),
			"true",
			StringComparison.OrdinalIgnoreCase);
}

public sealed class DockerFactAttribute : FactAttribute
{
	public DockerFactAttribute()
	{
		if (!IntegrationTestConditions.DockerEnabled)
		{
			Skip = "Defina RUN_POSTGRES_INTEGRATION_TESTS=true e mantenha o Docker em execução.";
		}
	}
}

public sealed class ApiIntegrationTests : IAsyncLifetime
{
	private readonly PostgreSqlContainer database = new PostgreSqlBuilder("postgres:16-alpine")
		.WithDatabase("payment_db")
		.WithUsername("postgres")
		.WithPassword("postgrespassword")
		.Build();

	private WebApplicationFactory<Program> factory = null!;

	public async Task InitializeAsync()
	{
		await database.StartAsync();
		factory = new WebApplicationFactory<Program>()
			.WithWebHostBuilder(builder =>
			{
				builder.UseEnvironment("Development");
				builder.ConfigureAppConfiguration((_, configuration) =>
				{
					configuration.AddInMemoryCollection(new Dictionary<string, string?>
					{
						["ConnectionStrings:DefaultConnection"] = database.GetConnectionString()
					});
				});
			});
	}

	public async Task DisposeAsync()
	{
		factory.Dispose();
		await database.DisposeAsync();
	}

	[DockerFact]
	public async Task HealthERegistroFuncionamComPostgresReal()
	{
		using var client = factory.CreateClient();

		var health = await client.GetAsync("/health");
		health.StatusCode.Should().Be(HttpStatusCode.OK);

		var response = await client.PostAsJsonAsync("/auth/register", new
		{
			nome = "Integration User",
			documento = "integration-document",
			email = "integration@example.com",
			senha = "Senha-Forte-123"
		});

		response.StatusCode.Should().Be(HttpStatusCode.Created);
	}
}
