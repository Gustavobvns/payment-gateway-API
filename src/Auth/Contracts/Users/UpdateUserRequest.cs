namespace payment_gateway_API.src.Auth.Contracts.Users;

/// <summary>Dados editáveis do perfil do usuário autenticado.</summary>
public sealed record UpdateUserRequest(
	string Nome,
	string Documento,
	string Email);
