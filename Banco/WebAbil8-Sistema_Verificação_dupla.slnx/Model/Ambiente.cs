using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Model
{
    [Table("ambiente")]
    public class Ambiente
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("nome", TypeName = "varchar(100)")]
        [MaxLength(100)]
        public string Nome { get; set; }

        [Column("dispositivoT50Id")]
        public int DispositivoT50Id { get; set; }

        [Column("tempoEsperaGravacaoSeg")]
        public int TempoEsperaGravacaoSeg { get; set; } = 60; // padrão 60

        [Column("dataCriacao")]
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    }
}
