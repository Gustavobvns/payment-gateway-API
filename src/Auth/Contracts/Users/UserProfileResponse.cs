namespace payment_gateway_API.src.Auth.Contracts.Users;


public sealed record UserProfileResponse(
	Guid Id,
	string Nome,
	string Documento,
	string Email,
	decimal Saldo,
	bool Ativo);