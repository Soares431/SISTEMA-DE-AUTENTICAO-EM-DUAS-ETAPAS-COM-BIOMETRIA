using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebAbil8_Sistema_Verificação_dupla.slnx.Model
{
    [Table("administrador")]
    public class Administrador
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("login", TypeName = "varchar(50)")]
        [MaxLength(50)]
        public string Login { get; set; }

        [Required]
        [Column("senhaHash", TypeName = "varchar(255)")]
        [MaxLength(255)]
        public string SenhaHash { get; set; } // BCrypt — nunca texto claro

        [Required]
        [Column("nomeCompleto", TypeName = "varchar(100)")]
        [MaxLength(100)]
        public string NomeCompleto { get; set; }

        [Column("dataCriacao")]
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    }
}
