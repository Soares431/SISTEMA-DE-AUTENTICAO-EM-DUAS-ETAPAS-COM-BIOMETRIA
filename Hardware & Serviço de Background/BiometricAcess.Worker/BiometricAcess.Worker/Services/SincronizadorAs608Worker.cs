using BiometricAcess.Worker.HardwareNosso;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;

namespace BiometricAcess.Worker.Services
{
    // Drena a fila de slots pendentes de delete no AS608. Quando o admin reseta biometria
    // ou inativa uma pessoa pelo painel, SlotAs608ParaApagar é preenchido — este worker
    // envia CMD|FINGER|DELETE|<slot> ao Arduino e limpa o marcador.
    //
    // Resolução tardia de IAnvizArduinoService via GetService: o serviço só existe quando a
    // OPÇÃO 2 (Arduino real) está ativa no Program.cs. No simulador puro ele não está
    // registrado — então o worker fica idle (não derruba o host).
    public class SincronizadorAs608Worker : BackgroundService
    {
        private readonly ILogger<SincronizadorAs608Worker> _logger;
        private readonly IServiceProvider _provider;
        private readonly IServiceScopeFactory _scopeFactory;

        public SincronizadorAs608Worker(
            ILogger<SincronizadorAs608Worker> logger,
            IServiceProvider provider,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _provider = provider;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var arduinoService = _provider.GetService<IAnvizArduinoService>();
                    if (arduinoService == null)
                    {
                        // Modo simulador ou T50M — não tem Arduino conectado, nada a fazer.
                        await Task.Delay(30000, stoppingToken);
                        continue;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var pessoaRepo = scope.ServiceProvider.GetRequiredService<IPessoaRepository>();

                    var pendentes = await pessoaRepo.ListarSlotsPendentesApagar();
                    foreach (var p in pendentes)
                    {
                        if (p.SlotAs608ParaApagar == null) continue;
                        arduinoService.NotificarApagarDigital(p.SlotAs608ParaApagar.Value);
                        // Limpa otimisticamente — se o sensor falhar, o Arduino emite
                        // EVT|FINGER|FAIL|DELETE (logado) mas o slot permanece "fantasma".
                        // É aceitável: o pool considera ocupado só quem tem SlotAs608 != null
                        // numa pessoa ativa, então o slot fantasma libera no próximo enroll.
                        await pessoaRepo.LimparSlotPendenteApagar(p.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Falha drenando fila de slots AS608: {msg}", ex.Message);
                }

                await Task.Delay(10000, stoppingToken);
            }
        }
    }
}
