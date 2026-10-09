namespace payment_gateway_API.src.Auth.Services;

public sealed class AuthValidationException(string message) : Exception(message);

public sealed class DuplicateUserException(string field) : Exception($"{field} indisponível.");

public sealed class InvalidCredentialsException : Exception;

public sealed class UserNotFoundException : Exception;

public sealed class InvalidInputException(string message) : Exception(message);