using FluentAssertions;
using payment_gateway_API.src.Auth.Infrastructure;

namespace payment_gateway_API.Tests.Auth.Infrastructure;

public class PasswordHasherTests
{
	[Fact]
	public void DeveGerarHashDiferenteDaSenhaOriginal()
	{
		var hasher = new PasswordHasher();

		var hash = hasher.Hash("Senha-Forte-123");

		hash.Should().NotBe("Senha-Forte-123");
		hash.Should().NotBeNullOrWhiteSpace();
	}

	[Fact]
	public void DeveValidarSenhaCorreta()
	{
		var hasher = new PasswordHasher();
		var hash = hasher.Hash("Senha-Forte-123");

		hasher.Verify("Senha-Forte-123", hash).Should().BeTrue();
	}

	[Fact]
	public void DeveRejeitarSenhaIncorreta()
	{
		var hasher = new PasswordHasher();
		var hash = hasher.Hash("Senha-Forte-123");

		hasher.Verify("Senha-Diferente-456", hash).Should().BeFalse();
	}
}