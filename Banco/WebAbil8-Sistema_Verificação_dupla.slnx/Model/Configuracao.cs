using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Model
{
    [Table("configuracao")]
    public class Configuracao
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("retencaoGravacoesTentativasDias")]
        public int RetencaoGravacoesTentativasDias { get; set; } = 90;

        [Column("retencaoLogsDias")]
        public int RetencaoLogsDias { get; set; } = 180;

        [Column("tempoEsperaGravacaoSeg")]
        public int TempoEsperaGravacaoSeg { get; set; } = 60;

        [Column("periodoInativacaoMeses")]
        public int PeriodoInativacaoMeses { get; set; } = 24;
    }
}

