namespace payment_Gateway_API.Models;

public class CodigosPagamento
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string CodigoPagamentoHash { get; set; }
    public Guid ContaGeradoraId { get; set; }
    public decimal ValorOriginal { get; set; }
    public DateOnly DataVencimento { get; set; }
    public decimal JurosDiario { get; set; }
    public bool Status { get; set; }
    public Contas ContaGeradora { get; set; } = null!;
}