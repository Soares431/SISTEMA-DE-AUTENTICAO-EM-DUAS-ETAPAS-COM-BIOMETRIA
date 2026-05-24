using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;

namespace InfraestruturaBloco1.Services;

public class CameraService
{
    private readonly ILogAdminRepository _logRepo;
    private readonly ICameraRepository _cameraRepo;
    private readonly string _basePath;

    public CameraService(ILogAdminRepository logRepo, ICameraRepository cameraRepo, string basePath)
    {
        _logRepo = logRepo;
        _cameraRepo = cameraRepo;
        _basePath = basePath;
    }

    /// <summary>
    /// Monitora a pasta de gravações e retorna o path do arquivo gerado.
    /// </summary>
    public string? MonitorarNovoArquivo(int ambienteId, DateTime timestamp, int tempoEsperaSeg = 30)
    {
        string ambientePath = Path.Combine(_basePath, $"ambiente_{ambienteId}");
        if (!Directory.Exists(ambientePath))
            return null;

        DateTime limite = DateTime.UtcNow.AddSeconds(tempoEsperaSeg);

        while (DateTime.UtcNow < limite)
        {
            var arquivos = Directory.GetFiles(ambientePath, "*.mp4")
                .Select(f => new FileInfo(f))
                .Where(f => f.CreationTimeUtc >= timestamp)
                .OrderBy(f => f.CreationTimeUtc)
                .ToList();

            if (arquivos.Any())
                return arquivos.First().FullName;

            Thread.Sleep(1000);
        }

        return null;
    }

    /// <summary>
    /// Registra log de acesso com vídeo associado.
    /// </summary>
    public async Task RegistrarAcessoComVideoAsync(int adminId, string acao, string entidade, int entidadeId, int ambienteId, DateTime timestamp)
    {
        var videoPath = MonitorarNovoArquivo(ambienteId, timestamp);

        var log = new LogAdmin
        {
            AdminId = adminId,
            Acao = acao,
            EntidadeAfetada = entidade,
            EntidadeId = entidadeId,
            DataHora = DateTime.UtcNow,
            DataExpiracao = DateTime.UtcNow.AddDays(180),
            VideoUrl = videoPath
        };

        await _logRepo.Registrar(log);
    }

    /// <summary>
    /// Retorna a URL RTSP da câmera para streaming ao vivo.
    /// </summary>
    public async Task<string?> ObterUrlStream(int cameraId)
    {
        var camera = await _cameraRepo.BuscarPorId(cameraId);
        if (camera == null || string.IsNullOrEmpty(camera.RtspUrl))
            return null;

        return camera.RtspUrl;
    }
}
