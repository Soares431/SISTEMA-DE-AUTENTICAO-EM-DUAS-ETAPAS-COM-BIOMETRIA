using InfraestruturaBloco1.Services;
using Microsoft.EntityFrameworkCore;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions;
using Xunit;

namespace BiometricAcess.Worker.Tests
{
    public class CameraServiceTests : IDisposable
    {
        private readonly string _tempDir;

        public CameraServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "cam_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        private AppDbContext CriarContexto()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;
            var context = new AppDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();
            return context;
        }

        private CameraService CriarService(AppDbContext db)
        {
            var logRepo = new LogAdminImplemetions(db);
            var cameraRepo = new CameraImplemetions(db);
            return new CameraService(logRepo, cameraRepo, _tempDir);
        }

        [Fact]
        public async Task MonitorarNovoArquivo_DiretorioInexistente_RetornaNull()
        {
            using var db = CriarContexto();
            var service = CriarService(db);
            // Ambiente 999 nunca teve diretório criado
            var path = await service.MonitorarNovoArquivo(999, DateTime.UtcNow, tempoEsperaSeg: 1);
            Assert.Null(path);
        }

        [Fact]
        public async Task MonitorarNovoArquivo_QuandoArquivoExiste_RetornaPath()
        {
            using var db = CriarContexto();
            var service = CriarService(db);

            var ambienteId = 42;
            var ambDir = Path.Combine(_tempDir, $"ambiente_{ambienteId}");
            Directory.CreateDirectory(ambDir);
            var arquivo = Path.Combine(ambDir, "video1.mp4");
            await File.WriteAllBytesAsync(arquivo, new byte[] { 1, 2, 3 });

            var timestamp = DateTime.UtcNow.AddSeconds(-5);
            var path = await service.MonitorarNovoArquivo(ambienteId, timestamp, tempoEsperaSeg: 2);

            Assert.NotNull(path);
            Assert.Equal(arquivo, path);
        }

        [Fact]
        public async Task MonitorarNovoArquivo_TimeoutSemArquivo_RetornaNull()
        {
            using var db = CriarContexto();
            var service = CriarService(db);

            var ambienteId = 43;
            Directory.CreateDirectory(Path.Combine(_tempDir, $"ambiente_{ambienteId}"));

            var path = await service.MonitorarNovoArquivo(ambienteId, DateTime.UtcNow, tempoEsperaSeg: 1);
            Assert.Null(path);
        }

        [Fact]
        public async Task MonitorarNovoArquivo_IgnoraArquivosAntigos()
        {
            using var db = CriarContexto();
            var service = CriarService(db);

            var ambienteId = 44;
            var ambDir = Path.Combine(_tempDir, $"ambiente_{ambienteId}");
            Directory.CreateDirectory(ambDir);
            var antigo = Path.Combine(ambDir, "old.mp4");
            await File.WriteAllBytesAsync(antigo, new byte[] { 0 });
            // Marca o arquivo como criado 1h atrás
            File.SetCreationTimeUtc(antigo, DateTime.UtcNow.AddHours(-1));

            // Procura arquivos criados a partir de "agora" — o antigo não deve qualificar
            var path = await service.MonitorarNovoArquivo(ambienteId, DateTime.UtcNow, tempoEsperaSeg: 1);
            Assert.Null(path);
        }
    }
}
