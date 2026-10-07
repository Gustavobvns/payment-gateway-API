namespace payment_gateway_API.src.Auth.Contracts.Users;

public sealed record ChangePasswordRequest(
	string SenhaAtual,
	string NovaSenha);