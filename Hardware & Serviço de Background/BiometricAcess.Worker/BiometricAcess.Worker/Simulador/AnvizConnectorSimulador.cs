using BiometricAcess.Worker.Models;
using BiometricAcess.Worker.Services;

namespace BiometricAcess.Worker.Simulador
{
    internal class AnvizConnectorSimulador : IAnvizConnector
    {
        private bool _conectado = false;
        private readonly IServiceScopeFactory _scopeFactory;

        public AnvizConnectorSimulador(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        // Igual ao IP que T50MSimulador.gerarEvento() preenche em IpDispositivo
        public string EnderecoIdentificador => "192.168.0.218";

        public bool Conectar()
        {
            _conectado = true;
            Console.WriteLine("Simulador: Conectado ao T50M (simulado)");
            return true;
        }

        public EventoAcesso? BuscarNovoEvento()
        {
            if (!_conectado) return null;
            return T50MSimulador.GerarEventoComBanco(_scopeFactory);
        }

        public List<EventoAcesso> BuscarEventosArmazenados()
        {
            if (!_conectado) return new List<EventoAcesso>();

            Console.WriteLine("Simulador: Buscando eventos armazenados no T50M...");

            var eventos = new List<EventoAcesso>();
            int quantidade = new Random().Next(0, 5);

            for (int i = 0; i < quantidade; i++)
                eventos.Add(T50MSimulador.GerarEventoComBanco(_scopeFactory));

            Console.WriteLine($"Simulador: {quantidade} eventos armazenados encontrados");
            return eventos;
        }

        public void Desconectar()
        {
            _conectado = false;
            Console.WriteLine("Simulador: Desconectado do T50M (simulado)");
        }
    }
}
