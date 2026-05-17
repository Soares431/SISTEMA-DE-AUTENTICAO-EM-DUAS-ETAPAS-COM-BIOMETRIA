using BiometricAcess.Worker.Models;

namespace BiometricAcess.Worker.Services
{
    public interface IEventProcessor
    {
        void Processar(EventoAcesso evento);
    }
}