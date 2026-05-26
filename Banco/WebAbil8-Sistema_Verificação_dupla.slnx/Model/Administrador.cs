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

        [Column("cpf", TypeName = "varchar(15)")]
        [MaxLength(15)]
        public string? Cpf { get; set; }

        [Column("email", TypeName = "varchar(150)")]
        [MaxLength(150)]
        public string? Email { get; set; }

        [Column("cargo", TypeName = "varchar(100)")]
        [MaxLength(100)]
        public string? Cargo { get; set; }

        [Column("telefone", TypeName = "varchar(20)")]
        [MaxLength(20)]
        public string? Telefone { get; set; }

        [Column("dataCriacao")]
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    }
}
