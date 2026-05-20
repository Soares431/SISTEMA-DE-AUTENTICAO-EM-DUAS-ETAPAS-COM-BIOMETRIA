using BiometricAcess.Worker.Models;

namespace BiometricAcess.Worker.Services
{
    public interface IEventProcessor
    {
        Task Processar(EventoAcesso evento);
    }
}