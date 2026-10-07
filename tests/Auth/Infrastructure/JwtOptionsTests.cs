using FluentAssertions;
using payment_gateway_API.src.Auth.Configuration;

namespace payment_gateway_API.Tests.Auth.Infrastructure;

public class JwtOptionsTests
{
	[Fact]
	public void DeveAceitarConfiguracaoCompleta()
	{
		// A configuração válida precisa permitir tanto emissão quanto validação do token.
		var options = new JwtOptions
		{
			Key = "development-only-change-this-key-before-production-123456",
			Issuer = "payment-gateway-api",
			Audience = "payment-gateway-client",
			ExpirationMinutes = 60
		};

		options.IsValid().Should().BeTrue();
	}

	[Fact]
	public void DeveRejeitarChaveCurta()
	{
		// Chaves curtas reduzem a segurança do algoritmo simétrico usado pelo JWT.
		var options = new JwtOptions
		{
			Key = "chave-curta",
			Issuer = "issuer",
			Audience = "audience",
			ExpirationMinutes = 60
		};

		options.IsValid().Should().BeFalse();
	}

	[Fact]
	public void DeveLancarExcecaoParaConfiguracaoInvalida()
	{
		var options = new JwtOptions();

		var action = () => options.EnsureValid();

		action.Should().Throw<InvalidOperationException>();
	}
}