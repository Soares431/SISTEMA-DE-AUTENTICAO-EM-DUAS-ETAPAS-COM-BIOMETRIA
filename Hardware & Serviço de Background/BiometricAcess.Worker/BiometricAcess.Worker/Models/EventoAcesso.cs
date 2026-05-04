namespace BiometricAcess.Worker.Models
{
    public class EventoAcesso
    {
        public int PessoaID { get; set; }
        public string TipoVerificacao { get; set; } = string.Empty;
        public bool AcessoLiberado { get; set; }
        public DateTime DataHora { get; set; }
        public string IpDispositivo { get; set; } = string.Empty;
    }
}