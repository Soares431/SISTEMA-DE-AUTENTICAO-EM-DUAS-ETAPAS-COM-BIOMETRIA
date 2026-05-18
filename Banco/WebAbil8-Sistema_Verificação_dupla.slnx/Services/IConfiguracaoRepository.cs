using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface IConfiguracaoRepository
    {
        Task<Configuracao> BuscarPorChave();
        Task<Configuracao> Atualizar(Configuracao configuracao);
    }
}
