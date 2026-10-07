using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using payment_gateway_API.src.Auth.CurrentUser;
using CurrentUserService = payment_gateway_API.src.Auth.CurrentUser.CurrentUser;

namespace payment_gateway_API.Tests.Auth.CurrentUser;

public class CurrentUserTests
{
	[Fact]
	public void DeveLerIdentidadeDosClaimsValidados()
	{
		var userId = Guid.CreateVersion7();
		var context = new DefaultHttpContext
		{
			User = new ClaimsPrincipal(new ClaimsIdentity(
			[
				new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
				new Claim(ClaimTypes.Email, "gustavo@example.com"),
				new Claim(ClaimTypes.Name, "Gustavo")
			],
			"TestAuthentication"))
		};
		var currentUser = new CurrentUserService(new HttpContextAccessor { HttpContext = context });

		currentUser.IsAuthenticated.Should().BeTrue();
		currentUser.UserId.Should().Be(userId);
		currentUser.Email.Should().Be("gustavo@example.com");
		currentUser.Name.Should().Be("Gustavo");
	}

	[Fact]
	public void DeveRepresentarContextoSemAutenticacao()
	{
		var currentUser = new CurrentUserService(new HttpContextAccessor
		{
			HttpContext = new DefaultHttpContext()
		});

		currentUser.IsAuthenticated.Should().BeFalse();
		currentUser.UserId.Should().BeNull();
		currentUser.Email.Should().BeNull();
		currentUser.Name.Should().BeNull();
	}

	[Fact]
	public void DeveIgnorarClaimDeIdComFormatoInvalido()
	{
		var context = new DefaultHttpContext
		{
			User = new ClaimsPrincipal(new ClaimsIdentity(
				[new Claim(ClaimTypes.NameIdentifier, "id-invalido")],
				"TestAuthentication"))
		};
		var currentUser = new CurrentUserService(new HttpContextAccessor { HttpContext = context });

		currentUser.UserId.Should().BeNull();
	}
}