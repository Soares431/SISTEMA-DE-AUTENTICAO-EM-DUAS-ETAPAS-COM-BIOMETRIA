using Microsoft.OpenApi.Models;
using InfraestruturaBloco1.Services;

var builder = WebApplication.CreateBuilder(args);

// Registrar serviços no container de injeção de dependência
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<AesService>();

// Adicionar suporte a controllers e Swagger/OpenAPI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Infraestrutura Bloco 1 API",
        Version = "v1",
        Description = "API para serviços de hash de senha, envio de email e criptografia AES",
        Contact = new OpenApiContact
        {
            Name = "Equipe AdminSoftware",
            Email = "suporte@adminsoftware.com",
            Url = new Uri("https://adminsoftware.com")
        }
    });
});

var app = builder.Build();

// Configuração do pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Infraestrutura Bloco 1 API v1");
        c.RoutePrefix = string.Empty; // abre o Swagger direto na raiz (http://localhost:5111)
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Mapear controllers automaticamente
app.MapControllers();

app.Run();
