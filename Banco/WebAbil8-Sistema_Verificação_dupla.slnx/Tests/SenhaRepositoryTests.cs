// Tests/SenhaRepositoryTests.cs
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Tests
{
    public class SenhaRepositoryTests
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
        public void BuscarDisponivel_DeveRetornarSenhaNaoUsada()
        {
            using var db = CriarContexto();
            var repo = new SenhaImplemetions(db);
            db.SenhasDisponiveis.Add(new SenhaDisponivel { Senha = "100001", EmUso = false });
            db.SenhasDisponiveis.Add(new SenhaDisponivel { Senha = "100002", EmUso = true });
            db.SaveChanges();
            var resultado = repo.BuscarDisponivel();
            Assert.NotNull(resultado);
            Assert.Equal("100001", resultado.Senha);
        }

        [Fact]
        public void BuscarDisponivel_NaoDeveRetornarSenhaAbaixoDe100000()
        {
            using var db = CriarContexto();
            var repo = new SenhaImplemetions(db);
            db.SenhasDisponiveis.Add(new SenhaDisponivel { Senha = "099999", EmUso = false });
            db.SaveChanges();
            var resultado = repo.BuscarDisponivel();
            Assert.Null(resultado);
        }

        [Fact]
        public void MarcarEmUso_DeveMudarStatus()
        {
            using var db = CriarContexto();
            var repo = new SenhaImplemetions(db);
            db.SenhasDisponiveis.Add(new SenhaDisponivel { Senha = "100001", EmUso = false });
            db.SaveChanges();
            repo.MarcarEmUso("100001", true);
            var resultado = repo.BuscarDisponivel("100001");
            Assert.True(resultado.EmUso);
        }

        [Fact]
        public void Liberar_DeveMarcarComoNaoUsada()
        {
            using var db = CriarContexto();
            var repo = new SenhaImplemetions(db);
            db.SenhasDisponiveis.Add(new SenhaDisponivel { Senha = "100001", EmUso = true });
            db.SaveChanges();
            repo.Liberar("100001");
            var resultado = repo.BuscarDisponivel("100001");
            Assert.False(resultado.EmUso);
        }
    }
}