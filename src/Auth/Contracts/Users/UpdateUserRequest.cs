namespace payment_gateway_API.src.Auth.Contracts.Users;

public sealed record UpdateUserRequest(
	string Nome,
	string Documento,
	string Email);
