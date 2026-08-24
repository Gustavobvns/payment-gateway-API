public class Transacoes
{
    public int Id { get; set; }
    public int ContaOrigemId { get; set; }
    public int ContaDestinoId { get; set; }
    public decimal Valor { get; set; }
    public string CodigoPagamentoId { get; set; }
    public DateTime DataTransacao { get; set; }
}