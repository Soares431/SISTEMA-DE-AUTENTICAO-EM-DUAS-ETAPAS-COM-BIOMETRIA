using BiometricAcess.Worker.Models;

namespace BiometricAcess.Worker.Services
{
    public  interface IAnvizConnector
    {
        bool Conectar();
        EventoAcesso? BuscarNovoEvento();
        List<EventoAcesso> BuscarEventosArmazenados();
        void Desconectar();
    }
}
