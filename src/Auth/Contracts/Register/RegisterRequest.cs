namespace payment_gateway_API.src.Auth.Contracts.Register;

public sealed record RegisterRequest(
	string Nome,
	string Documento,
	string Email,
	string Senha);