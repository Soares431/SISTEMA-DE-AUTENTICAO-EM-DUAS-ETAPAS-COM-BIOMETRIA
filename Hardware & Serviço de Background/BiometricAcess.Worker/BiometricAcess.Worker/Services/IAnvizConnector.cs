using BiometricAcess.Worker.Models;

namespace BiometricAcess.Worker.Services
{
    public  interface IAnvizConnector
    {
        bool Conectar();
        EventoAcesso? BuscarNovoEvento();
        List<EventoAcesso> BuscarEventosArmazenados();
        void Desconectar();
        // Identificador do dispositivo conectado — usado pelo Worker para registrar heartbeat.
        // T50M real: IP. Arduino: nome da porta serial (COM3). Simulador: IP fake.
        string EnderecoIdentificador { get; }
    }
}
