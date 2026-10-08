namespace payment_gateway_API.src.Auth.Contracts.Login;

public sealed record AuthResponse(
	string AccessToken,
	DateTime ExpiresAt);