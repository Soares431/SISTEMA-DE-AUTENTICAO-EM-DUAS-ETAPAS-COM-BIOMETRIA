// =============================================================================
// Program.cs - Ponto de entrada da aplicação
// Sistema de Controle de Acesso Biométrico do 5° CTA
// =============================================================================

using FrontendControleAcesso.Components;
using FrontendControleAcesso.Services;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.EntityFrameworkCore;
using System.Data;
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
builder.Services.AddScoped<IAdministradorRepository, AdministradorImplemetions>();

// HttpClient para chamar API do Int1 (autenticação JWT)
builder.Services.AddHttpClient("BancoAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BancoApiUrl"] ?? "https://localhost:7117/");
});
builder.Services.AddScoped<ITokenStore, TokenStore>();
builder.Services.AddScoped<CircuitHandler, AuthCircuitHandler>();
var app = builder.Build();

// Migração inline — garante que colunas novas existam no banco antes de qualquer request
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    var conn = db.Database.GetDbConnection();
    if (conn.State == ConnectionState.Closed) conn.Open();

    // administrador: cpf, email, cargo, telefone
    var adminCols = new HashSet<string>();
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "SELECT name FROM pragma_table_info('administrador')";
        using var r = cmd.ExecuteReader();
        while (r.Read()) adminCols.Add(r.GetString(0));
    }
    foreach (var (col, def) in new[] {
        ("cpf", "cpf VARCHAR(15)"),
        ("email", "email VARCHAR(150)"),
        ("cargo", "cargo VARCHAR(100)"),
        ("telefone", "telefone VARCHAR(20)")
    })
    {
        if (!adminCols.Contains(col))
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"ALTER TABLE administrador ADD COLUMN {def}";
            cmd.ExecuteNonQuery();
        }
    }

    // configuracao: periodoInativacaoMeses
    var configCols = new HashSet<string>();
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "SELECT name FROM pragma_table_info('configuracao')";
        using var r = cmd.ExecuteReader();
        while (r.Read()) configCols.Add(r.GetString(0));
    }
    if (!configCols.Contains("periodoInativacaoMeses"))
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "ALTER TABLE configuracao ADD COLUMN periodoInativacaoMeses INTEGER NOT NULL DEFAULT 24";
        cmd.ExecuteNonQuery();
    }
}

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