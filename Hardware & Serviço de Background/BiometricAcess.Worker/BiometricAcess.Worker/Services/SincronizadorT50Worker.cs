using Microsoft.Extensions.Configuration;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;

namespace BiometricAcess.Worker.Services
{
    // Consome a fila T50Pendencia escrita pelo Frontend (admin adicionou/removeu pessoa
    // de um T50) e executa cada comando no hardware via IAnvizService.
    //
    // §5.2/§5.8 doc técnica: cadastros no T50M acontecem via SDK quando admin opera no painel.
    // Como o Frontend não fala com hardware diretamente, ele só enfileira — este worker é o
    // único consumidor da fila.
    //
    // Roda a cada 10s, processa até 50 pendências por ciclo, marca sucesso ou erro.
    // Pendências com 5 falhas seguidas saem do round-robin (precisa intervenção manual).
    public class SincronizadorT50Worker : BackgroundService
    {
        private readonly ILogger<SincronizadorT50Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAnvizService _anvizService;
        private readonly string _aesKey;

        public SincronizadorT50Worker(
            ILogger<SincronizadorT50Worker> logger,
            IServiceScopeFactory scopeFactory,
            IAnvizService anvizService,
            IConfiguration configuration)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _anvizService = anvizService;
            _aesKey = AesHelper.ResolverChave(configuration);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var pendenciaRepo = scope.ServiceProvider.GetRequiredService<IT50PendenciaRepository>();
                    var pessoaRepo = scope.ServiceProvider.GetRequiredService<IPessoaRepository>();

                    var pendentes = pendenciaRepo.ListarPendentes(50);
                    foreach (var p in pendentes)
                    {
                        try
                        {
                            var pessoa = await pessoaRepo.BuscarPorId(p.PessoaId);
                            if (pessoa == null)
                            {
                                pendenciaRepo.RegistrarErro(p.Id, "Pessoa não encontrada no banco");
                                continue;
                            }

                            var codigoT50 = CodigoT50DePessoa(pessoa);
                            // senhaClear está AES-cifrada no banco — decifra antes de enviar ao T50,
                            // senão o hardware recebe lixo cifrado e o usuário nunca consegue logar com senha+ID.
                            string senhaPlain = "";
                            if (!string.IsNullOrEmpty(pessoa.senhaClear))
                            {
                                try { senhaPlain = AesHelper.Decrypt(pessoa.senhaClear, _aesKey); }
                                catch (Exception ex) { _logger.LogWarning("Falha ao decifrar senha da pessoa {Id}: {Msg}", pessoa.Id, ex.Message); }
                            }
                            bool ok = p.Acao switch
                            {
                                "adicionar" => _anvizService.AdicionarPessoa(codigoT50, pessoa.Nome ?? "", senhaPlain),
                                "remover"   => _anvizService.RemoverPessoa(codigoT50),
                                // Restaura biometria após mudança de modo (somente_senha → digital_e_senha).
                                // Re-cria user com senha + faz upload do template do banco + Mode 6 (FP|PWD).
                                "restaurar_biometria" => RestaurarBiometria(codigoT50, pessoa, senhaPlain),
                                // Limpa biometria após mudança de modo (digital_e_senha → somente_senha).
                                // AlterarModo(senha) = Mode 4 (PWD+ID), mantém user com senha mas tira FP.
                                "limpar_biometria"    => _anvizService.AlterarModo(codigoT50, "senha"),
                                _ => false
                            };

                            if (ok)
                                pendenciaRepo.MarcarSincronizado(p.Id);
                            else
                                pendenciaRepo.RegistrarErro(p.Id, $"AnvizService retornou false para ação '{p.Acao}'");
                        }
                        catch (Exception ex)
                        {
                            pendenciaRepo.RegistrarErro(p.Id, ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha no ciclo de sincronização T50");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private static int CodigoT50DePessoa(WebAbil8_Sistema_Verificação_dupla.slnx.Model.Pessoa pessoa)
        {
            if (!string.IsNullOrEmpty(pessoa.CodigoUsuario) && int.TryParse(pessoa.CodigoUsuario, out var c))
                return c;
            return (int)pessoa.Id;
        }

        private bool RestaurarBiometria(int codigoT50, WebAbil8_Sistema_Verificação_dupla.slnx.Model.Pessoa pessoa, string senhaPlain)
        {
            // Garante que o user existe no T50M com senha — necessário antes do upload do template.
            // Idempotente: se já existe, SetEmployeesData só atualiza.
            if (!_anvizService.AdicionarPessoa(codigoT50, pessoa.Nome ?? "", senhaPlain))
                return false;

            // Sem template no banco: pula upload — caso a pessoa volte pro modo digital_e_senha
            // antes de ter feito o primeiro cadastro biométrico no T50M.
            if (pessoa.templateBackup != null && pessoa.templateBackup.Length > 0)
            {
                if (!_anvizService.UploadTemplate(codigoT50, pessoa.templateBackup))
                    return false; // T50M falhou — tenta de novo no próximo ciclo
            }

            // Mode 6 = FP|PWD = aceita ambos (doc Anviz).
            return _anvizService.AlterarModo(codigoT50, "ambos");
        }
    }
}
