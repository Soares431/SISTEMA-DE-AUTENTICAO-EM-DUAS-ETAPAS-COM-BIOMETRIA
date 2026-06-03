using Anviz.SDK;
using Anviz.SDK.Responses;
using BiometricAcess.Worker.Models;
using System.Collections.Concurrent;

namespace BiometricAcess.Worker.Services
{
    public class AnvizConnector : IAnvizConnector
    {
        private AnvizDevice? _device;
        private readonly string _ip;
        private readonly int _porta;
        private readonly ConcurrentQueue<EventoAcesso> _filaEventos = new();

        public AnvizConnector(string ip, int porta)
        {
            _ip = ip;
            _porta = porta;
        }

        public string EnderecoIdentificador => _ip;

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

            // RecordType bit 7: 1 = porta abriu, 0 = porta não abriu
            // Fonte: documentação oficial Anviz SDK "New SDK API 2019.docx"
            bool acessoLiberado = (record.RecordType & 0x80) != 0;

            _filaEventos.Enqueue(new EventoAcesso
            {
                PessoaID = (int)record.UserCode,
                TipoVerificacao = tipoVerificacao,
                AcessoLiberado = acessoLiberado,
                DataHora = record.DateTime,
                IpDispositivo = _ip,
                MotivoNegacao = string.Empty
            });
        }

        private void OnDeviceError(object? sender, Exception ex)
        {
            // Erro de comunicação com o dispositivo — não é acesso negado
            // O Worker vai detectar a falha no próximo BuscarNovoEvento e reconectar
            Console.WriteLine($"Erro no dispositivo: {ex.Message}");
        }

        public EventoAcesso? BuscarNovoEvento()
        {
            if (_device == null)
                return null;

            _filaEventos.TryDequeue(out var evento);
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

                    // RecordType bit 7: 1 = porta abriu, 0 = porta não abriu
                    bool acessoLiberado = (record.RecordType & 0x80) != 0;

                    eventos.Add(new EventoAcesso
                    {
                        PessoaID = (int)record.UserCode,
                        TipoVerificacao = tipoVerificacao,
                        AcessoLiberado = acessoLiberado,
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