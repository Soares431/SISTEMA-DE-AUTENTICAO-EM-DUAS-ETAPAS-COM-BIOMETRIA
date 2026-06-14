using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Tests
{
    public class PessoaRepositoryTests
    {
        private static IConfiguration CriarConfiguration() =>
            new ConfigurationBuilder().Build();

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
            var repo = new PessoaImplemetions(db, CriarConfiguration());
            var pessoa = CriarPessoa("Lucas", "12345678901");
            var resultado = await repo.Adicionar(pessoa);
            Assert.NotNull(resultado);
            Assert.Equal("Lucas", resultado.Nome);
        }

        [Fact]
        public async Task Adicionar_CPFDuplicado_DeveLancarExcecao()
        {
            using var db = CriarContexto();
            var repo = new PessoaImplemetions(db, CriarConfiguration());
            await repo.Adicionar(CriarPessoa("Lucas", "12345678901"));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                repo.Adicionar(CriarPessoa("João", "12345678901")));
        }

        [Fact]
        public async Task BuscarPorId_DeveRetornarPessoa()
        {
            using var db = CriarContexto();
            var repo = new PessoaImplemetions(db, CriarConfiguration());
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
            var repo = new PessoaImplemetions(db, CriarConfiguration());
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
            var repo = new PessoaImplemetions(db, CriarConfiguration());
            var pessoa = CriarPessoa("Lucas", "33333333333");
            await repo.Adicionar(pessoa);
            await repo.AlterarStatus(pessoa.Id, false);
            var resultado = await repo.BuscarPorId(pessoa.Id);
            Assert.Equal("inativo", resultado.Status);
        }

        [Fact]
        public async Task AtualizarSenha_DeveCriptografarSenhaClearComAes()
        {
            using var db = CriarContexto();
            var repo = new PessoaImplemetions(db, CriarConfiguration());
            var pessoa = CriarPessoa("Lucas", "44444444444");
            await repo.Adicionar(pessoa);

            await repo.AtualizarSenha(pessoa.Id, "999000", "novoHash");

            var resultado = await repo.BuscarPorId(pessoa.Id);

            Assert.NotEqual("999000", resultado.senhaClear);
            Assert.Equal("novoHash", resultado.senhaHash);

            var key = "5cta-aes-key-senha-segura-32char";
            Assert.Equal("999000", Services.AesHelper.Decrypt(resultado.senhaClear, key));
        }

        [Fact]
        public void AesHelper_EncryptDecrypt_Roundtrip()
        {
            const string key = "5cta-aes-key-senha-segura-32char";
            var cipher = Services.AesHelper.Encrypt("123456", key);
            Assert.NotEqual("123456", cipher);
            Assert.Equal("123456", Services.AesHelper.Decrypt(cipher, key));
        }

        [Fact]
        public void AesHelper_EncryptComKeysIguais_GeraCiphersDiferentes()
        {

            const string key = "5cta-aes-key-senha-segura-32char";
            var c1 = Services.AesHelper.Encrypt("abc", key);
            var c2 = Services.AesHelper.Encrypt("abc", key);
            Assert.NotEqual(c1, c2);
            Assert.Equal("abc", Services.AesHelper.Decrypt(c1, key));
            Assert.Equal("abc", Services.AesHelper.Decrypt(c2, key));
        }
    }
}

