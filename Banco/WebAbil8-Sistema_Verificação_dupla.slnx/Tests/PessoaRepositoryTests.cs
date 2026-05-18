// Tests/PessoaRepositoryTests.cs
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Tests
{
    public class PessoaRepositoryTests
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

        private Pessoa CriarPessoa(string nome, string cpf) => new Pessoa
        {
            Nome = nome,
            Cpf = cpf,
            Cargo = "Funcionario",
            Email = $"{nome.ToLower()}@teste.com",
            senhaHash = "hash123",
            senhaClear = "123456",
            modoAcesso = "somente_senha",
            Status = "ativo",
            dataCadastro = DateTime.UtcNow
        };

        [Fact]
        public async Task Adicionar_DeveRetornarPessoa()
        {
            using var db = CriarContexto();
            var repo = new PessoaImplemetions(db);
            var pessoa = CriarPessoa("Lucas", "12345678901");
            var resultado = await repo.Adicionar(pessoa);
            Assert.NotNull(resultado);
            Assert.Equal("Lucas", resultado.Nome);
        }

        [Fact]
        public async Task Adicionar_CPFDuplicado_DeveLancarExcecao()
        {
            using var db = CriarContexto();
            var repo = new PessoaImplemetions(db);
            await repo.Adicionar(CriarPessoa("Lucas", "12345678901"));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                repo.Adicionar(CriarPessoa("João", "12345678901")));
        }

        [Fact]
        public async Task BuscarPorId_DeveRetornarPessoa()
        {
            using var db = CriarContexto();
            var repo = new PessoaImplemetions(db);
            var pessoa = CriarPessoa("Lucas", "11111111111");
            await repo.Adicionar(pessoa);
            var resultado = await repo.BuscarPorId(pessoa.Id);
            Assert.NotNull(resultado);
            Assert.Equal("Lucas", resultado.Nome);
        }

        [Fact]
        public async Task Remover_DeveRemoverPessoa()
        {
            using var db = CriarContexto();
            var repo = new PessoaImplemetions(db);
            var pessoa = CriarPessoa("Lucas", "22222222222");
            await repo.Adicionar(pessoa);
            await repo.Remover(pessoa.Id);
            var resultado = await repo.BuscarPorId(pessoa.Id);
            Assert.Null(resultado);
        }

        [Fact]
        public async Task AlterarStatus_DeveInativarPessoa()
        {
            using var db = CriarContexto();
            var repo = new PessoaImplemetions(db);
            var pessoa = CriarPessoa("Lucas", "33333333333");
            await repo.Adicionar(pessoa);
            await repo.AlterarStatus(pessoa.Id, false);
            var resultado = await repo.BuscarPorId(pessoa.Id);
            Assert.Equal("inativo", resultado.Status);
        }
    }
}