namespace payment_gateway_API.src.Auth.Infrastructure;

using payment_gateway_API.src.Models; 

public interface ITokenService
{
    string GenerateToken(Usuarios Usuarios);
}