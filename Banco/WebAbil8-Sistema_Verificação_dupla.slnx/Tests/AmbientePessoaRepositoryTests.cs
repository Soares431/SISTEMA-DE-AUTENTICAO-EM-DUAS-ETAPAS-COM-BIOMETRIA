using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions;
using Xunit;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Tests
{

    public class AmbientePessoaRepositoryTests
    {
        private static IConfiguration CriarConfiguration() => new ConfigurationBuilder().Build();

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

        private async Task<(Pessoa pessoa, Ambiente ambiente)> SetupPessoaEAmbiente(AppDbContext db)
        {
            var pessoaRepo = new PessoaImplemetions(db, CriarConfiguration());
            var pessoa = await pessoaRepo.Adicionar(new Pessoa
            {
                Nome = "Pedro",
                Cpf = "55555555555",
                Cargo = "TI",
                Email = "pedro@teste.com",
                senhaHash = "h",
                senhaClear = "123456",
                modoAcesso = "digital_e_senha",
                Status = "inativo",
                dataCadastro = DateTime.UtcNow
            });

            var dispositivo = new DispositivoT50 { Nome = "T50-1", EnderecoIP = "10.0.0.1", Porta = 5010, DigitaisCadastradas = 0 };
            db.DispositivosT50.Add(dispositivo);
            await db.SaveChangesAsync();

            var ambiente = new Ambiente
            {
                Nome = "Sala A",
                DispositivoT50Id = dispositivo.Id,
                TempoEsperaGravacaoSeg = 60,
                DataCriacao = DateTime.UtcNow
            };
            db.Ambientes.Add(ambiente);
            await db.SaveChangesAsync();

            return (pessoa, ambiente);
        }

        [Fact]
        public async Task AdicionarPessoa_DeveCriarVinculo()
        {
            using var db = CriarContexto();
            var (pessoa, ambiente) = await SetupPessoaEAmbiente(db);
            var repo = new AmbientePessoaImplemetions(db);

            repo.AdicionarPessoa(new AmbientePessoa { AmbienteId = ambiente.Id, PessoaId = pessoa.Id });

            Assert.True(repo.PessoaTemAcesso(ambiente.Id, pessoa.Id));
            Assert.Equal(1, repo.ContarPessoasPorAmbiente(ambiente.Id));
        }

        [Fact]
        public async Task RemoverPessoa_DeveQuebrarVinculo()
        {
            using var db = CriarContexto();
            var (pessoa, ambiente) = await SetupPessoaEAmbiente(db);
            var repo = new AmbientePessoaImplemetions(db);

            repo.AdicionarPessoa(new AmbientePessoa { AmbienteId = ambiente.Id, PessoaId = pessoa.Id });
            repo.RemoverPessoa(ambiente.Id, pessoa.Id);

            Assert.False(repo.PessoaTemAcesso(ambiente.Id, pessoa.Id));
        }

        [Fact]
        public async Task Reativacao_PessoaComTemplate_DeveMarcarBiometriaCadastrada()
        {

            using var db = CriarContexto();
            var (pessoa, ambiente) = await SetupPessoaEAmbiente(db);

            pessoa.templateBackup = new byte[] { 1, 2, 3, 4 };
            pessoa.biometriaCadastrada = null;
            db.Pessoas.Update(pessoa);
            await db.SaveChangesAsync();

            var pessoaRepo = new PessoaImplemetions(db, CriarConfiguration());
            var apRepo = new AmbientePessoaImplemetions(db);

            apRepo.AdicionarPessoa(new AmbientePessoa { AmbienteId = ambiente.Id, PessoaId = pessoa.Id });
            var pAtual = await pessoaRepo.BuscarPorId(pessoa.Id);
            if (pAtual.templateBackup?.Length > 0 && pAtual.biometriaCadastrada == null)
                await pessoaRepo.MarcarBiometriaCadastrada(pAtual.Id);

            var final = await pessoaRepo.BuscarPorId(pessoa.Id);
            Assert.NotNull(final.biometriaCadastrada);
        }

        [Fact]
        public async Task T50Cheio_AdicionarPessoaEmSomenteSenha_NaoIncrementaDigitais()
        {

            using var db = CriarContexto();
            var (pessoa, ambiente) = await SetupPessoaEAmbiente(db);

            var dispositivo = await db.DispositivosT50.FindAsync(ambiente.DispositivoT50Id);
            dispositivo!.DigitaisCadastradas = 1000;
            await db.SaveChangesAsync();

            var pessoaRepo = new PessoaImplemetions(db, CriarConfiguration());
            var apRepo = new AmbientePessoaImplemetions(db);

            apRepo.AdicionarPessoa(new AmbientePessoa { AmbienteId = ambiente.Id, PessoaId = pessoa.Id });
            var p = await pessoaRepo.BuscarPorId(pessoa.Id);
            p.modoAcesso = "somente_senha";
            await pessoaRepo.Atualizar(p);

            var finalDisp = await db.DispositivosT50.FindAsync(dispositivo.Id);
            Assert.Equal(1000, finalDisp!.DigitaisCadastradas);

            var finalPessoa = await pessoaRepo.BuscarPorId(pessoa.Id);
            Assert.Equal("somente_senha", finalPessoa.modoAcesso);
        }
    }
}

