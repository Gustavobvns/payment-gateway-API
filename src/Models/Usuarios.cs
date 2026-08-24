public class Usuarios
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Documento { get; set; }
    public string Email { get; set; }
    public string SenhaHash { get; set; }
    public bool Ativo { get; set; }
}