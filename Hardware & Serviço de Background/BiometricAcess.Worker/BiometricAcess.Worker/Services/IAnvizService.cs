
namespace BiometricAcess.Worker.Services
{
    public interface IAnvizService
    {
        bool AdicionarPessoa(int id, string nome, string senha);
        bool RemoverPessoa(int id);
        bool UploadTemplate(int id, byte[] template);
        byte[]? DownloadTemplate(int id);
        byte[]? IniciarCapturaDigital(int id);
        bool AlterarModo(int id, string modo);
        bool SincronizarHora();
    }
}
