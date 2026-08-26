namespace payment_Gateway_API.Models;

public class Usuarios
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Nome { get; set; }
    public required string Documento { get; set; }
    public required string Email { get; set; }
    public required string SenhaHash { get; set; }
    public bool Ativo { get; set; }
}