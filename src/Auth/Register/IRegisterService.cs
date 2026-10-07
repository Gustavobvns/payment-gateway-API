using payment_gateway_API.src.Auth.Contracts.Login;
using payment_gateway_API.src.Auth.Contracts.Register;

namespace payment_gateway_API.src.Auth.Register;

public interface IRegisterService
{
	Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
}