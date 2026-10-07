using payment_gateway_API.src.Auth.Contracts.Login;

namespace payment_gateway_API.src.Auth.Login;

public interface ILoginService
{
	Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}