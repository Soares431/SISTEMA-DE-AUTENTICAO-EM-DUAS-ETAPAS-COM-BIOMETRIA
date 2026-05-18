// Tests/DispositivoT50RepositoryTests.cs
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Tests
{
    public class DispositivoT50RepositoryTests
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

        private DispositivoT50 CriarDispositivo(string nome) => new DispositivoT50
        {
            Nome = nome,
            EnderecoIP = "192.168.1.1",
            Porta = 5010,
            DigitaisCadastradas = 0
        };

        [Fact]
        public void Adicionar_DeveRetornarDispositivo()
        {
            using var db = CriarContexto();
            var repo = new DispositivoT50Implemetions(db);
            var dispositivo = CriarDispositivo("T50-Principal");
            var resultado = repo.Adicionar(dispositivo);
            Assert.NotNull(resultado);
            Assert.Equal("T50-Principal", resultado.Nome);
        }

        [Fact]
        public void TemVagaDigital_DeveRetornarTrue_QuandoAbaixoDoLimite()
        {
            using var db = CriarContexto();
            var repo = new DispositivoT50Implemetions(db);
            var dispositivo = CriarDispositivo("T50-Principal");
            repo.Adicionar(dispositivo);
            var resultado = repo.TemVagaDigital(dispositivo.Id);
            Assert.True(resultado);
        }

        [Fact]
        public void TemVagaDigital_DeveRetornarFalse_QuandoLimiteAtingido()
        {
            using var db = CriarContexto();
            var repo = new DispositivoT50Implemetions(db);
            var dispositivo = CriarDispositivo("T50-Principal");
            dispositivo.DigitaisCadastradas = 1000;
            repo.Adicionar(dispositivo);
            var resultado = repo.TemVagaDigital(dispositivo.Id);
            Assert.False(resultado);
        }

        [Fact]
        public void ContarDigitaisCadastradas_DeveRetornarContagem()
        {
            using var db = CriarContexto();
            var repo = new DispositivoT50Implemetions(db);
            var dispositivo = CriarDispositivo("T50-Principal");
            dispositivo.DigitaisCadastradas = 5;
            repo.Adicionar(dispositivo);
            var resultado = repo.ContarDigitaisCadastradas(dispositivo.Id);
            Assert.Equal(5, resultado);
        }
    }
}