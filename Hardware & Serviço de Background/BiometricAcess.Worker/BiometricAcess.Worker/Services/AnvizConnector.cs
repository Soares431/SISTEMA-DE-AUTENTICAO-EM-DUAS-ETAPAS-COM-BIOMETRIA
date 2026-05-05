using Anviz.SDK;
using Anviz.SDK.Responses;
using BiometricAcess.Worker.Models;

namespace BiometricAcess.Worker.Services
{
    public class AnvizConnector : IAnvizConnector
    {
        private AnvizDevice? _device;
        private readonly string _ip;
        private readonly int _porta;
        private EventoAcesso? _ultimoEvento;

        public AnvizConnector(string ip, int porta)
        {
            _ip = ip;
            _porta = porta;
        }

        public bool Conectar()
        {
            try
            {
                var manager = new AnvizManager();
                _device = manager.Connect(_ip, _porta).Result;
                _device.ReceivedRecord += OnReceivedRecord;
                _device.DeviceError += OnDeviceError;
                Console.WriteLine($"Conectado ao T50M em {_ip}:{_porta}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao conectar: {ex.Message}");
                return false;
            }
        }

        private void OnReceivedRecord(object? sender, Record record)
        {
            string tipoVerificacao;
            if (record.BackupCode == 4)
            {
                tipoVerificacao = "senha_id";
            }
            else
            {
                tipoVerificacao = "digital_id";
            }

            _ultimoEvento = new EventoAcesso
            {
                PessoaID = (int)record.UserCode,
                TipoVerificacao = tipoVerificacao,
                AcessoLiberado = true,
                DataHora = record.DateTime,
                IpDispositivo = _ip,
                MotivoNegacao = string.Empty
            };
        }

        private void OnDeviceError(object? sender, Exception ex)
        {
            string motivo = string.Empty;

            if (ex.Message.Contains("NO_USER"))
            {
                motivo = "nao_cadastrado";
            }
            else if (ex.Message.Contains("FAIL"))
            {
                motivo = "biometria_invalida";
            }
            else if (ex.Message.Contains("TIME_OUT"))
            {
                motivo = "senha_invalida";
            }

            if (!string.IsNullOrEmpty(motivo))
            {
                _ultimoEvento = new EventoAcesso
                {
                    PessoaID = 0,
                    TipoVerificacao = string.Empty,
                    AcessoLiberado = false,
                    DataHora = DateTime.Now,
                    IpDispositivo = _ip,
                    MotivoNegacao = motivo
                };
            }
        }

        public EventoAcesso? BuscarNovoEvento()
        {
            if (_device == null)
            {
                return null;
            }

            var evento = _ultimoEvento;
            _ultimoEvento = null;
            return evento;
        }

        public List<EventoAcesso> BuscarEventosArmazenados()
        {
            if (_device == null)
            {
                return new List<EventoAcesso>();
            }

            var eventos = new List<EventoAcesso>();

            try
            {
                var records = _device.DownloadRecords(onlyNew: true).Result;

                foreach (var record in records)
                {
                    string tipoVerificacao;
                    if (record.BackupCode == 4)
                    {
                        tipoVerificacao = "senha_id";
                    }
                    else
                    {
                        tipoVerificacao = "digital_id";
                    }

                    eventos.Add(new EventoAcesso
                    {
                        PessoaID = (int)record.UserCode,
                        TipoVerificacao = tipoVerificacao,
                        AcessoLiberado = true,
                        DataHora = record.DateTime,
                        IpDispositivo = _ip,
                        MotivoNegacao = string.Empty
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar eventos armazenados: {ex.Message}");
            }

            return eventos;
        }

        public void Desconectar()
        {
            if (_device != null)
            {
                _device.ReceivedRecord -= OnReceivedRecord;
                _device.DeviceError -= OnDeviceError;
                _device = null;
            }
            Console.WriteLine("Desconectado do T50M");
        }
    }
}