using Microsoft.EntityFrameworkCore;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // Todos os DbSets
        public DbSet<Pessoa> Pessoas { get; set; }
        public DbSet<Ambiente> Ambientes { get; set; }
        public DbSet<AmbientePessoa> AmbientesPessoas { get; set; }
        public DbSet<DispositivoT50> DispositivosT50 { get; set; }
        public DbSet<Camera> Cameras { get; set; }
        public DbSet<TentativaAcesso> TentativasAcesso { get; set; }
        public DbSet<LogAdmin> LogsAdmin { get; set; }
        public DbSet<Administrador> Administradores { get; set; }
        public DbSet<SenhaDisponivel> SenhasDisponiveis { get; set; }
        public DbSet<Configuracao> Configuracoes { get; set; }

        // Relacionamento muitos-para-muitos (se você ainda usar essa entidade)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AmbientePessoa>()
                .HasKey(ap => new { ap.AmbienteId, ap.PessoaId });

  
        }
    }
}
