using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Model
{
    [Table("camera")]
    public class Camera
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("nome", TypeName = "varchar(100)")]
        [MaxLength(100)]
        public string Nome { get; set; }

        [Column("ambienteId")]
        public int AmbienteId { get; set; }

        [Column("urlRTSP", TypeName = "varchar(255)")]
        [MaxLength(255)]
        public string? UrlRTSP { get; set; }

        [Column("enderecoONVIF", TypeName = "varchar(255)")]
        [MaxLength(255)]
        public string? EnderecoONVIF { get; set; }

        // URL HLS (.m3u8) opcional — usada para exibir o stream ao vivo no navegador.
        // Browsers não falam RTSP nativamente; HLS é o formato suportado universalmente.
        // Convenção MediaMTX: rtsp://host:8554/path → http://host:8888/path/index.m3u8
        [Column("urlHLS", TypeName = "varchar(255)")]
        [MaxLength(255)]
        public string? UrlHLS { get; set; }

        [Required]
        [Column("tipo", TypeName = "varchar(10)")]
        [MaxLength(10)]
        public string Tipo { get; set; } // 'interna' ou 'externa'

        // Navegação
        [ForeignKey("AmbienteId")]
        public Ambiente Ambiente { get; set; }

        [Column("ativa")]
        public bool Ativa { get; set; } = true;
    }
}
