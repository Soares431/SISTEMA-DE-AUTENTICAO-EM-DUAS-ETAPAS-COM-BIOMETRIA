// Tests/TentativaAcessoRepositoryTests.cs
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Tests
{
    public class TentativaAcessoRepositoryTests
    {
        private (AppDbContext, int) CriarContextoComAmbiente()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;
            var context = new AppDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();

            var ambiente = new Ambiente
            {
                Nome = "Ambiente Teste",
                DispositivoT50Id = 1,
                TempoEsperaGravacaoSeg = 60,
                DataCriacao = DateTime.UtcNow
            };
            context.Ambientes.Add(ambiente);
            context.SaveChanges();

            return (context, ambiente.Id);
        }

        private (AppDbContext, int, int) CriarContextoComDoisAmbientes()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;
            var context = new AppDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();

            var amb1 = new Ambiente
            {
                Nome = "Ambiente 1",
                DispositivoT50Id = 1,
                TempoEsperaGravacaoSeg = 60,
                DataCriacao = DateTime.UtcNow
            };
            var amb2 = new Ambiente
            {
                Nome = "Ambiente 2",
                DispositivoT50Id = 2,
                TempoEsperaGravacaoSeg = 60,
                DataCriacao = DateTime.UtcNow
            };
            context.Ambientes.AddRange(amb1, amb2);
            context.SaveChanges();

            return (context, amb1.Id, amb2.Id);
        }

        private TentativaAcesso CriarTentativa(int ambienteId, bool acessoLiberado) => new TentativaAcesso
        {
            AmbienteId = ambienteId,
            AcessoLiberado = acessoLiberado,
            DataHora = DateTime.UtcNow
        };

        [Fact]
        public void Registrar_DeveAdicionarTentativa()
        {
            var (db, ambienteId) = CriarContextoComAmbiente();
            using (db)
            {
                var repo = new TentativaAcessoImplemetions(db);
                var tentativa = CriarTentativa(ambienteId, true);
                var resultado = repo.Registrar(tentativa);
                Assert.NotNull(resultado);
                Assert.True(resultado.AcessoLiberado);
            }
        }

        [Fact]
        public void ListarPorAmbiente_DeveRetornarTentativasDoAmbiente()
        {
            var (db, amb1, amb2) = CriarContextoComDoisAmbientes();
            using (db)
            {
                var repo = new TentativaAcessoImplemetions(db);
                repo.Registrar(CriarTentativa(amb1, true));
                repo.Registrar(CriarTentativa(amb1, false));
                repo.Registrar(CriarTentativa(amb2, true));
                var resultado = repo.ListarPorAmbiente(amb1);
                Assert.Equal(2, resultado.Count);
            }
        }

        [Fact]
        public void ListarComFiltros_DeveRetornarSomenteAcessosLiberados()
        {
            var (db, ambienteId) = CriarContextoComAmbiente();
            using (db)
            {
                var repo = new TentativaAcessoImplemetions(db);
                repo.Registrar(CriarTentativa(ambienteId, true));
                repo.Registrar(CriarTentativa(ambienteId, false));
                var resultado = repo.ListarComFiltros(null, null, true, null, null);
                Assert.All(resultado, t => Assert.True(t.AcessoLiberado));
            }
        }

        [Fact]
        public void Remover_DeveRemoverTentativa()
        {
            var (db, ambienteId) = CriarContextoComAmbiente();
            using (db)
            {
                var repo = new TentativaAcessoImplemetions(db);
                var tentativa = repo.Registrar(CriarTentativa(ambienteId, true));
                repo.Remover(tentativa.Id);
                var resultado = repo.BuscarPorId(tentativa.Id);
                Assert.Null(resultado);
            }
        }
    }
}