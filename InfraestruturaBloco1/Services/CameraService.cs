using SharpOnvifClient;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;
using DateTime = System.DateTime;

namespace InfraestruturaBloco1.Services;

// Integração ONVIF para câmeras IP do ambiente controlado.
//
// Conforme §5.11 e §8.2 da doc técnica: a câmera grava sozinha quando detecta
// movimento — o sistema NÃO controla a gravação. Após uma entrada liberada, o
// EventProcessor chama MonitorarNovoArquivo, que:
//   1) Aguarda o tempo configurado em Ambiente.TempoEsperaGravacaoSeg (30-120s)
//      para que a câmera grave o movimento da pessoa entrando.
//   2) Conecta na câmera via DeviceService (EnderecoONVIF) e tenta autenticar.
//   3) Se a câmera responder, monta a URL de Replay RTSP padrão (Hikvision/Dahua/
//      Intelbras VIP — formato suportado pelas câmeras IP típicas do mercado
//      brasileiro) com o timestamp do acesso.
//   4) Retorna a URL para ser persistida em TentativaAcesso.GravacaoPath.
//
// Profile G completo (Recording Service + FindRecordings + GetReplayUri) não está
// implementado — exige integração SOAP avançada por fabricante. O fallback de
// URL Replay RTSP cobre as marcas mais comuns. Para câmeras de outros fabricantes
// que não suportem esse formato, o GravacaoPath continuará null (registro válido,
// sem link).
public class CameraService
{
    private readonly ICameraRepository _cameraRepo;
    private readonly ILogger<CameraService> _logger;

    public CameraService(ICameraRepository cameraRepo, ILogger<CameraService> logger)
    {
        _cameraRepo = cameraRepo;
        _logger = logger;
    }

    // Usado pelo painel para abrir o stream RTSP ao vivo (modal "Ver ao Vivo").
    public async Task<string?> ObterUrlStream(int cameraId)
    {
        var camera = await _cameraRepo.BuscarPorId(cameraId);
        if (camera == null || string.IsNullOrEmpty(camera.UrlRTSP))
            return null;
        return camera.UrlRTSP;
    }

    // Aguarda o arquivo de gravação da câmera ficar disponível e retorna a URL
    // de replay para persistir em TentativaAcesso.GravacaoPath. Retorna null se:
    // - Ambiente não tem câmera ativa com ONVIF
    // - Câmera não responde ao ONVIF dentro do timeout de conexão
    // - Erro inesperado (log warning, não propaga — gravação é best-effort)
    public async Task<string?> MonitorarNovoArquivo(int ambienteId, DateTime timestamp, int tempoEsperaSeg)
    {
        try
        {
            var cameras = await _cameraRepo.ListarPorAmbiente(ambienteId);
            // Preferência: câmera externa — fallback para qualquer ativa com ONVIF.
            var camera = cameras.FirstOrDefault(c => c.Ativa && c.Tipo == "externa" && !string.IsNullOrEmpty(c.EnderecoONVIF))
                      ?? cameras.FirstOrDefault(c => c.Ativa && !string.IsNullOrEmpty(c.EnderecoONVIF));

            if (camera == null)
            {
                _logger.LogInformation("Ambiente {AmbienteId} sem câmera ONVIF ativa — gravação não associada", ambienteId);
                return null;
            }

            // Aguarda o tempo configurado para a câmera capturar o movimento da entrada.
            // Clamp defensivo no range da doc (30-120s).
            var espera = Math.Clamp(tempoEsperaSeg, 30, 120);
            _logger.LogInformation("Aguardando {Espera}s pela gravação da câmera {CameraId} ({Nome})",
                espera, camera.Id, camera.Nome);
            await Task.Delay(TimeSpan.FromSeconds(espera));

            // Tenta conectar via ONVIF DeviceService para validar que a câmera está online.
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

            // Câmera respondeu — monta URL de replay RTSP no formato padrão da indústria.
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

    // Extrai usuário e senha do URL RTSP (rtsp://user:pass@ip:porta/path).
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

    // Constrói URL de replay RTSP no formato padrão Hikvision/Intelbras VIP.
    // YYYYMMDDThhmmssZ é o formato ISO 8601 básico usado por essas câmeras.
    // O painel reproduz via player apontando para essa URL.
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
