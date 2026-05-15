namespace InfraestruturaBloco1.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string Admin { get; set; } = string.Empty;
        public string Acao { get; set; } = string.Empty;
        public string Entidade { get; set; } = string.Empty;
        public DateTime DataHora { get; set; } = DateTime.UtcNow;
    }
}
