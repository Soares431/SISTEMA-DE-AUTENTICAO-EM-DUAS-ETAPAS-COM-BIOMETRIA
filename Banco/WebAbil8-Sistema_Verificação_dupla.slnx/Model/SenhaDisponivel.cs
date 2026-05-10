using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Eventing.Reader;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Model
{
    [Table("senhaDisponivel")]
    public class SenhaDisponivel
    {
        [Key]
        [Column("senha", TypeName = "varchar(6)")]
        [MaxLength(6)]
        public string Senha { get; set; } // PK

        [Column("emUso")]
        public bool EmUso { get; set; }

        [Column("pessoaId")]
        public long? PessoaId { get; set; } // NULL se não atribuída

        // Navegação
        [ForeignKey("PessoaId")]
        public Pessoa? Pessoa { get; set; }
    }
}
