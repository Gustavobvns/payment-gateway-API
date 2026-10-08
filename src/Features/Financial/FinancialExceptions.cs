namespace payment_gateway_API.src.Features.Financial;

public sealed class FinancialValidationException(string message) : Exception(message);

public sealed class FinancialNotFoundException(string message) : Exception(message);

public sealed class InsufficientBalanceException : Exception;

public sealed class PaymentAlreadyPaidException : Exception;
