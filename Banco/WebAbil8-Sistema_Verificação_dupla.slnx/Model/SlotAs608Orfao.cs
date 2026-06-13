using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Model
{

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

