using Microsoft.EntityFrameworkCore;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions;
using Xunit;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Tests
{
    public class CameraRepositoryTests
    {
        private AppDbContext CriarContexto()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;
            var context = new AppDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();

            context.Ambientes.Add(new Ambiente { Id = 1, Nome = "Sala 1", TempoEsperaGravacaoSeg = 60, DataCriacao = DateTime.UtcNow });
            context.Ambientes.Add(new Ambiente { Id = 2, Nome = "Sala 2", TempoEsperaGravacaoSeg = 60, DataCriacao = DateTime.UtcNow });
            context.SaveChanges();
            return context;
        }

        private Camera CriarCamera(string nome, int ambienteId, bool ativa = true) => new Camera
        {
            Nome = nome,
            AmbienteId = ambienteId,
            UrlRTSP = "rtsp://teste/" + nome,
            EnderecoONVIF = "http://teste/onvif",
            Tipo = "interna",
            Ativa = ativa
        };

        [Fact]
        public async Task Adicionar_DeveCriarCamera()
        {
            using var db = CriarContexto();
            var repo = new CameraImplemetions(db);
            await repo.Adicionar(CriarCamera("Cam1", 1));
            var lista = await repo.ListarComFiltros(null, null);
            Assert.Single(lista);
        }

        [Fact]
        public async Task Atualizar_DeveRetornarTrueQuandoExiste()
        {
            using var db = CriarContexto();
            var repo = new CameraImplemetions(db);
            var cam = CriarCamera("CamA", 1);
            await repo.Adicionar(cam);

            cam.Nome = "CamRenomeada";
            var ok = await repo.Atualizar(cam);
            Assert.True(ok);

            var atual = await repo.BuscarPorId(cam.Id);
            Assert.Equal("CamRenomeada", atual!.Nome);
        }

        [Fact]
        public async Task Atualizar_DeveRetornarFalseQuandoNaoExiste()
        {
            using var db = CriarContexto();
            var repo = new CameraImplemetions(db);
            var fake = CriarCamera("Fake", 1);
            fake.Id = 999;
            Assert.False(await repo.Atualizar(fake));
        }

        [Fact]
        public async Task Remover_DeveRetornarTrueQuandoExiste()
        {
            using var db = CriarContexto();
            var repo = new CameraImplemetions(db);
            var cam = CriarCamera("CamX", 2);
            await repo.Adicionar(cam);

            Assert.True(await repo.Remover(cam.Id));
            Assert.Null(await repo.BuscarPorId(cam.Id));
        }

        [Fact]
        public async Task Remover_DeveRetornarFalseQuandoNaoExiste()
        {
            using var db = CriarContexto();
            var repo = new CameraImplemetions(db);
            Assert.False(await repo.Remover(123));
        }

        [Fact]
        public async Task ListarPorAmbiente_DeveFiltrarPeloAmbiente()
        {
            using var db = CriarContexto();
            var repo = new CameraImplemetions(db);
            await repo.Adicionar(CriarCamera("A1", 1));
            await repo.Adicionar(CriarCamera("A2", 1));
            await repo.Adicionar(CriarCamera("B1", 2));

            var ambiente1 = await repo.ListarPorAmbiente(1);
            Assert.Equal(2, ambiente1.Count);
        }

        [Fact]
        public async Task ListarComFiltros_PorNome()
        {
            using var db = CriarContexto();
            var repo = new CameraImplemetions(db);
            await repo.Adicionar(CriarCamera("Recepção", 1));
            await repo.Adicionar(CriarCamera("Garagem", 1));
            var r = await repo.ListarComFiltros("Recep", null);
            Assert.Single(r);
        }

        [Fact]
        public async Task ListarComFiltros_PorAtiva()
        {
            using var db = CriarContexto();
            var repo = new CameraImplemetions(db);
            await repo.Adicionar(CriarCamera("C1", 1, ativa: true));
            await repo.Adicionar(CriarCamera("C2", 1, ativa: false));
            Assert.Single(await repo.ListarComFiltros(null, true));
            Assert.Single(await repo.ListarComFiltros(null, false));
        }
    }
}

