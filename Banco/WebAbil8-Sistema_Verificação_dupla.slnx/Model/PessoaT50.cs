using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Model
{

    [Table("pessoaT50")]
    public class PessoaT50
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("pessoaId")]
        public long PessoaId { get; set; }

        [Column("dispositivoT50Id")]
        public int DispositivoT50Id { get; set; }

        [Column("dataCadastro")]
        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

        [ForeignKey("PessoaId")]
        public Pessoa? Pessoa { get; set; }

        [ForeignKey("DispositivoT50Id")]
        public DispositivoT50? Dispositivo { get; set; }
    }
}

