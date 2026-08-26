namespace payment_Gateway_API.Models;

public class Transacoes
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ContaOrigemId { get; set; }
    public Guid ContaDestinoId { get; set; }
    public decimal Valor { get; set; }
    public Guid? CodigoPagamentoId { get; set; }
    public DateTime DataTransacao { get; set; }
    public Contas ContaOrigem { get; set; } = null!;
    public Contas ContaDestino { get; set; } = null!;
    public CodigosPagamento? CodigoPagamento { get; set; }
}