namespace BiometricAcess.Worker.Services
{
    public interface ILogService
    {
        void Registrar(int adminId, string acao, string entidadeAfetada, int? entidadeId);
    }
}