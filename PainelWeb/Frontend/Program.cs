// =============================================================================
// Program.cs - Ponto de entrada da aplicação
// Sistema de Controle de Acesso Biométrico do 5° CTA
// =============================================================================

using FrontendControleAcesso.Components;

var builder = WebApplication.CreateBuilder(args);

// Registra os serviços do Blazor Server com componentes interativos (SignalR)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

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
