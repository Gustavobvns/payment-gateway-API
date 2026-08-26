namespace payment_Gateway_API.Models;

public class Usuarios
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Nome { get; set; }
    public string Documento { get; set; }
    public string Email { get; set; }
    public string SenhaHash { get; set; }
    public bool Ativo { get; set; }
}