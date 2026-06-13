using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Model
{
    [Table("senhaDisponivel")]
    public class SenhaDisponivel
    {
        [Key]
        [Column("senha", TypeName = "varchar(6)")]
        [MaxLength(6)]
        public string Senha { get; set; }

        [Column("emUso")]
        public bool EmUso { get; set; }

        [Column("pessoaId")]
        public long? PessoaId { get; set; }

        [ForeignKey("PessoaId")]
        public Pessoa? Pessoa { get; set; }
    }
}

