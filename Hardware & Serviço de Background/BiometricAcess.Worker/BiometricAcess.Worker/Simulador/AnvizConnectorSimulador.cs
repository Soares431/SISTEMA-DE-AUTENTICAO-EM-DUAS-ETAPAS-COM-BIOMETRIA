using BiometricAcess.Worker.Models;
using BiometricAcess.Worker.Services;

namespace BiometricAcess.Worker.Simulador
{
    internal class AnvizConnectorSimulador : IAnvizConnector
    {
        private bool _conectado = false;

        public bool Conectar()
        {
            _conectado = true;
            Console.WriteLine("Simulador: Conectado ao T50M (simulado)");
            return true;
        }

        public EventoAcesso? BuscarNovoEvento()
        {
            if (!_conectado)
            {
                return null;
            }
            else
            {
                return T50MSimulador.gerarEvento();
            }
        }

        public void Desconectar()
        {
            _conectado = false;
            Console.WriteLine("Simulador: Desconectado do T50M (simulado)");
        }
    }
}
