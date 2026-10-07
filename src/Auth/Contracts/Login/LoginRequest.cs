namespace payment_gateway_API.src.Auth.Contracts.Login;

/// Credenciais recebidas pelo endpoint de login.
public sealed record LoginRequest(
	string Email,
	string Senha);