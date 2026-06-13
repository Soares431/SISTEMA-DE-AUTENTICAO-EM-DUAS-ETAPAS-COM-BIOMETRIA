using Microsoft.EntityFrameworkCore;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions;
using Xunit;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Tests
{
    public class LogAdminRepositoryTests
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

        private void SeedConfig(AppDbContext db, int retencaoLogs = 180)
        {
            db.Configuracoes.Add(new Configuracao
            {
                RetencaoGravacoesTentativasDias = 90,
                RetencaoLogsDias = retencaoLogs,
                TempoEsperaGravacaoSeg = 60
            });
            db.SaveChanges();
        }

        private Administrador SeedAdmin(AppDbContext db)
        {
            var adm = new Administrador
            {
                Login = "admin",
                NomeCompleto = "Admin Teste",
                SenhaHash = "h",
                DataCriacao = DateTime.UtcNow
            };
            db.Administradores.Add(adm);
            db.SaveChanges();
            return adm;
        }

        [Fact]
        public void Registrar_DevePreencherDataExpiracaoComRetencaoLogs()
        {
            using var db = CriarContexto();
            SeedConfig(db, retencaoLogs: 365);
            var admin = SeedAdmin(db);
            var repo = new LogAdminImplemetions(db);

            var log = repo.Registrar(admin.Id, "Login", "Administrador", admin.Id);

            Assert.NotNull(log.DataExpiracao);

            var dias = (log.DataExpiracao!.Value - log.DataHora).TotalDays;
            Assert.InRange(dias, 364.9, 365.1);
        }

        [Fact]
        public void Registrar_SemConfig_DeveUsarDefault180()
        {
            using var db = CriarContexto();
            var admin = SeedAdmin(db);
            var repo = new LogAdminImplemetions(db);

            var log = repo.Registrar(admin.Id, "Login", "Administrador", admin.Id);
            var dias = (log.DataExpiracao!.Value - log.DataHora).TotalDays;
            Assert.InRange(dias, 179.9, 180.1);
        }

        [Fact]
        public void ListarComFiltros_PorAdminId()
        {
            using var db = CriarContexto();
            SeedConfig(db);
            var a1 = SeedAdmin(db);
            var a2 = new Administrador { Login = "a2", NomeCompleto = "A2", SenhaHash = "h", DataCriacao = DateTime.UtcNow };
            db.Administradores.Add(a2);
            db.SaveChanges();
            var repo = new LogAdminImplemetions(db);

            repo.Registrar(a1.Id, "Login", "Administrador", a1.Id);
            repo.Registrar(a2.Id, "Login", "Administrador", a2.Id);

            var apenasA1 = repo.ListarComFiltros(a1.Id, null, null, null, null);
            Assert.Single(apenasA1);
            Assert.Equal(a1.Id, apenasA1[0].AdminId);
        }

        [Fact]
        public void ListarComFiltros_PorEntidade()
        {
            using var db = CriarContexto();
            SeedConfig(db);
            var admin = SeedAdmin(db);
            var repo = new LogAdminImplemetions(db);

            repo.Registrar(admin.Id, "Adicionar", "Pessoa", 1);
            repo.Registrar(admin.Id, "Adicionar", "Ambiente", 1);

            var apenasPessoa = repo.ListarComFiltros(null, null, "Pessoa", null, null);
            Assert.Single(apenasPessoa);
            Assert.Equal("Pessoa", apenasPessoa[0].EntidadeAfetada);
        }

        [Fact]
        public void ListarComFiltros_PorIntervaloDatas()
        {
            using var db = CriarContexto();
            SeedConfig(db);
            var admin = SeedAdmin(db);
            var repo = new LogAdminImplemetions(db);

            var antigo = new LogAdmin
            {
                AdminId = admin.Id,
                Acao = "Login",
                EntidadeAfetada = "Administrador",
                DataHora = DateTime.UtcNow.AddDays(-10),
                DataExpiracao = DateTime.UtcNow.AddDays(170)
            };
            db.LogsAdmin.Add(antigo);
            db.SaveChanges();
            repo.Registrar(admin.Id, "Login", "Administrador", admin.Id);

            var ultimos7 = repo.ListarComFiltros(null, null, null,
                DateTime.UtcNow.AddDays(-7), null);
            Assert.Single(ultimos7);
        }

        [Fact]
        public void ListarTodos_DeveIncluirAdministrador()
        {
            using var db = CriarContexto();
            SeedConfig(db);
            var admin = SeedAdmin(db);
            var repo = new LogAdminImplemetions(db);
            repo.Registrar(admin.Id, "Login", "Administrador", admin.Id);

            var logs = repo.ListarTodos();
            Assert.NotNull(logs[0].Administrador);
            Assert.Equal("Admin Teste", logs[0].Administrador!.NomeCompleto);
        }
    }
}

