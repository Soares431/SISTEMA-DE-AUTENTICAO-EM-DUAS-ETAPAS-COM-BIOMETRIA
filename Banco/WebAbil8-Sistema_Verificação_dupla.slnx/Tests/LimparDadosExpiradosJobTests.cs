using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebAbil8_Sistema_Verificação_dupla.slnx.Jobs;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using Xunit;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Tests
{
    public class LimparDadosExpiradosJobTests
    {
        private AppDbContext CriarContexto()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;
            var context = new AppDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public void Executar_DeveRemoverTentativasExpiradas()
        {
            using var db = CriarContexto();
            var amb = new Ambiente { Nome = "Sala", TempoEsperaGravacaoSeg = 60, DataCriacao = DateTime.UtcNow };
            db.Ambientes.Add(amb);
            db.SaveChanges();

            db.TentativasAcesso.Add(new TentativaAcesso
            {
                AmbienteId = amb.Id,
                DataHora = DateTime.UtcNow.AddDays(-200),
                AcessoLiberado = true,
                DataExpiracao = DateTime.UtcNow.AddDays(-10)
            });
            db.TentativasAcesso.Add(new TentativaAcesso
            {
                AmbienteId = amb.Id,
                DataHora = DateTime.UtcNow,
                AcessoLiberado = true,
                DataExpiracao = DateTime.UtcNow.AddDays(90)
            });
            db.SaveChanges();

            var job = new LimparDadosExpiradosJob(db, NullLogger<LimparDadosExpiradosJob>.Instance);
            job.Executar();

            Assert.Single(db.TentativasAcesso.ToList());
        }

        [Fact]
        public void Executar_DeveRemoverLogsExpirados()
        {
            using var db = CriarContexto();
            var admin = new Administrador { Login = "a", NomeCompleto = "A", SenhaHash = "h", DataCriacao = DateTime.UtcNow };
            db.Administradores.Add(admin);
            db.SaveChanges();

            db.LogsAdmin.Add(new LogAdmin
            {
                AdminId = admin.Id,
                Acao = "Login",
                EntidadeAfetada = "Administrador",
                DataHora = DateTime.UtcNow.AddDays(-300),
                DataExpiracao = DateTime.UtcNow.AddDays(-1)
            });
            db.LogsAdmin.Add(new LogAdmin
            {
                AdminId = admin.Id,
                Acao = "Login",
                EntidadeAfetada = "Administrador",
                DataHora = DateTime.UtcNow,
                DataExpiracao = DateTime.UtcNow.AddDays(180)
            });
            db.SaveChanges();

            var job = new LimparDadosExpiradosJob(db, NullLogger<LimparDadosExpiradosJob>.Instance);
            job.Executar();

            Assert.Single(db.LogsAdmin.ToList());
        }

        [Fact]
        public void Executar_RegistrosSemDataExpiracao_NaoSaoTocados()
        {
            using var db = CriarContexto();
            var amb = new Ambiente { Nome = "Sala", TempoEsperaGravacaoSeg = 60, DataCriacao = DateTime.UtcNow };
            db.Ambientes.Add(amb);
            db.SaveChanges();

            db.TentativasAcesso.Add(new TentativaAcesso
            {
                AmbienteId = amb.Id,
                DataHora = DateTime.UtcNow.AddDays(-500),
                AcessoLiberado = false,
                DataExpiracao = null
            });
            db.SaveChanges();

            var job = new LimparDadosExpiradosJob(db, NullLogger<LimparDadosExpiradosJob>.Instance);
            job.Executar();

            Assert.Single(db.TentativasAcesso.ToList());
        }

        [Fact]
        public void Executar_BancoVazio_NaoQuebra()
        {
            using var db = CriarContexto();
            var job = new LimparDadosExpiradosJob(db, NullLogger<LimparDadosExpiradosJob>.Instance);

            var ex = Record.Exception(() => job.Executar());
            Assert.Null(ex);
        }
    }
}

