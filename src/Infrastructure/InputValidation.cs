using System.Net.Mail;
using payment_gateway_API.src.Auth.Services;
using payment_gateway_API.src.Features.Financial;

namespace payment_gateway_API.src.Infrastructure;

public static class InputValidation
{
	public static void ValidatePerson(string name, string document, string email)
	{
		if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100 ||
			string.IsNullOrWhiteSpace(document) || document.Trim().Length > 20 ||
			!IsEmail(email))
		{
			throw new InvalidInputException("Nome, documento e email devem ser válidos e respeitar os limites do cadastro.");
		}
	}

	public static void ValidatePassword(string password)
	{
		if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || password.Length > 100)
		{
			throw new InvalidInputException("A senha deve possuir entre 8 e 100 caracteres.");
		}
	}

	public static void ValidateMoney(decimal value, string fieldName)
	{
		if (value <= 0 || decimal.Round(value, 2) != value)
		{
			throw new FinancialValidationException($"{fieldName} deve ser maior que zero e possuir no máximo duas casas decimais.");
		}
	}

	public static void ValidateNonNegativeMoney(decimal value, string fieldName)
	{
		if (value < 0 || decimal.Round(value, 2) != value)
		{
			throw new FinancialValidationException($"{fieldName} não pode ser negativo e deve possuir no máximo duas casas decimais.");
		}
	}

	private static bool IsEmail(string email)
	{
		if (string.IsNullOrWhiteSpace(email) || email.Trim().Length > 100)
		{
			return false;
		}

		try
		{
			var parsed = new MailAddress(email.Trim());
			return parsed.Address == email.Trim();
		}
		catch (FormatException)
		{
			return false;
		}
	}
}
