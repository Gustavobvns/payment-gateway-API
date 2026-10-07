namespace payment_gateway_API.src.Auth.Contracts.Register;

/// Dados públicos necessários para criar um usuário e sua conta digital.
public sealed record RegisterRequest(
	string Nome,
	string Documento,
	string Email,
	string Senha);