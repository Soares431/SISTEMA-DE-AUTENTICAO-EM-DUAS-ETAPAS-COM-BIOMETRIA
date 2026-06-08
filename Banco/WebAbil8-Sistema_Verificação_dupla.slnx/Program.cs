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

// Pasta de gravações compartilhada entre Int1 e Worker — fica na RAIZ do repo
// para que ambos os processos resolvam o mesmo caminho absoluto.
// O Worker (Int2) grava aqui via FFmpeg; o Int1 lê via /api/gravacoes/{id}.
// Sem isso, cada processo usaria "cameras/" relativo ao seu próprio diretório.
var repoRoot = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", ".."));
var cameraBase = Environment.GetEnvironmentVariable("CAMERA_BASE_PATH")
    ?? Path.Combine(repoRoot, "gravacoes");
Environment.SetEnvironmentVariable("CAMERA_BASE_PATH", cameraBase);
Console.WriteLine($"[INT1 GRAVACOES] {cameraBase}");

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
builder.Services.AddScoped<ICodigoRepository, CodigoImplemetions>();
builder.Services.AddScoped<IConfiguracaoRepository, ConfiguracaoImplemetions>();
builder.Services.AddScoped<ICameraRepository, CameraImplemetions>();
builder.Services.AddScoped<IStatusService, StatusServiceImplemetions>();
builder.Services.AddScoped<IAdministradorRepository, AdministradorImplemetions>();
builder.Services.AddScoped<IAmbienteT50Repository, AmbienteT50Implemetions>();
builder.Services.AddScoped<IPessoaT50Repository, PessoaT50Implemetions>();

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

    // Migração inline — coluna urlHLS opcional na câmera para streaming HLS no painel.
    var cameraColsExistentes = new HashSet<string>();
    using (var pragmaCam = conn.CreateCommand())
    {
        pragmaCam.CommandText = "SELECT name FROM pragma_table_info('camera')";
        using var rdrCam = pragmaCam.ExecuteReader();
        while (rdrCam.Read()) cameraColsExistentes.Add(rdrCam.GetString(0));
    }
    if (!cameraColsExistentes.Contains("urlHLS"))
    {
        using var alterCmd = conn.CreateCommand();
        alterCmd.CommandText = "ALTER TABLE camera ADD COLUMN urlHLS VARCHAR(255) NULL";
        alterCmd.ExecuteNonQuery();
    }

    // Migração inline — soft-delete de ambiente preserva histórico de tentativas.
    // Sem isto, deletar um ambiente apagaria também todo o histórico vinculado (FK CASCADE).
    var ambColsExistentes = new HashSet<string>();
    using (var pragmaAmb = conn.CreateCommand())
    {
        pragmaAmb.CommandText = "SELECT name FROM pragma_table_info('ambiente')";
        using var rdrAmb = pragmaAmb.ExecuteReader();
        while (rdrAmb.Read()) ambColsExistentes.Add(rdrAmb.GetString(0));
    }
    if (!ambColsExistentes.Contains("excluido"))
    {
        using var alterCmd = conn.CreateCommand();
        alterCmd.CommandText = "ALTER TABLE ambiente ADD COLUMN excluido INTEGER NOT NULL DEFAULT 0";
        alterCmd.ExecuteNonQuery();
    }
    if (!ambColsExistentes.Contains("dataExclusao"))
    {
        using var alterCmd = conn.CreateCommand();
        alterCmd.CommandText = "ALTER TABLE ambiente ADD COLUMN dataExclusao TEXT NULL";
        alterCmd.ExecuteNonQuery();
    }

    // Migração inline — tabela N-N ambienteT50 para suportar múltiplos T50 por ambiente.
    // Backfill: cada ambiente vira uma linha com seu DispositivoT50Id atual como principal.
    bool ambT50Existe;
    using (var checkCmd = conn.CreateCommand())
    {
        checkCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='ambienteT50'";
        ambT50Existe = checkCmd.ExecuteScalar() != null;
    }
    if (!ambT50Existe)
    {
        using (var createCmd = conn.CreateCommand())
        {
            createCmd.CommandText = @"
                CREATE TABLE ambienteT50 (
                    id               INTEGER PRIMARY KEY AUTOINCREMENT,
                    ambienteId       INTEGER NOT NULL,
                    dispositivoT50Id INTEGER NOT NULL,
                    dataVinculo      TEXT NOT NULL,
                    ehPrincipal      INTEGER NOT NULL DEFAULT 0,
                    UNIQUE(ambienteId, dispositivoT50Id),
                    FOREIGN KEY(ambienteId)       REFERENCES ambiente(id) ON DELETE CASCADE,
                    FOREIGN KEY(dispositivoT50Id) REFERENCES dispositivoT50(id) ON DELETE CASCADE
                )";
            createCmd.ExecuteNonQuery();
        }
    }

    // Backfill defensivo: roda SEMPRE (não só na criação da tabela). Cobre o caso de
    // ambientes pré-existentes que não foram migrados na primeira execução depois do refactor.
    // É idempotente — o WHERE NOT EXISTS evita duplicação.
    // Também ignora ambientes cujo dispositivoT50Id aponta para um T50 que já não existe
    // (FK orfã herdada de excluir T50 sem cascade) — isso travava o startup com SQLite Error 19.
    using (var backfillCmd = conn.CreateCommand())
    {
        backfillCmd.CommandText = @"
            INSERT INTO ambienteT50 (ambienteId, dispositivoT50Id, dataVinculo, ehPrincipal)
            SELECT a.id, a.dispositivoT50Id, datetime('now'), 1
            FROM ambiente a
            INNER JOIN dispositivoT50 d ON d.id = a.dispositivoT50Id
            WHERE a.dispositivoT50Id IS NOT NULL AND a.dispositivoT50Id > 0
              AND NOT EXISTS (SELECT 1 FROM ambienteT50 at WHERE at.ambienteId = a.id AND at.dispositivoT50Id = a.dispositivoT50Id)";
        backfillCmd.ExecuteNonQuery();
    }

    // Ambientes cujo dispositivoT50Id aponta para T50 deletado:
    // (a) se há outro T50 vinculado em ambienteT50, aponta pra ele
    // (b) se não há nenhum, marca como excluido (soft-delete) — não dá pra setar
    //     dispositivoT50Id=NULL porque a coluna é NOT NULL no schema antigo
    using (var fixDangling = conn.CreateCommand())
    {
        fixDangling.CommandText = @"
            UPDATE ambiente
            SET dispositivoT50Id = COALESCE(
                (SELECT at.dispositivoT50Id FROM ambienteT50 at WHERE at.ambienteId = ambiente.id LIMIT 1),
                dispositivoT50Id
            )
            WHERE dispositivoT50Id NOT IN (SELECT id FROM dispositivoT50)";
        fixDangling.ExecuteNonQuery();
    }
    using (var softDeleteOrfaos = conn.CreateCommand())
    {
        softDeleteOrfaos.CommandText = @"
            UPDATE ambiente
            SET excluido = 1, dataExclusao = datetime('now')
            WHERE excluido = 0
              AND dispositivoT50Id NOT IN (SELECT id FROM dispositivoT50)";
        softDeleteOrfaos.ExecuteNonQuery();
    }

    // Limpa gravacaoPath de tentativas cujo arquivo MP4 não existe mais em disco.
    // Só limpa tentativas com mais de 10 minutos — fluxo fire-and-forget do Worker
    // grava arquivo ~60s DEPOIS de inserir a tentativa com path. Se cleanup for muito
    // agressivo, apaga o path antes do Worker terminar de escrever.
    using (var listTentativas = conn.CreateCommand())
    {
        listTentativas.CommandText = @"
            SELECT id, gravacaoPath
            FROM tentativaAcesso
            WHERE gravacaoPath IS NOT NULL AND gravacaoPath != ''
              AND dataHora < datetime('now', '-10 minutes')";
        var paraLimpar = new List<int>();
        using (var rdr = listTentativas.ExecuteReader())
        {
            while (rdr.Read())
            {
                var id = rdr.GetInt32(0);
                var path = rdr.GetString(1);
                if (!File.Exists(path)) paraLimpar.Add(id);
            }
        }
        if (paraLimpar.Count > 0)
        {
            using var clean = conn.CreateCommand();
            clean.CommandText = $"UPDATE tentativaAcesso SET gravacaoPath = NULL WHERE id IN ({string.Join(",", paraLimpar)})";
            clean.ExecuteNonQuery();
            Console.WriteLine($"[INT1 CLEANUP] {paraLimpar.Count} gravacaoPath órfãos limpos (arquivos não existem).");
        }
    }

    // Inverso: apaga arquivos MP4 em disco que não estão referenciados por nenhuma tentativa.
    // Cenário: bug histórico salvava o MP4 mas falhava ao gravar GravacaoPath — arquivos
    // ficavam órfãos consumindo disco. Só apaga se o mtime do arquivo for > 30 minutos,
    // evitando race com Worker que está gravando agora.
    try
    {
        if (Directory.Exists(cameraBase))
        {
            var pathsReferenciados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var listRefs = conn.CreateCommand();
            listRefs.CommandText = "SELECT gravacaoPath FROM tentativaAcesso WHERE gravacaoPath IS NOT NULL AND gravacaoPath != ''";
            using (var rdr = listRefs.ExecuteReader())
            {
                while (rdr.Read())
                {
                    try { pathsReferenciados.Add(Path.GetFullPath(rdr.GetString(0))); } catch { }
                }
            }

            int apagados = 0;
            long bytesLiberados = 0;
            var corteIdade = DateTime.UtcNow.AddMinutes(-30);
            foreach (var mp4 in Directory.EnumerateFiles(cameraBase, "*.mp4", SearchOption.AllDirectories))
            {
                var info = new FileInfo(mp4);
                if (info.LastWriteTimeUtc >= corteIdade) continue; // muito recente — Worker pode estar escrevendo
                if (pathsReferenciados.Contains(info.FullName)) continue; // referenciado, mantém

                try
                {
                    bytesLiberados += info.Length;
                    File.Delete(mp4);
                    apagados++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[INT1 CLEANUP] Falha apagar órfão {mp4}: {ex.Message}");
                }
            }
            if (apagados > 0)
                Console.WriteLine($"[INT1 CLEANUP] {apagados} MP4 órfãos apagados do disco ({bytesLiberados / 1024} KB liberados).");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[INT1 CLEANUP] Erro varredura MP4 órfãos: {ex.Message}");
    }

    // Migração inline — tabela pessoaT50 (qual pessoa está cadastrada em qual T50).
    // Permite múltiplos T50 por ambiente onde admin escolhe em quais a pessoa fica.
    bool pessoaT50Existe;
    using (var checkCmd = conn.CreateCommand())
    {
        checkCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='pessoaT50'";
        pessoaT50Existe = checkCmd.ExecuteScalar() != null;
    }
    if (!pessoaT50Existe)
    {
        using var createCmd = conn.CreateCommand();
        createCmd.CommandText = @"
            CREATE TABLE pessoaT50 (
                id               INTEGER PRIMARY KEY AUTOINCREMENT,
                pessoaId         INTEGER NOT NULL,
                dispositivoT50Id INTEGER NOT NULL,
                dataCadastro     TEXT NOT NULL,
                UNIQUE(pessoaId, dispositivoT50Id),
                FOREIGN KEY(pessoaId)         REFERENCES pessoa(id) ON DELETE CASCADE,
                FOREIGN KEY(dispositivoT50Id) REFERENCES dispositivoT50(id) ON DELETE CASCADE
            )";
        createCmd.ExecuteNonQuery();
    }

    // Backfill defensivo da pessoaT50 — para cada (pessoa, ambiente) em ambiente_pessoa,
    // cadastra a pessoa nos T50s desse ambiente. Idempotente via WHERE NOT EXISTS.
    // Só roda se ambiente_pessoa existir (não é o caso em DB recém-criado pelo EnsureCreated
    // se não houver Pessoa registrada — proteção defensiva).
    bool ambPessoaExiste;
    using (var checkAP = conn.CreateCommand())
    {
        checkAP.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='ambiente_pessoa'";
        ambPessoaExiste = checkAP.ExecuteScalar() != null;
    }
    if (ambPessoaExiste)
    {
        using var backfillP = conn.CreateCommand();
        backfillP.CommandText = @"
            INSERT INTO pessoaT50 (pessoaId, dispositivoT50Id, dataCadastro)
            SELECT DISTINCT ap.pessoaId, at.dispositivoT50Id, datetime('now')
            FROM ambiente_pessoa ap
            INNER JOIN ambienteT50 at ON at.ambienteId = ap.ambienteId
            WHERE NOT EXISTS (
                SELECT 1 FROM pessoaT50 pt
                WHERE pt.pessoaId = ap.pessoaId AND pt.dispositivoT50Id = at.dispositivoT50Id
            )";
        backfillP.ExecuteNonQuery();
    }

    // Re-sincroniza dispositivoT50.DigitaisCadastradas com a contagem real de pessoaT50.
    // Importante após backfill para o contador ficar consistente.
    bool dispExiste;
    using (var checkD = conn.CreateCommand())
    {
        checkD.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='dispositivoT50'";
        dispExiste = checkD.ExecuteScalar() != null;
    }
    if (dispExiste)
    {
        using var sync = conn.CreateCommand();
        sync.CommandText = @"
            UPDATE dispositivoT50
            SET digitaisCadastradas = (
                SELECT COUNT(*) FROM pessoaT50 pt WHERE pt.dispositivoT50Id = dispositivoT50.id
            )";
        sync.ExecuteNonQuery();
    }

    // Migração inline — cria tabela codigoDisponivel se não existir (pool de IDs 100000-999999)
    bool codigoTabelaExiste;
    using (var checkCmd = conn.CreateCommand())
    {
        checkCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='codigoDisponivel'";
        codigoTabelaExiste = checkCmd.ExecuteScalar() != null;
    }
    if (!codigoTabelaExiste)
    {
        using var createCmd = conn.CreateCommand();
        createCmd.CommandText = @"
            CREATE TABLE codigoDisponivel (
                codigo   VARCHAR(6) NOT NULL PRIMARY KEY,
                emUso    INTEGER NOT NULL DEFAULT 0,
                pessoaId INTEGER NULL REFERENCES pessoa(id) ON DELETE SET NULL
            )";
        createCmd.ExecuteNonQuery();
    }

    // Migração inline — adiciona codigoUsuario à pessoa
    var pessoaColsExistentes = new HashSet<string>();
    using (var pragmaCmd4 = conn.CreateCommand())
    {
        pragmaCmd4.CommandText = "SELECT name FROM pragma_table_info('pessoa')";
        using var rdr4 = pragmaCmd4.ExecuteReader();
        while (rdr4.Read()) pessoaColsExistentes.Add(rdr4.GetString(0));
    }
    if (!pessoaColsExistentes.Contains("codigoUsuario"))
    {
        using var alterCmd = conn.CreateCommand();
        alterCmd.CommandText = "ALTER TABLE pessoa ADD COLUMN codigoUsuario VARCHAR(6) NULL";
        alterCmd.ExecuteNonQuery();
    }

    // Índice unique para garantir códigos distintos
    using (var idxCmd = conn.CreateCommand())
    {
        idxCmd.CommandText = "CREATE UNIQUE INDEX IF NOT EXISTS idx_pessoa_codigoUsuario ON pessoa(codigoUsuario)";
        idxCmd.ExecuteNonQuery();
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

    // Pool de CodigoUsuario (ID exibido p/ usuário e usado como EmployeeId no T50M)
    // Mesma faixa 100000-999999, sem bloqueio de "triviais" — ID não é segredo.
    if (!db.CodigosDisponiveis.Any())
    {
        var codigos = Enumerable.Range(100000, 900000)
            .Select(i => new CodigoDisponivel
            {
                Codigo = i.ToString(),
                EmUso = false,
                PessoaId = null
            });
        db.CodigosDisponiveis.AddRange(codigos);
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

// Diagnóstico de variáveis críticas de ambiente — loga warning no startup se faltarem.
// Não bloqueia a subida do serviço; só facilita troubleshooting na entrega ao cliente.
{
    var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    string? VarVazia(string nome) => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(nome)) ? nome : null;
    var smtpFaltando = new[] { "SMTP_HOST", "SMTP_PORT", "SMTP_USER", "SMTP_PASS" }
        .Select(VarVazia).Where(v => v != null).ToList();
    if (smtpFaltando.Any())
        startupLogger.LogWarning("Variáveis SMTP não configuradas: {vars}. Reenvio de credenciais cairá no fallback console.",
            string.Join(", ", smtpFaltando));
    else
        startupLogger.LogInformation("Variáveis SMTP configuradas (host={host}).", Environment.GetEnvironmentVariable("SMTP_HOST"));

    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CAMERA_BASE_PATH")))
        startupLogger.LogWarning("CAMERA_BASE_PATH não configurado — usando './cameras' relativo ao processo.");

    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FFMPEG_PATH")))
        startupLogger.LogWarning("FFMPEG_PATH não configurado — assumindo 'ffmpeg' no PATH do sistema. Sem FFmpeg gravações não serão geradas.");
}

app.UseCors();
app.UseAuthentication(); // ← adicionado
app.UseAuthorization();

app.MapControllers();

app.Run();