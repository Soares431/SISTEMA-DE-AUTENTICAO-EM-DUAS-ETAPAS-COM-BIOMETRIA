// =============================================================================
// Program.cs - Ponto de entrada da aplicação
// Sistema de Controle de Acesso Biométrico do 5° CTA
// =============================================================================

using FrontendControleAcesso.Components;
using FrontendControleAcesso.Services;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.EntityFrameworkCore;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions;

var builder = WebApplication.CreateBuilder(args);

// Registra os serviços do Blazor Server com componentes interativos (SignalR)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Banco do Int1 — compartilhado via ProjectReference
// Banco do Int1 — caminho resolvido a partir do executável
var dbPath = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "..",
    "Banco", "WebAbil8-Sistema_Verificação_dupla.slnx", "banco.db"));
Console.WriteLine($"[INT3 DB] {dbPath}");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Repositórios do Int1
builder.Services.AddScoped<IPessoaRepository, PessoaImplemetions>();
builder.Services.AddScoped<IAmbienteRepository, AmbienteImplementions>();
builder.Services.AddScoped<IAmbientePessoaRepository, AmbientePessoaImplemetions>();
builder.Services.AddScoped<ITentativaAcessoRepository, TentativaAcessoImplemetions>();
builder.Services.AddScoped<ILogAdminRepository, LogAdminImplemetions>();
builder.Services.AddScoped<IDispositivoT50Repository, DispositivoT50Implemetions>();
builder.Services.AddScoped<ICameraRepository, CameraImplemetions>();
builder.Services.AddScoped<IConfiguracaoRepository, ConfiguracaoImplemetions>();
builder.Services.AddScoped<ISenhaRepository, SenhaImplemetions>();
builder.Services.AddScoped<ICodigoRepository, CodigoImplemetions>();
builder.Services.AddScoped<IAdministradorRepository, AdministradorImplemetions>();
builder.Services.AddScoped<IAmbienteT50Repository, AmbienteT50Implemetions>();
builder.Services.AddScoped<IPessoaT50Repository, PessoaT50Implemetions>();
builder.Services.AddScoped<IT50PendenciaRepository, T50PendenciaImplemetions>();

// HttpClient para chamar API do Int1 (autenticação JWT)
// Em desenvolvimento: bypassa validação de certificado SSL (certificado dev auto-assinado)
builder.Services.AddHttpClient("BancoAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BancoApiUrl"] ?? "http://localhost:5018/");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Aceita certificado dev auto-assinado (localhost apenas)
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});
builder.Services.AddScoped<ITokenStore, TokenStore>();
builder.Services.AddScoped<CircuitHandler, AuthCircuitHandler>();
var app = builder.Build();

// Migrações de schema são responsabilidade do Int1 (Banco API) — ele sempre sobe
// antes do Frontend (iniciar.ps1 espera porta 5018 responder).

// Configura o pipeline de middleware HTTP
// Na fase de produção, redireciona erros para a página de tratamento de erros
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// Habilita proteção contra Cross-Site Request Forgery nos formulários Blazor
app.UseAntiforgery();

// Habilita o serviço de arquivos estáticos (CSS, JS, imagens) da pasta wwwroot
app.UseStaticFiles();

// Mapeia os componentes Razor (App.razor como raiz) e habilita o modo interativo do servidor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();