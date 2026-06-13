using Microsoft.Extensions.Logging;
using SharpOnvifClient;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;
using DateTime = System.DateTime;

namespace InfraestruturaBloco1.Services;

public class CameraService
{
    private readonly ICameraRepository _cameraRepo;
    private readonly ILogger<CameraService> _logger;

    public CameraService(ICameraRepository cameraRepo, ILogger<CameraService> logger)
    {
        _cameraRepo = cameraRepo;
        _logger = logger;
    }

    public async Task<string?> ObterUrlStream(int cameraId)
    {
        var camera = await _cameraRepo.BuscarPorId(cameraId);
        if (camera == null || string.IsNullOrEmpty(camera.UrlRTSP))
            return null;
        return camera.UrlRTSP;
    }

    public async Task<string?> MonitorarNovoArquivo(int ambienteId, DateTime timestamp, int tempoEsperaSeg)
    {
        try
        {
            var cameras = await _cameraRepo.ListarPorAmbiente(ambienteId);

            var camera = cameras.FirstOrDefault(c => c.Ativa && c.Tipo == "externa" && !string.IsNullOrEmpty(c.EnderecoONVIF))
                      ?? cameras.FirstOrDefault(c => c.Ativa && !string.IsNullOrEmpty(c.EnderecoONVIF));

            if (camera == null)
            {
                _logger.LogInformation("Ambiente {AmbienteId} sem câmera ONVIF ativa — gravação não associada", ambienteId);
                return null;
            }

            var espera = Math.Clamp(tempoEsperaSeg, 30, 120);
            _logger.LogInformation("Aguardando {Espera}s pela gravação da câmera {CameraId} ({Nome})",
                espera, camera.Id, camera.Nome);
            await Task.Delay(TimeSpan.FromSeconds(espera));

            var (usuario, senha) = ExtrairCredenciais(camera.UrlRTSP);
            var enderecoOnvif = camera.EnderecoONVIF!;

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var client = new SimpleOnvifClient(enderecoOnvif, usuario, senha);
                _ = await client.GetDeviceInformationAsync().WaitAsync(cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Câmera {CameraId} não respondeu ao ONVIF — gravação não associada. {Erro}",
                    camera.Id, ex.Message);
                return null;
            }

            var urlReplay = MontarUrlReplay(camera.UrlRTSP!, timestamp);
            _logger.LogInformation("Gravação associada à tentativa: {Url}", MascararSenha(urlReplay));
            return urlReplay;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao monitorar gravação no ambiente {AmbienteId}", ambienteId);
            return null;
        }
    }

    private static (string usuario, string senha) ExtrairCredenciais(string? urlRtsp)
    {
        if (string.IsNullOrEmpty(urlRtsp)) return ("", "");
        try
        {
            var uri = new Uri(urlRtsp);
            if (string.IsNullOrEmpty(uri.UserInfo)) return ("", "");
            var partes = uri.UserInfo.Split(':', 2);
            return partes.Length == 2 ? (partes[0], partes[1]) : (partes[0], "");
        }
        catch
        {
            return ("", "");
        }
    }

    private static string MontarUrlReplay(string urlRtspBase, DateTime timestamp)
    {
        var uri = new Uri(urlRtspBase);
        var startTime = timestamp.ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
        var endTime = timestamp.AddMinutes(5).ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
        var port = uri.Port > 0 ? uri.Port : 554;
        return $"rtsp://{uri.UserInfo}@{uri.Host}:{port}/Streaming/tracks/101?starttime={startTime}&endtime={endTime}";
    }

    private static string MascararSenha(string url)
    {
        try
        {
            var uri = new Uri(url);
            if (string.IsNullOrEmpty(uri.UserInfo)) return url;
            var partes = uri.UserInfo.Split(':', 2);
            return url.Replace(uri.UserInfo, $"{partes[0]}:****");
        }
        catch { return url; }
    }
}

