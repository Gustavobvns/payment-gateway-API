namespace payment_gateway_API.src.Auth.CurrentUser;


public interface ICurrentUser
{
	bool IsAuthenticated { get; }
	Guid? UserId { get; }
	string? Email { get; }
	string? Name { get; }
}