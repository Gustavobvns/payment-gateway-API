namespace payment_gateway_API.src.Models;
public class Contas
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid UsuarioId { get; set; }
    public decimal Saldo { get; set; }
    public Usuarios Usuario { get; set; } = null!;
}