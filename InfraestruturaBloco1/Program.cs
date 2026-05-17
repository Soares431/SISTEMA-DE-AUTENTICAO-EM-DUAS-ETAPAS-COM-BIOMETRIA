using InfraestruturaBloco1.Services;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Banco do Int1
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositórios do Int1 — registrar ANTES dos serviços que dependem deles
builder.Services.AddScoped<ILogAdminRepository, LogAdminImplemetions>();
builder.Services.AddScoped<ISenhaRepository, SenhaImplemetions>();
builder.Services.AddScoped<IPessoaRepository, PessoaImplemetions>();

// Serviços do Int4
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<AesService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<CameraService>(provider =>
    new CameraService(
        provider.GetRequiredService<ILogAdminRepository>(),
        builder.Configuration["CameraBasePath"] ?? "C:\\gravacoes"
    ));

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
app.UseAuthorization();
app.MapControllers();
app.Run();