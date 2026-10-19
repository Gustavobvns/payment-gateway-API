using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using payment_gateway_API.src.Auth.Configuration;
using payment_gateway_API.src.Auth.Infrastructure;
using payment_gateway_API.src.Models;

namespace payment_gateway_API.Tests.Auth.Infrastructure;

public class JwtTokenGeneratorTests
{
	private static readonly JwtOptions ValidOptions = new()
	{
		Key = "development-only-change-this-key-before-production-123456",
		Issuer = "payment-gateway-api",
		Audience = "payment-gateway-client",
		ExpirationMinutes = 60
	};

	[Fact]
	public void DeveGerarTokenComClaimsDoUsuario()
	{
		var usuario = CreateUser();
		var generator = new JwtTokenGenerator(Options.Create(ValidOptions));

		var tokenString = generator.GenerateToken(usuario);
		var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

		token.Issuer.Should().Be(ValidOptions.Issuer);
		token.Audiences.Should().Contain(ValidOptions.Audience);
		token.Claims.Should().Contain(claim =>
			claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == usuario.Id.ToString());
		token.Claims.Should().Contain(claim => claim.Value == usuario.Email);
	}

	[Fact]
	public void DeveRespeitarTempoDeExpiracao()
	{
		var generator = new JwtTokenGenerator(Options.Create(ValidOptions));
		var token = new JwtSecurityTokenHandler().ReadJwtToken(
			generator.GenerateToken(CreateUser()));

		token.ValidTo.Should().BeAfter(DateTime.UtcNow.AddMinutes(59));
		token.ValidTo.Should().BeBefore(DateTime.UtcNow.AddMinutes(61));
	}

	[Fact]
	public void DeveRejeitarTokenComAssinaturaInvalida()
	{
		var generator = new JwtTokenGenerator(Options.Create(ValidOptions));
		var token = new JwtSecurityTokenHandler().ReadJwtToken(
			generator.GenerateToken(CreateUser()));
		var invalidToken = new JwtSecurityToken(
			token.Issuer,
			token.Audiences.Single(),
			token.Claims,
			token.ValidFrom,
			token.ValidTo,
			new SigningCredentials(
				new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
					"another-development-key-that-is-long-enough-123456")),
				SecurityAlgorithms.HmacSha256));

		var action = () => ValidateToken(new JwtSecurityTokenHandler().WriteToken(invalidToken));

		action.Should().Throw<SecurityTokenInvalidSignatureException>();
	}

	[Fact]
	public void DeveRejeitarTokenExpirado()
	{
		var handler = new JwtSecurityTokenHandler();
		var expiredToken = new JwtSecurityToken(
			ValidOptions.Issuer,
			ValidOptions.Audience,
			[ new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()) ],
			DateTime.UtcNow.AddMinutes(-2),
			DateTime.UtcNow.AddMinutes(-1),
			new SigningCredentials(
				new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ValidOptions.Key)),
				SecurityAlgorithms.HmacSha256));

		var action = () => ValidateToken(handler.WriteToken(expiredToken));

		action.Should().Throw<SecurityTokenExpiredException>();
	}

	[Fact]
	public void DeveRejeitarConfiguracaoJWTInvalida()
	{
		var invalidOptions = new JwtOptions
		{
			Key = "curta",
			Issuer = ValidOptions.Issuer,
			Audience = ValidOptions.Audience,
			ExpirationMinutes = ValidOptions.ExpirationMinutes
		};
		var generator = new JwtTokenGenerator(Options.Create(invalidOptions));

		var action = () => generator.GenerateToken(CreateUser());

		action.Should().Throw<InvalidOperationException>();
	}

	private static Usuarios CreateUser() => new()
	{
		Nome = "Gustavo",
		Documento = "12345678900",
		Email = "gustavo@example.com",
		SenhaHash = "hash",
		Ativo = true
	};

	private static void ValidateToken(string token)
	{
		new JwtSecurityTokenHandler().ValidateToken(
			token,
			new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(
					Encoding.UTF8.GetBytes(ValidOptions.Key)),
				ValidateIssuer = true,
				ValidIssuer = ValidOptions.Issuer,
				ValidateAudience = true,
				ValidAudience = ValidOptions.Audience,
				ValidateLifetime = true,
				ClockSkew = TimeSpan.Zero
			},
			out _);
	}
}