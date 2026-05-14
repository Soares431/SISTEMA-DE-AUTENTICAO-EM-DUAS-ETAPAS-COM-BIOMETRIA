using BCrypt.Net;
using WebAbil8_Sistema_Verificação_dupla.slnx.Configurations;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions;
using Microsoft.AspNetCore.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

// Scoped é usado para criar uma nova instância do serviço para cada solicitação HTTP. Isso é útil para serviços que possuem estado ou que precisam ser isolados por solicitação, como um serviço de pessoa neste caso  
// Scoped é instanciado uma vez por solicitação HTTP
// É injentado a instancia do serviço em toda a solicitação, ou seja, em todos os controladores ou outras classes que dependem dele durante a mesma solicitação.


// 
builder.AddSeriLogLogging();

//builder.Services.AddDatabaseConfiguration(builder.Configuration);
builder.Services.AddDataBaseConfiguration(builder.Configuration);
builder.Services.AddScoped<IPessoaRepository, PessoaImplemetions>();
builder.Services.AddScoped<IAmbienteRepository, AmbienteImplementions>();
builder.Services.AddScoped<IAmbientePessoaRepository, AmbientePessoaImplemetions>();
builder.Services.AddScoped<IDispositivoT50Repository, DispositivoT50Implemetions>();
builder.Services.AddScoped<ITentativaAcessoRepository, TentativaAcessoImplemetions>();
builder.Services.AddScoped<ILogAdminRepository, LogAdminImplemetions>();
builder.Services.AddScoped<ISenhaRepository, SenhaImplemetions>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
