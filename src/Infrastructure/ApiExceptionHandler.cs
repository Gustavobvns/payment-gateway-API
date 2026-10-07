using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using payment_gateway_API.src.Auth.Services;

namespace payment_gateway_API.src.Infrastructure;

public sealed class ApiExceptionHandler(
	IProblemDetailsService problemDetailsService) : IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(
		HttpContext httpContext,
		Exception exception,
		CancellationToken cancellationToken)
	{
		var (statusCode, title, detail) = exception switch
		{
			AuthValidationException =>
				(StatusCodes.Status400BadRequest, "Requisição inválida", exception.Message),
			DuplicateUserException =>
				(StatusCodes.Status409Conflict, "Conflito", exception.Message),
			InvalidCredentialsException =>
				(StatusCodes.Status401Unauthorized, "Não autorizado", "As credenciais informadas são inválidas."),
			UserNotFoundException =>
				(StatusCodes.Status404NotFound, "Usuário não encontrado", "O usuário informado não existe."),
			_ =>
				(StatusCodes.Status500InternalServerError, "Erro interno", "Ocorreu um erro inesperado."),
		};

		httpContext.Response.StatusCode = statusCode;
		await problemDetailsService.WriteAsync(new ProblemDetailsContext
		{
			HttpContext = httpContext,
			Exception = exception,
			ProblemDetails = new ProblemDetails
			{
				Status = statusCode,
				Title = title,
				Detail = detail,
				Instance = httpContext.Request.Path
			}
		});

		return true;
	}
}