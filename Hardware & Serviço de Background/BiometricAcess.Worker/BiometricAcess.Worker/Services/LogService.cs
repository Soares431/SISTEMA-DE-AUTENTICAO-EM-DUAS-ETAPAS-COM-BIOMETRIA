using WebAbil8_Sistema_Verificação_dupla.slnx.Services;

namespace BiometricAcess.Worker.Services
{
    public class LogService : ILogService
    {
        private readonly ILogAdminRepository _logRepository;

        public LogService(ILogAdminRepository logRepository)
        {
            _logRepository = logRepository;
        }

        public void Registrar(int adminId, string acao, string entidadeAfetada, int? entidadeId)
        {
            _logRepository.Registrar(adminId, acao, entidadeAfetada, entidadeId);
            Console.WriteLine($"Log registrado — Admin: {adminId} | Ação: {acao} | Entidade: {entidadeAfetada} | ID: {entidadeId}");
        }
    }
}