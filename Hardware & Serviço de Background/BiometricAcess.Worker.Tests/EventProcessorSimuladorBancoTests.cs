using BiometricAcess.Worker.Models;
using BiometricAcess.Worker.Simulador;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions;
using Xunit;

namespace BiometricAcess.Worker.Tests
{
    // Testa os 5 fluxos do EventProcessorSimuladorBanco contra um SQLite in-memory.
    // O EventProcessor é Singleton mas usa IServiceScopeFactory para resolver Scoped repos por evento.
    public class EventProcessorSimuladorBancoTests : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly string _dbConnString = "DataSource=:memory:;Cache=Shared;Mode=Memory";
        private readonly Microsoft.Data.Sqlite.SqliteConnection _keepAlive;

        public EventProcessorSimuladorBancoTests()
        {
            // SQLite :memory: morre quando a última conexão fecha — segura uma aberta o tempo todo.
            _keepAlive = new Microsoft.Data.Sqlite.SqliteConnection(_dbConnString);
            _keepAlive.Open();

            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(_dbConnString), ServiceLifetime.Scoped);
            services.AddScoped<IPessoaRepository, PessoaImplemetions>();
            services.AddScoped<IAmbienteRepository, AmbienteImplementions>();
            services.AddScoped<IAmbientePessoaRepository, AmbientePessoaImplemetions>();
            services.AddScoped<IDispositivoT50Repository, DispositivoT50Implemetions>();
            services.AddScoped<ITentativaAcessoRepository, TentativaAcessoImplemetions>();
            services.AddScoped<IConfiguracaoRepository, ConfiguracaoImplemetions>();
            services.AddScoped<ILogAdminRepository, LogAdminImplemetions>();
            services.AddScoped<ICameraRepository, CameraImplemetions>();
            services.AddScoped<IAmbienteT50Repository, AmbienteT50Implemetions>();
            services.AddScoped<IPessoaT50Repository, PessoaT50Implemetions>();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

            _provider = services.BuildServiceProvider();

            using var scope = _provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            db.Configuracoes.Add(new Configuracao
            {
                RetencaoGravacoesTentativasDias = 90,
                RetencaoLogsDias = 180,
                TempoEsperaGravacaoSeg = 60
            });
            db.SaveChanges();
        }

        public void Dispose()
        {
            _provider.Dispose();
            _keepAlive.Close();
            _keepAlive.Dispose();
        }

        private (DispositivoT50 disp, Ambiente amb) SeedDispositivoEAmbiente(string ip = "10.0.0.1")
        {
            using var scope = _provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var disp = new DispositivoT50 { Nome = "T50", EnderecoIP = ip, Porta = 5010, DigitaisCadastradas = 0 };
            db.DispositivosT50.Add(disp);
            db.SaveChanges();
            var amb = new Ambiente
            {
                Nome = "Sala",
                DispositivoT50Id = disp.Id,
                TempoEsperaGravacaoSeg = 60,
                DataCriacao = DateTime.UtcNow
            };
            db.Ambientes.Add(amb);
            db.SaveChanges();
            // Multi-T50: cria o vínculo na tabela N-N — o simulador agora resolve ambiente por ela
            db.AmbientesT50.Add(new AmbienteT50
            {
                AmbienteId = amb.Id,
                DispositivoT50Id = disp.Id,
                DataVinculo = DateTime.UtcNow,
                EhPrincipal = true
            });
            db.SaveChanges();
            return (disp, amb);
        }

        private Pessoa SeedPessoa(string cpf, string status = "ativo")
        {
            using var scope = _provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var p = new Pessoa
            {
                Nome = "P" + cpf,
                Cpf = cpf,
                Cargo = "C",
                Email = cpf + "@t.com",
                senhaHash = "h",
                senhaClear = "x",
                modoAcesso = "somente_senha",
                Status = status,
                dataCadastro = DateTime.UtcNow
            };
            db.Pessoas.Add(p);
            db.SaveChanges();
            return p;
        }

        private void VincularAoAmbiente(long pessoaId, int ambienteId)
        {
            using var scope = _provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AmbientesPessoas.Add(new AmbientePessoa { AmbienteId = ambienteId, PessoaId = pessoaId });
            db.SaveChanges();
        }

        private List<TentativaAcesso> ListarTentativas()
        {
            using var scope = _provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return db.TentativasAcesso.ToList();
        }

        [Fact]
        public async Task Processar_PessoaNaoCadastrada_DeveRegistrarNaoCadastrado()
        {
            var (_, amb) = SeedDispositivoEAmbiente();
            var processor = new EventProcessorSimuladorBanco(_provider.GetRequiredService<IServiceScopeFactory>());

            await processor.Processar(new EventoAcesso
            {
                PessoaID = 999, // não existe
                TipoVerificacao = "senha_id",
                DataHora = DateTime.UtcNow,
                IpDispositivo = "10.0.0.1"
            });

            var tentativas = ListarTentativas();
            Assert.Single(tentativas);
            Assert.False(tentativas[0].AcessoLiberado);
            Assert.Equal("nao_cadastrado", tentativas[0].MotivoNegacao);
        }

        [Fact]
        public async Task Processar_PessoaInativa_DeveNegarComMotivoInativo()
        {
            var (_, amb) = SeedDispositivoEAmbiente();
            var p = SeedPessoa("11111111111", status: "inativo");
            var processor = new EventProcessorSimuladorBanco(_provider.GetRequiredService<IServiceScopeFactory>());

            await processor.Processar(new EventoAcesso
            {
                PessoaID = (int)p.Id,
                TipoVerificacao = "senha_id",
                DataHora = DateTime.UtcNow,
                IpDispositivo = "10.0.0.1"
            });

            var t = Assert.Single(ListarTentativas());
            Assert.False(t.AcessoLiberado);
            Assert.Equal("inativo", t.MotivoNegacao);
        }

        [Fact]
        public async Task Processar_PessoaSemPermissaoNoAmbiente_DeveNegarComMotivoSemPermissao()
        {
            var (_, amb) = SeedDispositivoEAmbiente();
            var p = SeedPessoa("22222222222"); // ativo mas sem vínculo
            var processor = new EventProcessorSimuladorBanco(_provider.GetRequiredService<IServiceScopeFactory>());

            await processor.Processar(new EventoAcesso
            {
                PessoaID = (int)p.Id,
                TipoVerificacao = "digital_id",
                DataHora = DateTime.UtcNow,
                IpDispositivo = "10.0.0.1"
            });

            var t = Assert.Single(ListarTentativas());
            Assert.False(t.AcessoLiberado);
            Assert.Equal("sem_permissao", t.MotivoNegacao);
        }

        [Fact]
        public async Task Processar_AcessoLiberado_DeveAtualizarUltimoAcesso()
        {
            var (_, amb) = SeedDispositivoEAmbiente();
            var p = SeedPessoa("33333333333");
            VincularAoAmbiente(p.Id, amb.Id);
            var processor = new EventProcessorSimuladorBanco(_provider.GetRequiredService<IServiceScopeFactory>());

            var antes = DateTime.UtcNow;
            await processor.Processar(new EventoAcesso
            {
                PessoaID = (int)p.Id,
                TipoVerificacao = "digital_id",
                DataHora = antes,
                IpDispositivo = "10.0.0.1"
            });

            var t = Assert.Single(ListarTentativas());
            Assert.True(t.AcessoLiberado);
            Assert.Null(t.MotivoNegacao);

            using var scope = _provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var atual = await db.Pessoas.FindAsync(p.Id);
            Assert.NotNull(atual!.dataUltimoAcesso);
            Assert.True(atual.dataUltimoAcesso >= antes.AddSeconds(-1));
        }

        [Fact]
        public async Task Processar_AcessoLiberado_DevePreencherDataExpiracao()
        {
            var (_, amb) = SeedDispositivoEAmbiente();
            var p = SeedPessoa("44444444444");
            VincularAoAmbiente(p.Id, amb.Id);
            var processor = new EventProcessorSimuladorBanco(_provider.GetRequiredService<IServiceScopeFactory>());

            await processor.Processar(new EventoAcesso
            {
                PessoaID = (int)p.Id,
                TipoVerificacao = "senha_id",
                DataHora = DateTime.UtcNow,
                IpDispositivo = "10.0.0.1"
            });

            var t = Assert.Single(ListarTentativas());
            Assert.NotNull(t.DataExpiracao);
            // ~90 dias (RetencaoGravacoesTentativasDias do seed)
            var dias = (t.DataExpiracao!.Value - DateTime.UtcNow).TotalDays;
            Assert.InRange(dias, 89.9, 90.1);
        }

        [Fact]
        public async Task Processar_AmbienteResolvidoPorIp()
        {
            // Cria 2 dispositivos com IPs diferentes; o evento traz o IP do segundo
            var (_, amb1) = SeedDispositivoEAmbiente("10.0.0.1");
            var (_, amb2) = SeedDispositivoEAmbiente("10.0.0.2");

            var p = SeedPessoa("55555555555");
            VincularAoAmbiente(p.Id, amb2.Id);

            var processor = new EventProcessorSimuladorBanco(_provider.GetRequiredService<IServiceScopeFactory>());
            await processor.Processar(new EventoAcesso
            {
                PessoaID = (int)p.Id,
                TipoVerificacao = "digital_id",
                DataHora = DateTime.UtcNow,
                IpDispositivo = "10.0.0.2"
            });

            var t = Assert.Single(ListarTentativas());
            Assert.Equal(amb2.Id, t.AmbienteId);
            Assert.True(t.AcessoLiberado);
        }
    }
}
