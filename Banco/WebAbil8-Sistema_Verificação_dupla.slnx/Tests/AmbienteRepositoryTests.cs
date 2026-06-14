using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Tests
{
    public class AmbienteRepositoryTests
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

        private Ambiente CriarAmbiente(string nome) => new Ambiente
        {
            Nome = nome,
            TempoEsperaGravacaoSeg = 60,
            DataCriacao = DateTime.UtcNow
        };

        [Fact]
        public void Adicionar_DeveRetornarAmbiente()
        {
            using var db = CriarContexto();
            var repo = new AmbienteImplementions(db);
            var ambiente = CriarAmbiente("Sala de Servidores");
            var resultado = repo.Adicionar(ambiente);
            Assert.NotNull(resultado);
            Assert.Equal("Sala de Servidores", resultado.Nome);
        }

        [Fact]
        public void BuscarPorId_DeveRetornarAmbiente()
        {
            using var db = CriarContexto();
            var repo = new AmbienteImplementions(db);
            var ambiente = CriarAmbiente("Sala de Servidores");
            repo.Adicionar(ambiente);
            var resultado = repo.BuscarPorId(ambiente.Id);
            Assert.NotNull(resultado);
            Assert.Equal("Sala de Servidores", resultado.Nome);
        }

        [Fact]
        public void Remover_DeveMarcarComoExcluido()
        {

            using var db = CriarContexto();
            var repo = new AmbienteImplementions(db);
            var ambiente = CriarAmbiente("Sala de Servidores");
            repo.Adicionar(ambiente);
            repo.Remover(ambiente.Id);
            var resultado = repo.BuscarPorId(ambiente.Id);
            Assert.NotNull(resultado);
            Assert.True(resultado.Excluido);
            Assert.NotNull(resultado.DataExclusao);
        }

        [Fact]
        public void Atualizar_DeveAtualizarAmbiente()
        {
            using var db = CriarContexto();
            var repo = new AmbienteImplementions(db);
            var ambiente = CriarAmbiente("Sala de Servidores");
            repo.Adicionar(ambiente);
            ambiente.Nome = "Sala de Reuniões";
            repo.Atualizar(ambiente);
            var resultado = repo.BuscarPorId(ambiente.Id);
            Assert.Equal("Sala de Reuniões", resultado.Nome);
        }

        [Fact]
        public void ListarTodos_DeveRetornarTodos()
        {
            using var db = CriarContexto();
            var repo = new AmbienteImplementions(db);
            repo.Adicionar(CriarAmbiente("Ambiente 1"));
            repo.Adicionar(CriarAmbiente("Ambiente 2"));
            var resultado = repo.ListarTodos();
            Assert.Equal(2, resultado.Count);
        }
    }
}

