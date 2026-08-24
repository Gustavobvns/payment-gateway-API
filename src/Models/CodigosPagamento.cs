public class CodigosPagamento
{
    public int Id { get; set; }
    public string CodigoPagamentoHash { get; set; }
    public int ContaGeradoraId { get; set; }
    public decimal ValorOriginal { get; set; }
    public DateOnly DataVencimento { get; set; }
    public decimal JurosDiario { get; set; }
    public bool Status { get; set; }
}