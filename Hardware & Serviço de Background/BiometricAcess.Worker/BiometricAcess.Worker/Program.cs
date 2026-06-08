using BiometricAcess.Worker;
using BiometricAcess.Worker.Services;
using BiometricAcess.Worker.Simulador;
using BiometricAcess.Worker.HardwareNosso;
using BiometricAcess.Worker.HardwareNosso.Simulador;
using InfraestruturaBloco1.Services;
using Microsoft.EntityFrameworkCore;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService();

// ═══════════════════════════════════════════════════════════════
// Banco compartilhado com Int1
// ═══════════════════════════════════════════════════════════════
var dbPath = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..",
    "Banco", "WebAbil8-Sistema_Verificação_dupla.slnx", "banco.db"));
Console.WriteLine($"[INT2 DB] {dbPath}");

// Pasta de gravações: mesma do Int1 — raiz do repo / "gravacoes".
// Sem isso, o Worker salvaria em BiometricAcess.Worker/cameras/ e o Int1
// procuraria em Banco/.../cameras/ — paths divergentes, API não encontra.
var repoRoot = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", ".."));
var cameraBase = Environment.GetEnvironmentVariable("CAMERA_BASE_PATH")
    ?? Path.Combine(repoRoot, "gravacoes");
Environment.SetEnvironmentVariable("CAMERA_BASE_PATH", cameraBase);
Console.WriteLine($"[INT2 GRAVACOES] {cameraBase}");

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

// ═══════════════════════════════════════════════════════════════
// Serviços compartilhados
// ═══════════════════════════════════════════════════════════════
builder.Services.AddScoped<CameraService>(sp =>
    new CameraService(
        sp.GetRequiredService<ILogAdminRepository>(),
        sp.GetRequiredService<ICameraRepository>(),
        // Já resolvido para absoluto acima (compartilhado Int1↔Int2)
        Environment.GetEnvironmentVariable("CAMERA_BASE_PATH") ?? "gravacoes",
        Environment.GetEnvironmentVariable("FFMPEG_PATH")
    ));

// ═══════════════════════════════════════════════════════════════
// OPÇÃO 1 — Simulador falso (sem banco, apenas console)
// ═══════════════════════════════════════════════════════════════
//builder.Services.AddSingleton<IEventProcessor, EventProcessorSimulador>();

// ═══════════════════════════════════════════════════════════════
// OPÇÃO 1B — Simulador com banco real (padrão para demonstração)
// Grava TentativasAcesso no SQLite — o painel exibe os dados em tempo real
// Requer pelo menos um Ambiente cadastrado no painel
// ═══════════════════════════════════════════════════════════════
builder.Services.AddSingleton<IAnvizConnector, AnvizConnectorSimulador>();
builder.Services.AddSingleton<IAnvizService, AnvizServiceSimulador>();
builder.Services.AddSingleton<IEventProcessor, EventProcessorSimuladorBanco>();

// ═══════════════════════════════════════════════════════════════
// ═══════════════════════════════════════════════════════════════
// OPÇÃO 2 — Nosso Arduino (hardware físico)
// Antes de ativar: ajuste a porta COM no ArduinoConnector
// ═══════════════════════════════════════════════════════════════

//var arduinoConnector = new ArduinoConnector(porta: "COM3"); // ← ajuste a porta
//builder.Services.AddSingleton<IAnvizConnector>(arduinoConnector);
//builder.Services.AddSingleton<IAnvizService>(new ArduinoService(arduinoConnector));
//builder.Services.AddSingleton<IAnvizArduinoService>(new ArduinoServiceExtras(arduinoConnector));

// OPÇÃO 2A — banco vazio / dados mockados
//builder.Services.AddSingleton<IEventProcessor, EventProcessorArduinoSimulador>();

// OPÇÃO 2B — banco real
//builder.Services.AddSingleton<IEventProcessor, EventProcessorArduino>();

// ═══════════════════════════════════════════════════════════════

// OPÇÃO 3 — T50M real (hardware Anviz)
//builder.Services.AddSingleton<IAnvizConnector, AnvizConnector>();
//builder.Services.AddSingleton<IAnvizService, AnvizService>();
//builder.Services.AddSingleton<IEventProcessor, EventProcessor>();

// ═══════════════════════════════════════════════

builder.Services.AddHostedService<Worker>();

// Sincroniza hora do T50M com o servidor diariamente (no-op no simulador).
builder.Services.AddHostedService<TimeSyncWorker>();

var host = builder.Build();

// Verifica FFmpeg na inicialização e printa status visível no console.
// Sem FFmpeg, GravarTrechoRTSP retorna null silenciosamente e nenhuma tentativa
// fica com GravacaoPath preenchido.
using (var scope = host.Services.CreateScope())
{
    var cameraService = scope.ServiceProvider.GetRequiredService<CameraService>();
    var ffmpegOk = cameraService.FfmpegDisponivel();
    var camBase = Path.GetFullPath(Environment.GetEnvironmentVariable("CAMERA_BASE_PATH") ?? "cameras");
    Console.WriteLine("");
    Console.WriteLine("═══════════════════════════════════════════════════════════════");
    Console.WriteLine($"  FFmpeg disponível: {(ffmpegOk ? "SIM ✓" : "NÃO ✗ — gravações NÃO serão geradas")}");
    Console.WriteLine($"  Pasta das gravações: {camBase}");
    if (!ffmpegOk)
    {
        Console.WriteLine("  → Defina FFMPEG_PATH apontando para o ffmpeg.exe, ou");
        Console.WriteLine("    coloque a pasta bin do FFmpeg no PATH do sistema.");
    }

    // Avisa sobre RTSP/MediaMTX para URLs localhost — causa #1 de "gravações virando dummy"
    // quando o teste do user usa webcam local servida por MediaMTX e o servidor não está rodando.
    try
    {
        var cameraRepo = scope.ServiceProvider.GetRequiredService<ICameraRepository>();
        var todasCams = await cameraRepo.ListarComFiltros(null, true);
        var camsLocal = todasCams.Where(c => !string.IsNullOrWhiteSpace(c.UrlRTSP)
                                          && (c.UrlRTSP.Contains("localhost") || c.UrlRTSP.Contains("127.0.0.1")))
                                 .ToList();
        if (camsLocal.Count > 0)
        {
            Console.WriteLine("  ───────────────────────────────────────────────────────────");
            Console.WriteLine($"  Câmeras com URL localhost detectadas: {camsLocal.Count}");
            foreach (var c in camsLocal)
                Console.WriteLine($"    • {c.Nome}: {c.UrlRTSP}");
            Console.WriteLine("  ATENÇÃO: gravações reais só funcionam se houver um servidor");
            Console.WriteLine("  RTSP local (ex: MediaMTX) escutando essa porta. Sem ele, todas");
            Console.WriteLine("  as gravações virarão vídeo dummy (pattern bar).");
            Console.WriteLine("  Suba o MediaMTX antes pra capturar a webcam de verdade.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  (Falha ao listar câmeras para diagnóstico: {ex.Message})");
    }
    Console.WriteLine("═══════════════════════════════════════════════════════════════");
    Console.WriteLine("");
}

host.Run();