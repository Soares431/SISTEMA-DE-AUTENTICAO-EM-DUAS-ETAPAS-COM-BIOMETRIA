using InfraestruturaBloco1.Services;
using Microsoft.EntityFrameworkCore;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions;
using Xunit;

namespace BiometricAcess.Worker.Tests
{
    // Testes do novo CameraService (Bloco D — FFmpeg sob demanda).
    // Os testes não exercitam o FFmpeg em si — apenas os caminhos de guard sem hardware:
    // ambiente sem câmera, câmera sem RTSP. O comportamento real do FFmpeg precisa
    // ser validado em integração (com hardware) e está fora deste suite.
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
            // FFMPEG_PATH inválido — testes não devem chegar a invocar o ffmpeg
            return new CameraService(logRepo, cameraRepo, _tempDir, "ffmpeg-inexistente");
        }

        [Fact]
        public async Task GravarTrechoRTSP_AmbienteSemCamera_RetornaNull()
        {
            using var db = CriarContexto();
            var service = CriarService(db);

            var path = await service.GravarTrechoRTSP(999, DateTime.UtcNow, duracaoSeg: 1);
            Assert.Null(path);
        }

        [Fact]
        public async Task GravarTrechoRTSP_CameraSemRtsp_RetornaNull()
        {
            using var db = CriarContexto();
            db.Ambientes.Add(new Ambiente { Id = 1, Nome = "Amb1", DispositivoT50Id = 1 });
            db.Cameras.Add(new Camera { Id = 10, Nome = "C", UrlRTSP = "", Tipo = "interna", AmbienteId = 1, Ativa = true });
            db.SaveChanges();

            var service = CriarService(db);
            var path = await service.GravarTrechoRTSP(1, DateTime.UtcNow, duracaoSeg: 1);
            Assert.Null(path);
        }

        [Fact]
        public void FfmpegDisponivel_ComBinarioInexistente_RetornaFalse()
        {
            using var db = CriarContexto();
            var service = CriarService(db);
            Assert.False(service.FfmpegDisponivel());
        }

        [Fact]
        public async Task MonitorarNovoArquivo_WrapperLegado_DelegaParaGravarTrecho()
        {
            using var db = CriarContexto();
            var service = CriarService(db);
            // Ambiente inexistente → null, mesmo via wrapper antigo
            var path = await service.MonitorarNovoArquivo(999, DateTime.UtcNow, tempoEsperaSeg: 1);
            Assert.Null(path);
        }
    }
}
