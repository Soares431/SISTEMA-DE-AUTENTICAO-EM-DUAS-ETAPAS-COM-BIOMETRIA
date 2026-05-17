using BiometricAcess.Worker.Services;

namespace BiometricAcess.Worker.Simulador
{
    public class AnvizServiceSimulador : IAnvizService
    {
        public bool AdicionarPessoa(int id, string nome, string senha)
        {
            Console.WriteLine($"Simulador: Pessoa adicionada — ID: {id} | Nome: {nome}");
            return true;
        }

        public bool RemoverPessoa(int id)
        {
            Console.WriteLine($"Simulador: Pessoa removida — ID: {id}");
            return true;
        }

        public bool UploadTemplate(int id, byte[] template)
        {
            Console.WriteLine($"Simulador: Template enviado — ID: {id} | Tamanho: {template.Length} bytes");
            return true;
        }

        public byte[]? DownloadTemplate(int id)
        {
            Console.WriteLine($"Simulador: Template baixado — ID: {id}");
            return new byte[338]; 
        }

        public bool IniciarCapturaDigital(int id)
        {
            Console.WriteLine($"Simulador: Captura de digital iniciada — ID: {id} | T50M exibiria PLACE FINGER");
            return true;
        }

        public bool AlterarModo(int id, string modo)
        {
            Console.WriteLine($"Simulador: Modo alterado — ID: {id} | Modo: {modo}");
            return true;
        }

        public bool SincronizarHora()
        {
            Console.WriteLine($"Simulador: Hora sincronizada — {DateTime.Now}");
            return true;
        }
    }
}