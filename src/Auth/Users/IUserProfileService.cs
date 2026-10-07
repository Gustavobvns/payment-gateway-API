using payment_gateway_API.src.Auth.Contracts.Users;

namespace payment_gateway_API.src.Auth.Users;

public interface IUserProfileService
{
	Task<UserProfileResponse?> GetProfileAsync(Guid userId, CancellationToken cancellationToken);
	Task<UserProfileResponse> UpdateProfileAsync(
		Guid userId,
		UpdateUserRequest request,
		CancellationToken cancellationToken);
	Task DeleteProfileAsync(Guid userId, CancellationToken cancellationToken);
}