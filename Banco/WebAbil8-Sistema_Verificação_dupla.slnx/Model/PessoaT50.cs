using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Model
{
    // Indica que uma pessoa está com biometria cadastrada em um T50 específico.
    // Permite que: (a) ambiente com vários T50 escolha em quais a pessoa fica cadastrada,
    // (b) o mesmo T50 sirva múltiplos ambientes carregando pessoas de cada um,
    // (c) DigitaisCadastradas do T50 = COUNT desta tabela onde dispositivoT50Id = X.
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
