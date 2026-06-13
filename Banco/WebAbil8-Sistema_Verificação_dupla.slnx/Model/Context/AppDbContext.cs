using Microsoft.EntityFrameworkCore;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

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
        public DbSet<T50Pendencia> T50Pendencias { get; set; }
        public DbSet<SlotAs608Orfao> SlotsAs608Orfaos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AmbientePessoa>()
                .HasKey(ap => new { ap.AmbienteId, ap.PessoaId });

            modelBuilder.Entity<AmbienteT50>()
                .HasIndex(at => new { at.AmbienteId, at.DispositivoT50Id })
                .IsUnique();

            modelBuilder.Entity<PessoaT50>()
                .HasIndex(pt => new { pt.PessoaId, pt.DispositivoT50Id })
                .IsUnique();
        }
    }
}

