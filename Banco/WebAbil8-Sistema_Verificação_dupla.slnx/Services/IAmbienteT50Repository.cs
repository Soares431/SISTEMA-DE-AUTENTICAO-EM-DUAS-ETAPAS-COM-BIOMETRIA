using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface IAmbienteT50Repository
    {

        AmbienteT50 Adicionar(int ambienteId, int t50Id, bool ehPrincipal = false);

        void Remover(int ambienteId, int t50Id);

        List<DispositivoT50> ListarT50sDoAmbiente(int ambienteId);

        List<Ambiente> ListarAmbientesDoT50(int t50Id);

        DispositivoT50? BuscarPrincipal(int ambienteId);

        void DefinirPrincipal(int ambienteId, int t50Id);

        bool Existe(int ambienteId, int t50Id);

        bool T50EmUso(int t50Id);
    }
}

