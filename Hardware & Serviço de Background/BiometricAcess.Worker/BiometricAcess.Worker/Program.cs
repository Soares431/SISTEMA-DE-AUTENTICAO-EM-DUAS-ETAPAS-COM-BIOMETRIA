using BiometricAcess.Worker;
using BiometricAcess.Worker.Services;
using InfraestruturaBloco1.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService();

// ═══════════════════════════════════════════════════════════════
// Banco compartilhado com Int1
// Em Docker, DB_PATH aponta pro volume compartilhado (ex.: /data/banco.db).
// ═══════════════════════════════════════════════════════════════
var dbPath = Environment.GetEnvironmentVariable("DB_PATH");
if (string.IsNullOrWhiteSpace(dbPath))
    dbPath = Path.GetFullPath(
        Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..",
        "Banco", "WebAbil8-Sistema_Verificação_dupla.slnx", "banco.db"));
Console.WriteLine($"[INT2 DB] {dbPath}");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddScoped<IPessoaRepository, PessoaImplemetions>();
builder.Services.AddScoped<IAmbienteRepository, AmbienteImplementions>();
builder.Services.AddScoped<IAmbientePessoaRepository, AmbientePessoaImplemetions>();
builder.Services.AddScoped<IDispositivoT50Repository, DispositivoT50Implemetions>();
builder.Services.AddScoped<ITentativaAcessoRepository, TentativaAcessoImplemetions>();
builder.Services.AddScoped<IConfiguracaoRepository, ConfiguracaoImplemetions>();
builder.Services.AddScoped<ILogAdminRepository, LogAdminImplemetions>();
builder.Services.AddScoped<ICameraRepository, CameraImplemetions>();
builder.Services.AddScoped<IAmbienteT50Repository, AmbienteT50Implemetions>();
builder.Services.AddScoped<IPessoaT50Repository, PessoaT50Implemetions>();
builder.Services.AddScoped<IT50PendenciaRepository, T50PendenciaImplemetions>();

// CameraService — consumido pelo EventProcessor T50M via IServiceScopeFactory
// para associar gravações ONVIF a tentativas de acesso liberado (§5.11 doc técnica).
builder.Services.AddScoped<CameraService>();

// ═══════════════════════════════════════════════════════════════
// T50M real (hardware Anviz) — único modo suportado em produção.
//
// IP/porta vêm das variáveis T50M_IP / T50M_PORTA (defaults: 192.168.0.218:5010).
// AnvizConnector é Singleton — uma instância compartilhada por AnvizService
// (que acessa connector.Device) e pelo Worker (que chama Conectar()).
// ═══════════════════════════════════════════════════════════════
var t50Ip = Environment.GetEnvironmentVariable("T50M_IP");
if (string.IsNullOrWhiteSpace(t50Ip)) t50Ip = "192.168.0.218";
var t50PortaStr = Environment.GetEnvironmentVariable("T50M_PORTA");
if (!int.TryParse(t50PortaStr, out var t50Porta) || t50Porta < 1 || t50Porta > 65535) t50Porta = 5010;
Console.WriteLine($"[INT2 T50M] {t50Ip}:{t50Porta}");

builder.Services.AddSingleton<AnvizConnector>(_ => new AnvizConnector(t50Ip, t50Porta));
builder.Services.AddSingleton<IAnvizConnector>(sp => sp.GetRequiredService<AnvizConnector>());
builder.Services.AddSingleton<IAnvizService>(sp => new AnvizService(sp.GetRequiredService<AnvizConnector>()));
builder.Services.AddSingleton<IEventProcessor, EventProcessor>();

builder.Services.AddHostedService<Worker>();

// Sincroniza hora do T50M com o servidor diariamente.
builder.Services.AddHostedService<TimeSyncWorker>();

// Consome a fila T50Pendencia (cadastros/remoções enfileiradas pelo Frontend) e executa
// via Anviz SDK no hardware. §5.2 doc técnica.
builder.Services.AddHostedService<SincronizadorT50Worker>();

var host = builder.Build();

host.Run();
