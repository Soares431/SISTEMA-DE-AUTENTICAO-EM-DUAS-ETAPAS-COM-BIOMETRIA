using Microsoft.EntityFrameworkCore;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions;
using Xunit;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Tests
{
    public class ConfiguracaoRepositoryTests
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

        private Configuracao SeedConfig(AppDbContext db)
        {
            var config = new Configuracao
            {
                RetencaoGravacoesTentativasDias = 90,
                RetencaoLogsDias = 180,
                TempoEsperaGravacaoSeg = 60
            };
            db.Configuracoes.Add(config);
            db.SaveChanges();
            return config;
        }

        [Fact]
        public async Task BuscarPorChave_DeveRetornarConfigSingleton()
        {
            using var db = CriarContexto();
            SeedConfig(db);
            var repo = new ConfiguracaoImplemetions(db);

            var c = await repo.BuscarPorChave();
            Assert.NotNull(c);
            Assert.Equal(90, c!.RetencaoGravacoesTentativasDias);
            Assert.Equal(180, c.RetencaoLogsDias);
            Assert.Equal(60, c.TempoEsperaGravacaoSeg);
        }

        [Fact]
        public async Task BuscarPorChave_SemRegistro_DeveRetornarNull()
        {
            using var db = CriarContexto();
            var repo = new ConfiguracaoImplemetions(db);
            Assert.Null(await repo.BuscarPorChave());
        }

        [Fact]
        public async Task Atualizar_DevePersistirNovosValores()
        {
            using var db = CriarContexto();
            SeedConfig(db);
            var repo = new ConfiguracaoImplemetions(db);

            var atual = await repo.BuscarPorChave();
            atual!.RetencaoGravacoesTentativasDias = 120;
            atual.RetencaoLogsDias = 365;
            atual.TempoEsperaGravacaoSeg = 90;
            await repo.Atualizar(atual);

            var depois = await repo.BuscarPorChave();
            Assert.Equal(120, depois!.RetencaoGravacoesTentativasDias);
            Assert.Equal(365, depois.RetencaoLogsDias);
            Assert.Equal(90, depois.TempoEsperaGravacaoSeg);
        }

        [Fact]
        public async Task Atualizar_SemRegistro_DeveLancarExcecao()
        {
            using var db = CriarContexto();
            var repo = new ConfiguracaoImplemetions(db);
            var c = new Configuracao
            {
                RetencaoGravacoesTentativasDias = 90,
                RetencaoLogsDias = 180,
                TempoEsperaGravacaoSeg = 60
            };
            await Assert.ThrowsAsync<ArgumentNullException>(() => repo.Atualizar(c));
        }
    }
}

