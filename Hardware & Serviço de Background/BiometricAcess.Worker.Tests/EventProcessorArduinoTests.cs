using BiometricAcess.Worker.HardwareNosso;
using BiometricAcess.Worker.Models;
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
    // Cobre os 4 bugs corrigidos do EventProcessorArduino + fluxos básicos.
    public class EventProcessorArduinoTests
    {
        // Fake do IAnvizArduinoService que captura as notificações para asserção.
        private class FakeArduinoService : IAnvizArduinoService
        {
            public List<string> Notificacoes { get; } = new();
            public void NotificarPedirSenha(int pessoaId) => Notificacoes.Add($"pedirSenha:{pessoaId}");
            public void NotificarPrimeiroAcesso(int pessoaId, int slotAs608) => Notificacoes.Add($"primeiroAcesso:{pessoaId}:slot{slotAs608}");
            public void NotificarVerificarDigital(int pessoaId) => Notificacoes.Add($"verificarDigital:{pessoaId}");
            public void NotificarAcessoNegado(int pessoaId, string motivo) => Notificacoes.Add($"negado:{pessoaId}:{motivo}");
            public void NotificarAcessoLiberado(int duracaoSegundos = 5) => Notificacoes.Add($"acessoLiberado:{duracaoSegundos}s");
            public void NotificarApagarDigital(int slotAs608) => Notificacoes.Add($"apagarDigital:{slotAs608}");
        }

        private const string PortaSerial = "COM3";
        private const string AesKeyTeste = "5cta-aes-key-senha-segura-32char";

        private AppDbContext CriarContexto()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;
            var ctx = new AppDbContext(options);
            ctx.Database.OpenConnection();
            ctx.Database.EnsureCreated();
            ctx.Configuracoes.Add(new Configuracao
            {
                RetencaoGravacoesTentativasDias = 90,
                RetencaoLogsDias = 180,
                TempoEsperaGravacaoSeg = 60
            });
            ctx.SaveChanges();
            return ctx;
        }

        private IConfiguration CriarConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["AesKey"] = AesKeyTeste })
                .Build();
        }

        private (EventProcessorArduino processor, FakeArduinoService arduino, AppDbContext db, Ambiente amb, DispositivoT50 disp) Setup()
        {
            var db = CriarContexto();
            var disp = new DispositivoT50 { Nome = "Arduino", EnderecoIP = PortaSerial, Porta = 0, DigitaisCadastradas = 0 };
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

            var arduino = new FakeArduinoService();
            // O fire-and-forget de gravação ONVIF é assíncrono e não bloqueia o fluxo testado;
            // basta um scope factory que devolva algum scope válido (cria um provider mínimo).
            var sp = new ServiceCollection().BuildServiceProvider();
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var processor = new EventProcessorArduino(
                new PessoaImplemetions(db, CriarConfiguration()),
                new AmbientePessoaImplemetions(db),
                new DispositivoT50Implemetions(db),
                new AmbienteImplementions(db),
                new TentativaAcessoImplemetions(db),
                new ConfiguracaoImplemetions(db),
                arduino,
                CriarConfiguration(),
                scopeFactory
            );
            return (processor, arduino, db, amb, disp);
        }

        private Pessoa CadastrarPessoa(AppDbContext db, string cpf, string senhaClearPlain, string status = "ativo", DateTime? biometria = null)
        {
            // PessoaImplemetions.Adicionar criptografa senhaClear automaticamente
            var repo = new PessoaImplemetions(db, CriarConfiguration());
            var p = repo.Adicionar(new Pessoa
            {
                Nome = "P" + cpf,
                Cpf = cpf,
                Cargo = "C",
                Email = cpf + "@t.com",
                senhaHash = "h",
                senhaClear = senhaClearPlain,
                modoAcesso = "digital_e_senha",
                Status = status,
                biometriaCadastrada = biometria,
                dataCadastro = DateTime.UtcNow
            }).Result;
            return p;
        }

        private void Vincular(AppDbContext db, long pessoaId, int ambienteId)
        {
            db.AmbientesPessoas.Add(new AmbientePessoa { AmbienteId = ambienteId, PessoaId = pessoaId });
            db.SaveChanges();
        }

        // ════════════════════════════════════════════════════════════════
        // Fluxo EVT|ID
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task EvtId_PessoaNaoCadastrada_DeveNegar()
        {
            var (processor, arduino, db, _, _) = Setup();

            await processor.Processar(new EventoAcesso
            {
                PessoaID = 999,
                TipoVerificacao = "id",
                IpDispositivo = PortaSerial,
                DataHora = DateTime.UtcNow
            });

            Assert.Contains("negado:999:nao_cadastrado", arduino.Notificacoes);
            var t = Assert.Single(db.TentativasAcesso.ToList());
            Assert.Equal("nao_cadastrado", t.MotivoNegacao);
            Assert.NotNull(t.DataExpiracao);
        }

        [Fact]
        public async Task EvtId_PessoaInativa_DeveNegar()
        {
            var (processor, arduino, db, amb, _) = Setup();
            var p = CadastrarPessoa(db, "11111111111", "100001", status: "inativo");
            Vincular(db, p.Id, amb.Id);

            await processor.Processar(new EventoAcesso
            {
                PessoaID = (int)p.Id,
                TipoVerificacao = "id",
                IpDispositivo = PortaSerial,
                DataHora = DateTime.UtcNow
            });

            Assert.Contains($"negado:{p.Id}:inativo", arduino.Notificacoes);
        }

        [Fact]
        public async Task EvtId_SemPermissao_DeveNegar()
        {
            var (processor, arduino, db, _, _) = Setup();
            var p = CadastrarPessoa(db, "22222222222", "100002");
            // não vincula ao ambiente

            await processor.Processar(new EventoAcesso
            {
                PessoaID = (int)p.Id,
                TipoVerificacao = "id",
                IpDispositivo = PortaSerial,
                DataHora = DateTime.UtcNow
            });

            Assert.Contains($"negado:{p.Id}:sem_permissao", arduino.Notificacoes);
        }

        [Fact]
        public async Task EvtId_SemBiometria_DevePedirSenha()
        {
            var (processor, arduino, db, amb, _) = Setup();
            var p = CadastrarPessoa(db, "33333333333", "100003");
            Vincular(db, p.Id, amb.Id);

            await processor.Processar(new EventoAcesso
            {
                PessoaID = (int)p.Id,
                TipoVerificacao = "id",
                IpDispositivo = PortaSerial,
                DataHora = DateTime.UtcNow
            });

            Assert.Contains($"pedirSenha:{p.Id}", arduino.Notificacoes);
            // Não registra tentativa ainda — só pede senha
            Assert.Empty(db.TentativasAcesso.ToList());
        }

        [Fact]
        public async Task EvtId_ComBiometria_DevePedirDigital()
        {
            var (processor, arduino, db, amb, _) = Setup();
            var p = CadastrarPessoa(db, "44444444444", "100004", biometria: DateTime.UtcNow);
            Vincular(db, p.Id, amb.Id);

            await processor.Processar(new EventoAcesso
            {
                PessoaID = (int)p.Id,
                TipoVerificacao = "id",
                IpDispositivo = PortaSerial,
                DataHora = DateTime.UtcNow
            });

            Assert.Contains($"verificarDigital:{p.Id}", arduino.Notificacoes);
        }

        // ════════════════════════════════════════════════════════════════
        // Fluxo EVT|SENHA — bugs corrigidos vivem aqui
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task EvtSenha_SenhaCorreta_DevePedirCadastrarBiometria()
        {
            // Bug crítico corrigido: comparação direta com senhaClear cifrado SEMPRE negava.
            // Agora descriptografa antes de comparar.
            var (processor, arduino, db, amb, _) = Setup();
            var p = CadastrarPessoa(db, "55555555555", "100005");
            Vincular(db, p.Id, amb.Id);

            await processor.Processar(new EventoAcesso
            {
                PessoaID = (int)p.Id,
                TipoVerificacao = "senha",
                MotivoNegacao = "100005", // senha em texto claro
                IpDispositivo = PortaSerial,
                DataHora = DateTime.UtcNow
            });

            Assert.Contains(arduino.Notificacoes, s => s.StartsWith($"primeiroAcesso:{p.Id}:"));
        }

        [Fact]
        public async Task EvtSenha_SenhaErrada_DeveNegar()
        {
            var (processor, arduino, db, amb, _) = Setup();
            var p = CadastrarPessoa(db, "66666666666", "100006");
            Vincular(db, p.Id, amb.Id);

            await processor.Processar(new EventoAcesso
            {
                PessoaID = (int)p.Id,
                TipoVerificacao = "senha",
                MotivoNegacao = "999999",
                IpDispositivo = PortaSerial,
                DataHora = DateTime.UtcNow
            });

            Assert.Contains($"negado:{p.Id}:senha_invalida", arduino.Notificacoes);
            var t = Assert.Single(db.TentativasAcesso.ToList());
            Assert.Equal("senha_invalida", t.MotivoNegacao);
            Assert.NotNull(t.DataExpiracao); // DataExpiracao foi preenchida (bug fix)
        }

        [Fact]
        public async Task EvtSenha_PessoaInativa_DeveNegarMesmoComSenhaCorreta()
        {
            // Bug fix: antes, pessoa inativa que digitasse senha conseguia passar.
            var (processor, arduino, db, amb, _) = Setup();
            var p = CadastrarPessoa(db, "77777777777", "100007", status: "inativo");
            Vincular(db, p.Id, amb.Id);

            await processor.Processar(new EventoAcesso
            {
                PessoaID = (int)p.Id,
                TipoVerificacao = "senha",
                MotivoNegacao = "100007",
                IpDispositivo = PortaSerial,
                DataHora = DateTime.UtcNow
            });

            Assert.Contains($"negado:{p.Id}:inativo", arduino.Notificacoes);
            Assert.DoesNotContain(arduino.Notificacoes, s => s.StartsWith($"primeiroAcesso:{p.Id}:"));
        }

        [Fact]
        public async Task EvtSenha_SemPermissao_DeveNegar()
        {
            var (processor, arduino, db, _, _) = Setup();
            var p = CadastrarPessoa(db, "88888888888", "100008");
            // Não vincula

            await processor.Processar(new EventoAcesso
            {
                PessoaID = (int)p.Id,
                TipoVerificacao = "senha",
                MotivoNegacao = "100008",
                IpDispositivo = PortaSerial,
                DataHora = DateTime.UtcNow
            });

            Assert.Contains($"negado:{p.Id}:sem_permissao", arduino.Notificacoes);
        }

        // ════════════════════════════════════════════════════════════════
        // Fluxo digital / primeiro_acesso
        // ════════════════════════════════════════════════════════════════

        [Fact]
        public async Task PrimeiroAcesso_DeveMarcarBiometriaEIncrementarDigitais()
        {
            var (processor, _, db, amb, disp) = Setup();
            var p = CadastrarPessoa(db, "99999999999", "100009");
            Vincular(db, p.Id, amb.Id);
            var dispBefore = disp.DigitaisCadastradas;

            await processor.Processar(new EventoAcesso
            {
                PessoaID = (int)p.Id,
                TipoVerificacao = "primeiro_acesso",
                IpDispositivo = PortaSerial,
                DataHora = DateTime.UtcNow
            });

            var pAtual = await db.Pessoas.FindAsync(p.Id);
            Assert.NotNull(pAtual!.biometriaCadastrada);

            var dispAtual = await db.DispositivosT50.FindAsync(disp.Id);
            Assert.Equal(dispBefore + 1, dispAtual!.DigitaisCadastradas);
        }

        [Fact]
        public async Task Digital_DeveRegistrarTentativaComDataExpiracao()
        {
            // Bug fix: DataExpiracao nunca era preenchida — job de limpeza ignorava tudo.
            var (processor, _, db, amb, _) = Setup();
            var p = CadastrarPessoa(db, "10101010101", "100010", biometria: DateTime.UtcNow);
            Vincular(db, p.Id, amb.Id);

            await processor.Processar(new EventoAcesso
            {
                PessoaID = (int)p.Id,
                TipoVerificacao = "digital",
                IpDispositivo = PortaSerial,
                DataHora = DateTime.UtcNow
            });

            var t = Assert.Single(db.TentativasAcesso.ToList());
            Assert.True(t.AcessoLiberado);
            Assert.NotNull(t.DataExpiracao);
            var dias = (t.DataExpiracao!.Value - DateTime.UtcNow).TotalDays;
            Assert.InRange(dias, 89.9, 90.1);
        }

        [Fact]
        public async Task Digital_DeveResolverAmbientePeloDispositivoT50Id()
        {
            // Bug C2 corrigido (paralelo ao EventProcessor real): antes usava dispositivo.Id como ambienteId.
            // Cria 2 dispositivos com ambientes diferentes — o evento deve cair no ambiente certo.
            var (processor, _, db, amb1, _) = Setup();
            var disp2 = new DispositivoT50 { Nome = "Arduino2", EnderecoIP = "COM4", Porta = 0, DigitaisCadastradas = 0 };
            db.DispositivosT50.Add(disp2);
            db.SaveChanges();
            var amb2 = new Ambiente { Nome = "Sala B", DispositivoT50Id = disp2.Id, TempoEsperaGravacaoSeg = 60, DataCriacao = DateTime.UtcNow };
            db.Ambientes.Add(amb2);
            db.SaveChanges();

            var p = CadastrarPessoa(db, "12121212121", "100011", biometria: DateTime.UtcNow);
            Vincular(db, p.Id, amb1.Id);
            Vincular(db, p.Id, amb2.Id);

            await processor.Processar(new EventoAcesso
            {
                PessoaID = (int)p.Id,
                TipoVerificacao = "digital",
                IpDispositivo = "COM4", // evento do disp2
                DataHora = DateTime.UtcNow
            });

            var t = Assert.Single(db.TentativasAcesso.ToList());
            Assert.Equal(amb2.Id, t.AmbienteId);
        }
    }
}
