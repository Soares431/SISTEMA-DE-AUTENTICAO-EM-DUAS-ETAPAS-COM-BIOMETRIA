using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Model
{
    [Table("dispositivoT50")]
    public class DispositivoT50
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("nome", TypeName = "varchar(100)")]
        [MaxLength(100)]
        public string Nome { get; set; }

        [Required]
        [Column("enderecoIP", TypeName = "varchar(15)")]
        [MaxLength(15)]
        public string EnderecoIP { get; set; }

        [Column("porta")]
        public int Porta { get; set; } = 5010; // padrão 5010

        [Column("digitaisCadastradas")]
        public int DigitaisCadastradas { get; set; }

        // Atualizado pelo Worker quando conecta ou recebe evento. Null = nunca conectou.
        // Considerado "online" se a última conexão foi há menos de OnlineThreshold.
        [Column("ultimaConexao")]
        public DateTime? UltimaConexao { get; set; }

        // Threshold para considerar online — definido em 2 min porque o polling é de 2s.
        // Margem extra para tolerar 1-2 ciclos perdidos sem flutuar entre online/offline.
        [NotMapped]
        public static readonly TimeSpan OnlineThreshold = TimeSpan.FromMinutes(2);

        [NotMapped]
        public bool EstaOnline =>
            UltimaConexao.HasValue && (DateTime.UtcNow - UltimaConexao.Value) < OnlineThreshold;
    }
}
