using BiometricAcess.Worker.Models;

namespace BiometricAcess.Worker.Services
{
    public  interface IAnvizConnector
    {
        bool Conectar();
        EventoAcesso? BuscarNovoEvento();
        List<EventoAcesso> BuscarEventosArmazenados();
        void Desconectar();
        // Identificador do dispositivo conectado (IP do T50M) — usado pelo Worker para registrar heartbeat.
        string EnderecoIdentificador { get; }
    }
}
