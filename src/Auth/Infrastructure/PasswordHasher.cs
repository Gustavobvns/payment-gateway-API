namespace payment_gateway_API.src.Auth.Infrastructure;

using payment_gateway_API.src.Auth.Infrastructure;
using BCrypt.Net;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return BCrypt.HashPassword(password);
    }

    public bool Verify(string password, string passwordHash)
    {
        return BCrypt.Verify(password, passwordHash);
    }
}