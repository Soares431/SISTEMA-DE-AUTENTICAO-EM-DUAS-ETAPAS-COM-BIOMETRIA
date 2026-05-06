using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Model
{
    [Table("logAdmin")]
    public class LogAdmin
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Column("adminId")]
        public int AdminId { get; set; }

        [Required]
        [Column("acao", TypeName = "varchar(100)")]
        [MaxLength(100)]
        public string Acao { get; set; }

        [Required]
        [Column("entidadeAfetada", TypeName = "varchar(50)")]
        [MaxLength(50)]
        public string EntidadeAfetada { get; set; }

        [Column("entidadeId")]
        public int? EntidadeId { get; set; }

        [Column("dataHora")]
        public DateTime DataHora { get; set; } = DateTime.UtcNow;

        [Column("dataExpiracao")]
        public DateTime? DataExpiracao { get; set; }

        // Navegação
        [ForeignKey("AdminId")]
        public Administrador Administrador { get; set; }
    }
}
