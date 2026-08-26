namespace payment_Gateway_API.Models;

public class Contas
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid UsuarioId {get; set;}
    public decimal Saldo {get; set;}
}