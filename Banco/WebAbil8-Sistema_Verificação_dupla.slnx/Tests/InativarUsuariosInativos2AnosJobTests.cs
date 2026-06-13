using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebAbil8_Sistema_Verificação_dupla.slnx.Jobs;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using Xunit;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Tests
{
    public class InativarUsuariosInativos2AnosJobTests
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

        private void SeedConfig(AppDbContext db, int meses = 24)
        {
            db.Configuracoes.Add(new Configuracao
            {
                RetencaoGravacoesTentativasDias = 90,
                RetencaoLogsDias = 180,
                TempoEsperaGravacaoSeg = 60,
                PeriodoInativacaoMeses = meses
            });
            db.SaveChanges();
        }

        private Pessoa AdicionarPessoa(AppDbContext db, string nome, string cpf,
            DateTime? ultimoAcesso, DateTime cadastro, string status = "ativo")
        {
            var p = new Pessoa
            {
                Nome = nome,
                Cpf = cpf,
                Cargo = "C",
                Email = nome + "@t.com",
                senhaHash = "h",
                senhaClear = "x",
                modoAcesso = "somente_senha",
                Status = status,
                dataUltimoAcesso = ultimoAcesso,
                dataCadastro = cadastro
            };
            db.Pessoas.Add(p);
            db.SaveChanges();
            return p;
        }

        [Fact]
        public void Executar_DeveInativarQuemUltrapassouPeriodo()
        {
            using var db = CriarContexto();
            SeedConfig(db, meses: 24);
            var velho = AdicionarPessoa(db, "Velho", "1",
                ultimoAcesso: DateTime.UtcNow.AddYears(-3),
                cadastro: DateTime.UtcNow.AddYears(-5));
            var novo = AdicionarPessoa(db, "Novo", "2",
                ultimoAcesso: DateTime.UtcNow.AddMonths(-3),
                cadastro: DateTime.UtcNow.AddMonths(-6));

            var job = new InativarUsuariosInativos2AnosJob(db, NullLogger<InativarUsuariosInativos2AnosJob>.Instance);
            job.Executar();

            Assert.Equal("inativo", db.Pessoas.Find(velho.Id)!.Status);
            Assert.Equal("ativo", db.Pessoas.Find(novo.Id)!.Status);
        }

        [Fact]
        public void Executar_SemUltimoAcesso_UsaDataCadastro()
        {
            using var db = CriarContexto();
            SeedConfig(db);
            var p = AdicionarPessoa(db, "Nunca", "9",
                ultimoAcesso: null,
                cadastro: DateTime.UtcNow.AddYears(-3));

            var job = new InativarUsuariosInativos2AnosJob(db, NullLogger<InativarUsuariosInativos2AnosJob>.Instance);
            job.Executar();

            Assert.Equal("inativo", db.Pessoas.Find(p.Id)!.Status);
        }

        [Fact]
        public void Executar_DeveRemoverTodosVinculosDeAmbiente()
        {
            using var db = CriarContexto();
            SeedConfig(db);
            var velho = AdicionarPessoa(db, "Velho", "1",
                ultimoAcesso: DateTime.UtcNow.AddYears(-3),
                cadastro: DateTime.UtcNow.AddYears(-5));

            var amb = new Ambiente { Nome = "Sala", TempoEsperaGravacaoSeg = 60, DataCriacao = DateTime.UtcNow };
            db.Ambientes.Add(amb);
            db.SaveChanges();
            db.AmbientesPessoas.Add(new AmbientePessoa { AmbienteId = amb.Id, PessoaId = velho.Id });
            db.SaveChanges();

            var job = new InativarUsuariosInativos2AnosJob(db, NullLogger<InativarUsuariosInativos2AnosJob>.Instance);
            job.Executar();

            Assert.Empty(db.AmbientesPessoas.Where(ap => ap.PessoaId == velho.Id).ToList());
        }

        [Fact]
        public void Executar_NaoTocaInativos()
        {
            using var db = CriarContexto();
            SeedConfig(db);
            var p = AdicionarPessoa(db, "JaInativo", "1",
                ultimoAcesso: DateTime.UtcNow.AddYears(-3),
                cadastro: DateTime.UtcNow.AddYears(-5),
                status: "inativo");

            var job = new InativarUsuariosInativos2AnosJob(db, NullLogger<InativarUsuariosInativos2AnosJob>.Instance);
            job.Executar();

            Assert.Equal("inativo", db.Pessoas.Find(p.Id)!.Status);
        }

        [Fact]
        public void Executar_PeriodoMenor_DeveInativarMaisCedo()
        {
            using var db = CriarContexto();
            SeedConfig(db, meses: 3);
            var p = AdicionarPessoa(db, "Recente", "1",
                ultimoAcesso: DateTime.UtcNow.AddMonths(-4),
                cadastro: DateTime.UtcNow.AddYears(-1));

            var job = new InativarUsuariosInativos2AnosJob(db, NullLogger<InativarUsuariosInativos2AnosJob>.Instance);
            job.Executar();

            Assert.Equal("inativo", db.Pessoas.Find(p.Id)!.Status);
        }
    }
}

