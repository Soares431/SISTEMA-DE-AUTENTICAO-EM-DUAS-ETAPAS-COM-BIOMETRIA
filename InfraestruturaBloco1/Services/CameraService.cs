using System.Diagnostics;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;

namespace InfraestruturaBloco1.Services;

public class CameraService
{
    private readonly ILogAdminRepository _logRepo;
    private readonly ICameraRepository _cameraRepo;
    private readonly string _basePath;
    private readonly string _ffmpegPath;
    private readonly int _ffmpegStartTimeoutSeg;

    public CameraService(ILogAdminRepository logRepo, ICameraRepository cameraRepo, string basePath, string? ffmpegPath = null)
    {
        _logRepo = logRepo;
        _cameraRepo = cameraRepo;
        _basePath = basePath;
        _ffmpegPath = string.IsNullOrWhiteSpace(ffmpegPath) ? "ffmpeg" : ffmpegPath;
        _ffmpegStartTimeoutSeg = 5;
    }

    // Dispara FFmpeg para capturar `duracaoSeg` do stream RTSP da câmera do ambiente.
    // Salva como ambiente_{id}/acesso_{yyyyMMddHHmmss}.mp4 e retorna o path absoluto.
    // Retorna null se o ambiente não tem câmera ativa com RTSP, ou se o FFmpeg falhar.
    public async Task<string?> GravarTrechoRTSP(int ambienteId, DateTime timestamp, int duracaoSeg = 30)
    {
        var cameras = await _cameraRepo.ListarPorAmbiente(ambienteId);
        var camera = cameras.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.UrlRTSP));
        if (camera == null) return null;

        string ambientePath = Path.Combine(_basePath, $"ambiente_{ambienteId}");
        Directory.CreateDirectory(ambientePath);

        string filename = $"acesso_{timestamp.ToLocalTime():yyyyMMdd_HHmmss}.mp4";
        string fullPath = Path.Combine(ambientePath, filename);

        var psi = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            // -rtsp_transport tcp: mais confiável que UDP em redes restritas (5º CTA)
            // -t: duração máxima
            // -c:v libx264 -preset veryfast -c:a aac: reencode pra garantir compat. HTML5
            // -movflags +faststart: permite playback enquanto baixa
            // -y: sobrescreve se já existir
            Arguments = $"-rtsp_transport tcp -i \"{camera.UrlRTSP}\" -t {duracaoSeg} " +
                        $"-c:v libx264 -preset veryfast -crf 28 -c:a aac -movflags +faststart " +
                        $"-y \"{fullPath}\"",
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            using var proc = Process.Start(psi);
            if (proc == null) return null;

            // Espera até duracaoSeg + 5s de margem (conexão RTSP demora)
            bool exited = proc.WaitForExit((duracaoSeg + 10) * 1000);
            if (!exited)
            {
                try { proc.Kill(true); } catch { }
                Console.WriteLine($"[CameraService] FFmpeg excedeu timeout no ambiente {ambienteId}");
                return null;
            }

            if (proc.ExitCode != 0)
            {
                var stderr = await proc.StandardError.ReadToEndAsync();
                Console.WriteLine($"[CameraService] FFmpeg exit {proc.ExitCode} no ambiente {ambienteId}: {stderr.Substring(0, Math.Min(stderr.Length, 500))}");
                return null;
            }

            if (!File.Exists(fullPath) || new FileInfo(fullPath).Length < 1024)
            {
                Console.WriteLine($"[CameraService] Arquivo gerado vazio ou inexistente: {fullPath}");
                return null;
            }

            return fullPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CameraService] Erro ao gravar via FFmpeg: {ex.Message}");
            return null;
        }
    }

    // Compat: chamadores antigos. Encaminha pro GravarTrechoRTSP — agora é proativo, não fica vigiando pasta.
    public async Task<string?> MonitorarNovoArquivo(int ambienteId, DateTime timestamp, int tempoEsperaSeg = 30)
    {
        return await GravarTrechoRTSP(ambienteId, timestamp, tempoEsperaSeg);
    }

    public async Task<string?> ObterUrlStream(int cameraId)
    {
        var camera = await _cameraRepo.BuscarPorId(cameraId);
        if (camera == null || string.IsNullOrEmpty(camera.UrlRTSP))
            return null;
        return camera.UrlRTSP;
    }

    // Valida que o FFmpeg está disponível. Roda uma vez no startup.
    public bool FfmpegDisponivel()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = "-version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null) return false;
            bool exited = proc.WaitForExit(_ffmpegStartTimeoutSeg * 1000);
            return exited && proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
