using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Model
{
    // Tabela de junção N-N entre Ambiente e DispositivoT50.
    // Um ambiente pode ter mais de um T50 (ex: duas portas diferentes), e um T50
    // não compartilha entre ambientes (mas a tabela suporta isso se precisar).
    // Mantém ambiente.dispositivoT50Id como o "principal" para backward compat —
    // após a migração, todo ambiente tem pelo menos uma linha aqui com ehPrincipal=1.
    [Table("ambienteT50")]
    public class AmbienteT50
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("ambienteId")]
        public int AmbienteId { get; set; }

        [Column("dispositivoT50Id")]
        public int DispositivoT50Id { get; set; }

        [Column("dataVinculo")]
        public DateTime DataVinculo { get; set; } = DateTime.UtcNow;

        // Marca qual T50 é o "principal" do ambiente — usado como dispositivo de
        // referência para contador de digitais quando admin adiciona pessoa.
        [Column("ehPrincipal")]
        public bool EhPrincipal { get; set; } = false;

        [ForeignKey("AmbienteId")]
        public Ambiente? Ambiente { get; set; }

        [ForeignKey("DispositivoT50Id")]
        public DispositivoT50? Dispositivo { get; set; }
    }
}
