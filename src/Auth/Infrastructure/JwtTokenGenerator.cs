using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using payment_gateway_API.src.Auth.Configuration;
using payment_gateway_API.src.Models;

namespace payment_gateway_API.src.Auth.Infrastructure;

public sealed class JwtTokenGenerator : ITokenService
{
	private readonly JwtOptions options;

	public JwtTokenGenerator(IOptions<JwtOptions> options)
	{
		this.options = options.Value;
	}

	public string GenerateToken(Usuarios usuario)
	{
		options.EnsureValid();

		var claims = new[]
		{
			new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
			new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
			new Claim(ClaimTypes.Name, usuario.Nome),
			new Claim(ClaimTypes.Email, usuario.Email)
		};

		var credentials = new SigningCredentials(
			new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
			SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: options.Issuer,
			audience: options.Audience,
			claims: claims,
			expires: DateTime.UtcNow.AddMinutes(options.ExpirationMinutes),
			signingCredentials: credentials);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}
