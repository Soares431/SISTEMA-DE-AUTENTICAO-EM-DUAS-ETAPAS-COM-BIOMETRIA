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

    public void RegistrarAcessoComVideo(int adminId, string acao, string entidade, int entidadeId, int ambienteId, DateTime timestamp)
    {
        var videoPath = MonitorarNovoArquivo(ambienteId, timestamp);
        // VideoUrl não existe no LogAdmin — registra sem vídeo, path fica apenas no GravacaoPath da TentativaAcesso
        _logRepo.Registrar(adminId, acao, entidade, entidadeId);
    }

    public async Task<string?> ObterUrlStream(int cameraId)
    {
        var camera = await _cameraRepo.BuscarPorId(cameraId);
        if (camera == null || string.IsNullOrEmpty(camera.UrlRTSP))
            return null;

        return camera.UrlRTSP;
    }
}