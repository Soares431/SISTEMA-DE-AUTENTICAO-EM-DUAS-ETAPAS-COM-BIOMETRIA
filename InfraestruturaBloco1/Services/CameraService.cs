using WebAbil8_Sistema_Verificação_dupla.slnx.Services;

namespace InfraestruturaBloco1.Services;

public class CameraService
{
    private readonly ILogAdminRepository _logRepo;
    private readonly string _basePath;

    public CameraService(ILogAdminRepository logRepo, string basePath)
    {
        _logRepo = logRepo;
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
        // VideoUrl não existe no LogAdmin ainda — registra sem o vídeo
        _logRepo.Registrar(adminId, acao, entidade, entidadeId);
    }
}