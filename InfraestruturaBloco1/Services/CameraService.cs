using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
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

    /// <summary>
    /// Monitora a pasta de gravações de um ambiente e retorna o path do arquivo gerado.
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

            Thread.Sleep(1000); // espera 1 segundo antes de checar novamente
        }

        return null;
    }

    /// <summary>
    /// Registra log de acesso já com o vídeo associado.
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
            VideoUrl = videoPath // campo do LogAdmin
        };

        await _logRepo.Registrar(log);
    }
}
