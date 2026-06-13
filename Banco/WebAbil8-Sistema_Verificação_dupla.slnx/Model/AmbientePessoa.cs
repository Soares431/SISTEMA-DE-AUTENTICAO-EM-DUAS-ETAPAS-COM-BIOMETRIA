using System.ComponentModel.DataAnnotations.Schema;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Model
{
    [Table("ambiente_pessoa")]
    public class AmbientePessoa
    {
        [Column("ambienteId")]
        public int AmbienteId { get; set; }

        [Column("pessoaId")]
        public long PessoaId { get; set; }

        [Column("dataAdicionado")]
        public DateTime DataAdicionado { get; set; } = DateTime.UtcNow;

        [ForeignKey("AmbienteId")]
        public Ambiente Ambiente { get; set; }

        [ForeignKey("PessoaId")]
        public Pessoa Pessoa{ get; set; }
    }
}

