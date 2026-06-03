using InfraestruturaBloco1.Services;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions;
using Microsoft.EntityFrameworkCore;
using Hangfire; 
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Banco do Int1
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositórios do Int1 — registrar ANTES dos serviços que dependem deles
builder.Services.AddScoped<ILogAdminRepository, LogAdminImplemetions>();
builder.Services.AddScoped<ISenhaRepository, SenhaImplemetions>();
builder.Services.AddScoped<IPessoaRepository, PessoaImplemetions>();
builder.Services.AddScoped<ICameraRepository, CameraImplemetions>();

// Serviços do Int4
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<AesService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<CameraService>(provider =>
    new CameraService(
        provider.GetRequiredService<ILogAdminRepository>(),
        provider.GetRequiredService<ICameraRepository>(),
        builder.Configuration["CameraBasePath"] ?? "C:\\gravacoes",
        Environment.GetEnvironmentVariable("FFMPEG_PATH")
    ));
// Serviços do Bloco 4 — Relatórios
builder.Services.AddScoped<RelatorioAmbienteService>();
builder.Services.AddScoped<ExportService>();

// Configuração do Hangfire sem SQLite
builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddHangfireServer(); 

// Configuração JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

// Controllers e Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Infraestrutura Bloco 1 API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Dashboard do Hangfire em /hangfire
app.UseHangfireDashboard("/hangfire");

// Mapear controllers
app.MapControllers();

// Exemplo de job recorrente (limpeza de logs)
RecurringJob.AddOrUpdate("limpeza-logs",
    () => Console.WriteLine("Executando limpeza de logs..."),
    Cron.Daily);

app.Run();
