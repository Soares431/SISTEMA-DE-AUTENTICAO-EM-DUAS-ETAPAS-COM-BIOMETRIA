using BCrypt.Net;
using Hangfire;
using Microsoft.EntityFrameworkCore;
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

// Banco — força caminho absoluto na pasta do projeto Int1
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "banco.db");
builder.Configuration["SQLiteConnection:SQLiteConnectionString"] = $"Data Source={dbPath}";
Console.WriteLine($"[INT1 DB] {dbPath}");

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
builder.Services.AddScoped<IAdministradorRepository, AdministradorImplemetions>();

builder.Services.AddScoped<InativarUsuariosInativos2AnosJob>();
builder.Services.AddScoped<LimparDadosExpiradosJob>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS — permite que o Frontend (porta 8080) faça fetch autenticado para esta API (porta 5018).
// Sem isso, downloads de PDF feitos pelo JS interop falham com erro CORS no browser.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p => p
        .WithOrigins("http://localhost:8080")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

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

    // Migração inline — adiciona colunas novas ao administrador sem perder dados existentes
    var conn = db.Database.GetDbConnection();
    if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
    var colsExistentes = new HashSet<string>();
    using (var pragmaCmd = conn.CreateCommand())
    {
        pragmaCmd.CommandText = "SELECT name FROM pragma_table_info('administrador')";
        using var rdr = pragmaCmd.ExecuteReader();
        while (rdr.Read()) colsExistentes.Add(rdr.GetString(0));
    }
    foreach (var (col, def) in new[] {
        ("cpf", "cpf VARCHAR(15)"),
        ("email", "email VARCHAR(150)"),
        ("cargo", "cargo VARCHAR(100)"),
        ("telefone", "telefone VARCHAR(20)")
    })
    {
        if (!colsExistentes.Contains(col))
        {
            using var alterCmd = conn.CreateCommand();
            alterCmd.CommandText = $"ALTER TABLE administrador ADD COLUMN {def}";
            alterCmd.ExecuteNonQuery();
        }
    }

    // Migração inline — adiciona periodoInativacaoMeses à configuracao
    var configColsExistentes = new HashSet<string>();
    using (var pragmaCmd2 = conn.CreateCommand())
    {
        pragmaCmd2.CommandText = "SELECT name FROM pragma_table_info('configuracao')";
        using var rdr2 = pragmaCmd2.ExecuteReader();
        while (rdr2.Read()) configColsExistentes.Add(rdr2.GetString(0));
    }
    if (!configColsExistentes.Contains("periodoInativacaoMeses"))
    {
        using var alterCmd = conn.CreateCommand();
        alterCmd.CommandText = "ALTER TABLE configuracao ADD COLUMN periodoInativacaoMeses INTEGER NOT NULL DEFAULT 24";
        alterCmd.ExecuteNonQuery();
    }

    // Migração inline — adiciona ultimaConexao ao dispositivoT50 (status online/offline)
    var dispColsExistentes = new HashSet<string>();
    using (var pragmaCmd3 = conn.CreateCommand())
    {
        pragmaCmd3.CommandText = "SELECT name FROM pragma_table_info('dispositivoT50')";
        using var rdr3 = pragmaCmd3.ExecuteReader();
        while (rdr3.Read()) dispColsExistentes.Add(rdr3.GetString(0));
    }
    if (!dispColsExistentes.Contains("ultimaConexao"))
    {
        using var alterCmd = conn.CreateCommand();
        alterCmd.CommandText = "ALTER TABLE dispositivoT50 ADD COLUMN ultimaConexao TEXT NULL";
        alterCmd.ExecuteNonQuery();
    }

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
    // Roda uma vez por dia às 03:00 UTC conforme especificado na doc técnica
    RecurringJob.AddOrUpdate<InativarUsuariosInativos2AnosJob>(
        "inativar-usuarios-inativos",
        job => job.Executar(),
        "0 3 * * *");

    RecurringJob.AddOrUpdate<LimparDadosExpiradosJob>(
        "limpar-dados-expirados",
        job => job.Executar(),
        "0 3 * * *");
}

// UseHttpsRedirection removido — Int1 é API interna (localhost apenas).
// O redirect HTTP→HTTPS fazia o HttpClient do Int3 falhar no certificado dev.

app.UseCors();
app.UseAuthentication(); // ← adicionado
app.UseAuthorization();

app.MapControllers();

app.Run();