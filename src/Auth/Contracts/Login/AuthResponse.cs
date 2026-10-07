namespace payment_gateway_API.src.Auth.Contracts.Login;

/// Resposta pública devolvida após uma autenticação bem-sucedida.
public sealed record AuthResponse(
	string AccessToken,
	DateTime ExpiresAt);