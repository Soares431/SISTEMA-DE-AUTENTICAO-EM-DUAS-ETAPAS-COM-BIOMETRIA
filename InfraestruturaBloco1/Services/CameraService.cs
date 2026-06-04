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
        // Sempre resolver para absoluto — assim o path salvo em TentativaAcesso.GravacaoPath
        // é resolvível pelo Int1 (que roda de outro diretório) sem ambiguidade.
        _basePath = Path.GetFullPath(basePath);
        Directory.CreateDirectory(_basePath);
        _ffmpegPath = string.IsNullOrWhiteSpace(ffmpegPath) ? "ffmpeg" : ffmpegPath;
        _ffmpegStartTimeoutSeg = 5;
    }

    // Dispara FFmpeg para capturar `duracaoSeg` do stream RTSP da câmera do ambiente.
    // Salva como ambiente_{id}/acesso_{yyyyMMddHHmmss}.mp4 e retorna o path absoluto.
    // SEMPRE retorna um path: se RTSP falhar, gera um vídeo dummy com texto explicativo
    // pra UI nunca ficar sem gravação. O admin vê na thumbnail/playback que algo deu errado.
    public async Task<string?> GravarTrechoRTSP(int ambienteId, DateTime timestamp, int duracaoSeg = 30)
    {
        var cameras = await _cameraRepo.ListarPorAmbiente(ambienteId);
        var camera = cameras.FirstOrDefault(c => c.Ativa && !string.IsNullOrWhiteSpace(c.UrlRTSP));

        string ambientePath = Path.Combine(_basePath, $"ambiente_{ambienteId}");
        Directory.CreateDirectory(ambientePath);

        string filename = $"acesso_{timestamp.ToLocalTime():yyyyMMdd_HHmmss}.mp4";
        string fullPath = Path.Combine(ambientePath, filename);

        // Câmera ausente: gera dummy imediato e retorna.
        if (camera == null)
        {
            return await GerarDummyVideo(fullPath, "SEM CAMERA CONFIGURADA", ambienteId, timestamp);
        }

        var psi = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            // -rtsp_transport tcp: mais confiável que UDP em redes restritas (5º CTA)
            // -stimeout 5000000: 5s timeout de conexão RTSP — sem isso, URL inacessível
            //   ficava travada esperando até o WaitForExit estourar (90s)
            // -t: duração máxima
            // -c:v libx264 -preset veryfast -c:a aac: reencode pra garantir compat. HTML5
            //   -an: sem áudio (várias câmeras IP não têm mic, evita warning de codec)
            // -movflags +faststart: permite playback enquanto baixa
            // -y: sobrescreve se já existir
            Arguments = $"-rtsp_transport tcp -stimeout 5000000 -i \"{camera.UrlRTSP}\" -t {duracaoSeg} " +
                        $"-c:v libx264 -preset veryfast -crf 28 -an -movflags +faststart " +
                        $"-y \"{fullPath}\"",
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            using var proc = Process.Start(psi);
            if (proc == null)
            {
                return await GerarDummyVideo(fullPath, "FFMPEG NAO INICIOU", ambienteId, timestamp);
            }

            // Espera até duracaoSeg + 30s de margem (RTSP demora a estabilizar + reencode + flush).
            bool exited = proc.WaitForExit((duracaoSeg + 30) * 1000);
            if (!exited)
            {
                try { proc.Kill(true); } catch { }
                Console.WriteLine($"[CameraService] FFmpeg excedeu timeout no ambiente {ambienteId}, verificando arquivo parcial...");
                if (File.Exists(fullPath) && new FileInfo(fullPath).Length >= 1024)
                {
                    Console.WriteLine($"[CameraService] Arquivo parcial aproveitado: {fullPath}");
                    return fullPath;
                }
                // Sem arquivo válido — gera dummy explicando o motivo
                return await GerarDummyVideo(fullPath, $"RTSP TIMEOUT - {camera.UrlRTSP}", ambienteId, timestamp);
            }

            if (proc.ExitCode != 0)
            {
                var stderr = await proc.StandardError.ReadToEndAsync();
                Console.WriteLine($"[CameraService] FFmpeg exit {proc.ExitCode} no ambiente {ambienteId}: {stderr.Substring(0, Math.Min(stderr.Length, 500))}");
                if (File.Exists(fullPath) && new FileInfo(fullPath).Length >= 1024)
                {
                    return fullPath;
                }
                return await GerarDummyVideo(fullPath, $"RTSP FALHOU - {camera.UrlRTSP}", ambienteId, timestamp);
            }

            if (!File.Exists(fullPath) || new FileInfo(fullPath).Length < 1024)
            {
                Console.WriteLine($"[CameraService] Arquivo gerado vazio: {fullPath}");
                return await GerarDummyVideo(fullPath, "ARQUIVO VAZIO", ambienteId, timestamp);
            }

            Console.WriteLine($"[CameraService] Gravação concluída: {fullPath} ({new FileInfo(fullPath).Length / 1024} KB)");
            return fullPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CameraService] Erro ao gravar via FFmpeg: {ex.Message}");
            return await GerarDummyVideo(fullPath, $"ERRO - {ex.Message}", ambienteId, timestamp);
        }
    }

    // Gera um vídeo dummy de 5s como fallback quando RTSP falha ou câmera não está configurada.
    // Usa pattern bar (testsrc) — não usa drawtext porque o build comum do FFmpeg pra Windows
    // (gyan.dev) vem sem libfontconfig habilitada e o filtro drawtext falha silenciosamente.
    // Sem texto visual: a UI mostra o motivo via metadata da tentativa (motivoNegacao, etc).
    private async Task<string?> GerarDummyVideo(string fullPath, string motivo, int ambienteId, DateTime timestamp)
    {
        Console.WriteLine($"[CameraService] Gerando dummy ({motivo}) para ambiente {ambienteId}...");

        var psi = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = $"-f lavfi -i \"testsrc=duration=5:size=640x480:rate=25\" " +
                        $"-c:v libx264 -preset veryfast -crf 28 -pix_fmt yuv420p -movflags +faststart -y \"{fullPath}\"",
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            using var proc = Process.Start(psi);
            if (proc == null) return null;
            bool exited = proc.WaitForExit(15000);
            if (!exited) { try { proc.Kill(true); } catch { } return null; }
            if (File.Exists(fullPath) && new FileInfo(fullPath).Length >= 1024)
            {
                Console.WriteLine($"[CameraService] Dummy gerado: {motivo} → {fullPath} ({new FileInfo(fullPath).Length} bytes)");
                return fullPath;
            }
            var stderr = await proc.StandardError.ReadToEndAsync();
            Console.WriteLine($"[CameraService] Falha gerar dummy: exit={proc.ExitCode} stderr={stderr.Substring(0, Math.Min(stderr.Length, 300))}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CameraService] Erro ao gerar dummy: {ex.Message}");
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
