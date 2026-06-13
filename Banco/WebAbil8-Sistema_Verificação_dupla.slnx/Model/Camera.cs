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

        [Column("urlHLS", TypeName = "varchar(255)")]
        [MaxLength(255)]
        public string? UrlHLS { get; set; }

        [Required]
        [Column("tipo", TypeName = "varchar(10)")]
        [MaxLength(10)]
        public string Tipo { get; set; }

        [ForeignKey("AmbienteId")]
        public Ambiente Ambiente { get; set; }

        [Column("ativa")]
        public bool Ativa { get; set; } = true;
    }
}

