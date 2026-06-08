using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface IT50PendenciaRepository
    {
        T50Pendencia Enfileirar(string acao, long pessoaId, int dispositivoT50Id);
        List<T50Pendencia> ListarPendentes(int max = 50);
        void MarcarSincronizado(int id);
        void RegistrarErro(int id, string erro);
    }
}
