namespace payment_gateway_API.src.Auth.Configuration;

/// Configurações usadas para emitir e validar tokens JWT.
public sealed class JwtOptions
{
	public const string SectionName = "Jwt";

	public string Key { get; init; } = string.Empty;
	public string Issuer { get; init; } = string.Empty;
	public string Audience { get; init; } = string.Empty;
	public int ExpirationMinutes { get; init; } = 60;

	/// Mantém a validação da configuração em um único ponto para emissão e middleware.
	public bool IsValid() =>
		!string.IsNullOrWhiteSpace(Key) &&
		Key.Length >= 32 &&
		!string.IsNullOrWhiteSpace(Issuer) &&
		!string.IsNullOrWhiteSpace(Audience) &&
		ExpirationMinutes > 0;

	public void EnsureValid()
	{
		if (!IsValid())
		{
			throw new InvalidOperationException(
				"A configuração Jwt é inválida. A chave deve ter pelo menos 32 caracteres e issuer, audience e expiração devem ser informados.");
		}
	}
}