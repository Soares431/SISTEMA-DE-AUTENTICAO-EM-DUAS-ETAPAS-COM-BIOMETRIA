using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Model
{
    [Table("Pessoa")]
    [Index(nameof(Cpf), IsUnique = true)]
    [Index(nameof(CodigoUsuario), IsUnique = true)]
    public class Pessoa
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        // Código de 6 dígitos (100000-999999) exibido na UI e usado como EmployeeId no T50M.
        // Vindo do pool CodigoDisponivel. Nullable para compatibilidade com cadastros antigos
        // (que serão preenchidos por migração de dados).
        [Column("codigoUsuario", TypeName = "varchar(6)")]
        [MaxLength(6)]
        public string? CodigoUsuario { get; set; }

        [Required]
        [Column("nome",TypeName="varchar(80)")]
        [MaxLength(80)]
        public string Nome { get; set; }

        [Required]
        [Column("cpf",TypeName="varchar(15)")]
        [MaxLength(15)]
        public string Cpf { get; set; }

        [Required]
        [Column("cargo", TypeName = "varchar(100)")]
        [MaxLength(100)]
        public string Cargo { get; set; }

        [Required]
        [Column("email", TypeName = "varchar(150)")]
        [MaxLength(150)]
        public string Email { get; set; }

        [Required]
        [Column("senhaHash", TypeName = "varchar(255)")]
        [MaxLength(255)]
        public string senhaHash { get; set; }

        [Required]
        [Column("senhaClear", TypeName = "varchar(255)")]
        [MaxLength(255)]
        public string senhaClear { get; set; }

        [Required]
        [Column("modoAcesso", TypeName = "varchar(15)")]
        [MaxLength(15)]
        public string modoAcesso { get; set; }

    
        [Column("biometriaCadastrada", TypeName = "DATETIME")]
        public DateTime? biometriaCadastrada { get; set; }

        [Column("templateBackup", TypeName = "BLOB")]
        public byte[] templateBackup { get; set; }

        [Required]
        [Column("status", TypeName = "varchar(10)")]
        [MaxLength(10)]
        public string Status {  get; set; }

        [Column("dataUltimoAcesso", TypeName = "DATETIME")]
        public DateTime? dataUltimoAcesso { get; set; }

        [Required]
        [Column("dataCadastro", TypeName = "DATETIME")]
        public DateTime? dataCadastro { get; set; }

        // Slot ocupado no AS608 (1-127). Alocado no enroll, liberado no reset/inativar.
        // Permite reuso de slots quando pessoa é removida — sem isso, o sensor lota em 127.
        [Column("slotAs608")]
        public int? SlotAs608 { get; set; }

        // Quando admin reseta biometria/inativa, o slot anterior fica aqui pra o Worker
        // enviar CMD|FINGER|DELETE ao Arduino. Worker zera após confirmação.
        [Column("slotAs608ParaApagar")]
        public int? SlotAs608ParaApagar { get; set; }
    }
}
