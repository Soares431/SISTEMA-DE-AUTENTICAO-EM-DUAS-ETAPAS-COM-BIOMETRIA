using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Model
{
    [Table("tentativaAcesso")]
    public class TentativaAcesso
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("pessoaId")]
        public int? PessoaId { get; set; } // NULL se não cadastrada

        [Column("ambienteId")]
        public int AmbienteId { get; set; }

        [Column("dataHora")]
        public DateTime DataHora { get; set; } = DateTime.UtcNow;

        [Column("acessoLiberado")]
        public bool AcessoLiberado { get; set; }

        [Column("motivoNegacao", TypeName = "varchar(50)")]
        [MaxLength(50)]
        public string? MotivoNegacao { get; set; }

        [Column("tipoVerificacao", TypeName = "varchar(15)")]
        [MaxLength(15)]
        public string? TipoVerificacao { get; set; } // 'digital_id' ou 'senha_id'

        [Column("gravacaoPath", TypeName = "varchar(255)")]
        [MaxLength(255)]
        public string? GravacaoPath { get; set; }

        [Column("dataExpiracao")]
        public DateTime? DataExpiracao { get; set; }

        // Navegação
        [ForeignKey("PessoaId")]
        public long? Pessoa { get; set; }

        [ForeignKey("AmbienteId")]
        public Ambiente Ambiente { get; set; }
    }
}
