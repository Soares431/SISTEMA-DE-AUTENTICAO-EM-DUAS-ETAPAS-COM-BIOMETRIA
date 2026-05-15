namespace InfraestruturaBloco1.Models
{
    public class Camera
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Ip { get; set; } = string.Empty;
        public string RtspUrl { get; set; } = string.Empty;
        public bool Ativa { get; set; } = true;
    }
}
