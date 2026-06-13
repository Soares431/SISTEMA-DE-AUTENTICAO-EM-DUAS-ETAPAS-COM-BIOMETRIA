using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Model
{

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

        [Column("ehPrincipal")]
        public bool EhPrincipal { get; set; } = false;

        [ForeignKey("AmbienteId")]
        public Ambiente? Ambiente { get; set; }

        [ForeignKey("DispositivoT50Id")]
        public DispositivoT50? Dispositivo { get; set; }
    }
}

