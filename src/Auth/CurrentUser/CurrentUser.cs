using System.Security.Claims;

namespace payment_gateway_API.src.Auth.CurrentUser;

/// Adapta os claims já validados pelo middleware JWT para a aplicação.
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
	private ClaimsPrincipal Principal =>
		httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

	public bool IsAuthenticated =>
		Principal.Identity?.IsAuthenticated == true;

	public Guid? UserId
	{
		get
		{
			var claim = Principal.FindFirstValue(ClaimTypes.NameIdentifier);
			return Guid.TryParse(claim, out var userId) ? userId : null;
		}
	}

	public string? Email => Principal.FindFirstValue(ClaimTypes.Email);

	public string? Name => Principal.FindFirstValue(ClaimTypes.Name);
}