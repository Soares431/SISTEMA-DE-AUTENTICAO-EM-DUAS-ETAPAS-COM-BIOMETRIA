using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Model
{
    // Fila de slots do AS608 cuja pessoa-dona já foi DELETADA do banco.
    // Pessoa.SlotAs608ParaApagar não serve nesse caso (some junto com a pessoa).
    // O SincronizadorAs608Worker drena também esta tabela.
    [Table("SlotAs608Orfao")]
    public class SlotAs608Orfao
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("slot")]
        public int Slot { get; set; }

        [Required]
        [Column("criadoEm")]
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
