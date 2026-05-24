using BCrypt.Net;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WebAbil8_Sistema_Verificação_dupla.slnx.Configurations;
using WebAbil8_Sistema_Verificação_dupla.slnx.Jobs;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Scoped é usado para criar uma nova instância do serviço para cada solicitação HTTP. Isso é útil para serviços que possuem estado ou que precisam ser isolados por solicitação, como um serviço de pessoa neste caso  
// Scoped é instanciado uma vez por solicitação HTTP
// É injentado a instancia do serviço em toda a solicitação, ou seja, em todos os controladores ou outras classes que dependem dele durante a mesma solicitação.

builder.AddSeriLogLogging();

builder.Services.AddControllers();

// JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

builder.Services.AddHangfire(config =>
    config.UseMemoryStorage()); // ou UseStorage para persistir os jobs

builder.Services.AddHangfireServer();

//builder.Services.AddDatabaseConfiguration(builder.Configuration);
builder.Services.AddDataBaseConfiguration(builder.Configuration);
builder.Services.AddScoped<IPessoaRepository, PessoaImplemetions>();
builder.Services.AddScoped<IAmbienteRepository, AmbienteImplementions>();
builder.Services.AddScoped<IAmbientePessoaRepository, AmbientePessoaImplemetions>();
builder.Services.AddScoped<IDispositivoT50Repository, DispositivoT50Implemetions>();
builder.Services.AddScoped<ITentativaAcessoRepository, TentativaAcessoImplemetions>();
builder.Services.AddScoped<ILogAdminRepository, LogAdminImplemetions>();
builder.Services.AddScoped<ISenhaRepository, SenhaImplemetions>();
builder.Services.AddScoped<IConfiguracaoRepository, ConfiguracaoImplemetions>();
builder.Services.AddScoped<ICameraRepository, CameraImplemetions>();
builder.Services.AddScoped<IStatusService, StatusServiceImplemetions>();

builder.Services.AddScoped<InativarUsuariosInativos2AnosJob>();
builder.Services.AddScoped<LimparDadosExpiradosJob>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ Build() APENAS aqui, depois de todos os serviços registrados
var app = builder.Build();

app.UseHangfireDashboard("/hangfire");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.SenhasDisponiveis.Any())
    {
        var triviais = new HashSet<string> {
            "123456", "654321", "111111", "222222", "333333",
            "444444", "555555", "666666", "777777", "888888",
            "999999", "123123", "321321", "112233"
        };

        var senhas = Enumerable.Range(100000, 900000)
            .Select(i => new SenhaDisponivel
            {
                Senha = i.ToString(),
                EmUso = triviais.Contains(i.ToString()),
                PessoaId = null
            });

        db.SenhasDisponiveis.AddRange(senhas);
        db.SaveChanges();
    }

    // Fora do if anterior
    if (!db.Administradores.Any())
    {
        db.Administradores.Add(new Administrador
        {
            Login = "admin",
            SenhaHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            NomeCompleto = "Administrador Padrão",
            DataCriacao = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    if (!db.Configuracoes.Any())
    {
        db.Configuracoes.Add(new Configuracao
        {
            RetencaoGravacoesTentativasDias = 90,
            RetencaoLogsDias = 180,
            TempoEsperaGravacaoSeg = 60
        });
        db.SaveChanges();
    }
}

using (var scope = app.Services.CreateScope())
{
    // Roda uma vez por dia às meia-noite
    RecurringJob.AddOrUpdate<InativarUsuariosInativos2AnosJob>(
        "inativar-usuarios-inativos",
        job => job.Executar(),
        Cron.Daily);

    RecurringJob.AddOrUpdate<LimparDadosExpiradosJob>(
        "limpar-dados-expirados",
        job => job.Executar(),
        Cron.Daily);
}

app.UseHttpsRedirection();

app.UseAuthentication(); // ← adicionado
app.UseAuthorization();

app.MapControllers();

app.Run();