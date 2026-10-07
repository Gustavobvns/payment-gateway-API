using payment_gateway_API.src.Auth.Contracts.Users;

namespace payment_gateway_API.src.Auth.Users;

public interface IChangePasswordService
{
	Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken);
}