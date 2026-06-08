using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Model
{
    // Fila de comandos pendentes para o hardware T50 — escrita pelo Frontend
    // (quando admin adiciona/remove pessoa de um T50) e consumida pelo Worker,
    // que executa via Anviz SDK e marca como sincronizado.
    //
    // Existe porque o Frontend (Blazor Server) não tem acesso direto ao hardware.
    // A doc §5.2 manda cadastrar a pessoa no T50 ao adicionar a um ambiente —
    // sem essa fila, o vínculo só existia no banco e nunca no firmware do T50.
    [Table("t50Pendencia")]
    public class T50Pendencia
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("acao", TypeName = "varchar(20)")]
        [MaxLength(20)]
        public string Acao { get; set; } = ""; // "adicionar" | "remover"

        [Column("pessoaId")]
        public long PessoaId { get; set; }

        [Column("dispositivoT50Id")]
        public int DispositivoT50Id { get; set; }

        [Column("criadoEm")]
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        [Column("sincronizadoEm")]
        public DateTime? SincronizadoEm { get; set; }

        [Column("sincronizado")]
        public bool Sincronizado { get; set; } = false;

        [Column("tentativasFalhas")]
        public int TentativasFalhas { get; set; } = 0;

        [Column("erroUltimaTentativa", TypeName = "varchar(500)")]
        [MaxLength(500)]
        public string? ErroUltimaTentativa { get; set; }
    }
}
