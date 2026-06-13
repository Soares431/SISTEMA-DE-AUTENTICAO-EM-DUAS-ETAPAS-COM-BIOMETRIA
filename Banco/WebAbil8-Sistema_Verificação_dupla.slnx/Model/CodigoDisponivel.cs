using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Model
{

    [Table("codigoDisponivel")]
    public class CodigoDisponivel
    {
        [Key]
        [Column("codigo", TypeName = "varchar(6)")]
        [MaxLength(6)]
        public string Codigo { get; set; }

        [Column("emUso")]
        public bool EmUso { get; set; }

        [Column("pessoaId")]
        public long? PessoaId { get; set; }

        [ForeignKey("PessoaId")]
        public Pessoa? Pessoa { get; set; }
    }
}

