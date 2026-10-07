namespace payment_gateway_API.src.Auth.Infrastructure;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}