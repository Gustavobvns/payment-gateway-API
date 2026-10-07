using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using payment_gateway_API.src.Auth.Contracts.Users;
using payment_gateway_API.src.Auth.CurrentUser;

namespace payment_gateway_API.src.Auth.Users;


public static class UserEndpoints
{
	public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/users").RequireAuthorization();
		group.MapGet("/me", GetProfileAsync)
			.WithName("GetCurrentUser")
			.WithSummary("Consulta o perfil do usuário autenticado")
			.Produces<UserProfileResponse>(StatusCodes.Status200OK)
			.Produces(StatusCodes.Status401Unauthorized)
			.Produces(StatusCodes.Status404NotFound);
		group.MapPut("/me", UpdateProfileAsync)
			.WithName("UpdateCurrentUser")
			.WithSummary("Atualiza o perfil do usuário autenticado")
			.Produces<UserProfileResponse>(StatusCodes.Status200OK)
			.Produces(StatusCodes.Status400BadRequest)
			.Produces(StatusCodes.Status401Unauthorized)
			.Produces(StatusCodes.Status404NotFound)
			.Produces(StatusCodes.Status409Conflict);
		group.MapPatch("/change-password", ChangePasswordAsync)
			.WithName("ChangeCurrentUserPassword")
			.WithSummary("Altera a senha do usuário autenticado")
			.Produces(StatusCodes.Status204NoContent)
			.Produces(StatusCodes.Status400BadRequest)
			.Produces(StatusCodes.Status401Unauthorized)
			.Produces(StatusCodes.Status404NotFound);
		group.MapDelete("/me", DeleteProfileAsync)
			.WithName("DeleteCurrentUser")
			.WithSummary("Desativa o usuário autenticado")
			.Produces(StatusCodes.Status204NoContent)
			.Produces(StatusCodes.Status401Unauthorized)
			.Produces(StatusCodes.Status404NotFound);
		return endpoints;
	}

	private static async Task<IResult> GetProfileAsync(
		ICurrentUser currentUser,
		IUserProfileService userProfileService,
		CancellationToken cancellationToken)
	{
		if (currentUser.UserId is not Guid userId)
		{
			return Results.Unauthorized();
		}

		var profile = await userProfileService.GetProfileAsync(userId, cancellationToken);
		return profile is null ? Results.NotFound() : Results.Ok(profile);
	}

	private static async Task<IResult> UpdateProfileAsync(
		UpdateUserRequest request,
		ICurrentUser currentUser,
		IUserProfileService userProfileService,
		CancellationToken cancellationToken)
	{
		if (currentUser.UserId is not Guid userId)
		{
			return Results.Unauthorized();
		}

		var profile = await userProfileService.UpdateProfileAsync(userId, request, cancellationToken);
		return Results.Ok(profile);
	}

	private static async Task<IResult> ChangePasswordAsync(
		ChangePasswordRequest request,
		ICurrentUser currentUser,
		IChangePasswordService changePasswordService,
		CancellationToken cancellationToken)
	{
		if (currentUser.UserId is not Guid userId)
		{
			return Results.Unauthorized();
		}

		await changePasswordService.ChangePasswordAsync(userId, request, cancellationToken);
		return Results.NoContent();
	}

	private static async Task<IResult> DeleteProfileAsync(
		ICurrentUser currentUser,
		IUserProfileService userProfileService,
		CancellationToken cancellationToken)
	{
		if (currentUser.UserId is not Guid userId)
		{
			return Results.Unauthorized();
		}

		await userProfileService.DeleteProfileAsync(userId, cancellationToken);
		return Results.NoContent();
	}
}