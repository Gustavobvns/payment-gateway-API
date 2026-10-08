namespace payment_gateway_API.src.Auth.Contracts.Login;

public sealed record LoginRequest(
	string Email,
	string Senha);