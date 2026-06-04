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
        public DbSet<CodigoDisponivel> CodigosDisponiveis { get; set; }
        public DbSet<Configuracao> Configuracoes { get; set; }
        public DbSet<AmbienteT50> AmbientesT50 { get; set; }
        public DbSet<PessoaT50> PessoasT50 { get; set; }

        // Relacionamento muitos-para-muitos (se você ainda usar essa entidade)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AmbientePessoa>()
                .HasKey(ap => new { ap.AmbienteId, ap.PessoaId });

            // Garante que a mesma combinação ambiente+T50 não se repita
            modelBuilder.Entity<AmbienteT50>()
                .HasIndex(at => new { at.AmbienteId, at.DispositivoT50Id })
                .IsUnique();

            // Mesma pessoa não pode ser cadastrada duas vezes no mesmo T50
            modelBuilder.Entity<PessoaT50>()
                .HasIndex(pt => new { pt.PessoaId, pt.DispositivoT50Id })
                .IsUnique();
        }
    }
}
