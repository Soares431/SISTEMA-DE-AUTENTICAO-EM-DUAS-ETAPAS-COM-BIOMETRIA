using Microsoft.EntityFrameworkCore;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions;
using Xunit;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Tests
{
    public class AdministradorRepositoryTests
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

        private Administrador CriarAdmin(string login, string nome) => new Administrador
        {
            Login = login,
            NomeCompleto = nome,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 10),
            DataCriacao = DateTime.UtcNow
        };

        [Fact]
        public void Adicionar_DeveAtribuirId()
        {
            using var db = CriarContexto();
            var repo = new AdministradorImplemetions(db);
            var adm = repo.Adicionar(CriarAdmin("user1", "Usuário 1"));
            Assert.True(adm.Id > 0);
        }

        [Fact]
        public void BuscarPorLogin_DeveRetornarAdminCorreto()
        {
            using var db = CriarContexto();
            var repo = new AdministradorImplemetions(db);
            repo.Adicionar(CriarAdmin("ana", "Ana Silva"));
            repo.Adicionar(CriarAdmin("bia", "Beatriz Souza"));

            var encontrado = repo.BuscarPorLogin("ana");
            Assert.NotNull(encontrado);
            Assert.Equal("Ana Silva", encontrado!.NomeCompleto);
        }

        [Fact]
        public void BuscarPorLogin_LoginInexistente_DeveRetornarNull()
        {
            using var db = CriarContexto();
            var repo = new AdministradorImplemetions(db);
            Assert.Null(repo.BuscarPorLogin("ninguem"));
        }

        [Fact]
        public void LoginExiste_DeveDetectarDuplicata()
        {
            using var db = CriarContexto();
            var repo = new AdministradorImplemetions(db);
            repo.Adicionar(CriarAdmin("admin", "Admin"));
            Assert.True(repo.LoginExiste("admin"));
            Assert.False(repo.LoginExiste("outro"));
        }

        [Fact]
        public void LoginExiste_IgnorarId_PermiteEdicaoDoProprio()
        {
            using var db = CriarContexto();
            var repo = new AdministradorImplemetions(db);
            var adm = repo.Adicionar(CriarAdmin("admin", "Admin"));
            // Quando ignora o próprio Id, o login dele não conta como duplicata
            Assert.False(repo.LoginExiste("admin", ignorarId: adm.Id));
            // Mas conta para os demais
            Assert.True(repo.LoginExiste("admin", ignorarId: 999));
        }

        [Fact]
        public void ListarTodos_DeveOrdenarPorNomeCompleto()
        {
            using var db = CriarContexto();
            var repo = new AdministradorImplemetions(db);
            repo.Adicionar(CriarAdmin("c", "Carla"));
            repo.Adicionar(CriarAdmin("a", "Ana"));
            repo.Adicionar(CriarAdmin("b", "Beatriz"));

            var lista = repo.ListarTodos();
            Assert.Equal("Ana", lista[0].NomeCompleto);
            Assert.Equal("Beatriz", lista[1].NomeCompleto);
            Assert.Equal("Carla", lista[2].NomeCompleto);
        }

        [Fact]
        public void Atualizar_DeveSalvarMudancas()
        {
            using var db = CriarContexto();
            var repo = new AdministradorImplemetions(db);
            var adm = repo.Adicionar(CriarAdmin("u", "Original"));
            adm.NomeCompleto = "Atualizado";
            adm.Cargo = "Sargento";
            repo.Atualizar(adm);

            var atual = repo.BuscarPorId(adm.Id);
            Assert.Equal("Atualizado", atual!.NomeCompleto);
            Assert.Equal("Sargento", atual.Cargo);
        }

        [Fact]
        public void Atualizar_AdminInexistente_DeveLancarExcecao()
        {
            using var db = CriarContexto();
            var repo = new AdministradorImplemetions(db);
            var adm = CriarAdmin("u", "Fantasma");
            adm.Id = 999;
            Assert.Throws<ArgumentException>(() => repo.Atualizar(adm));
        }
    }
}
