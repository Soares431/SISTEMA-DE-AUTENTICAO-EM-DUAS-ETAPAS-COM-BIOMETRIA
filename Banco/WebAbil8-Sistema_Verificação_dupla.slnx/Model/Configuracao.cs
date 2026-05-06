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
        public int RetencaoGravacoesTentativasDias { get; set; } = 90; // padrão 90, min 30, max 180

        [Column("retencaoLogsDias")]
        public int RetencaoLogsDias { get; set; } = 180; // padrão 180, min 90, max 365

        [Column("tempoEsperaGravacaoSeg")]
        public int TempoEsperaGravacaoSeg { get; set; } = 60; // padrão 60, min 30, max 120
    }
}
