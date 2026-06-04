using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface IAmbienteT50Repository
    {
        // Vincula um T50 ao ambiente. Se ehPrincipal=true, desmarca o atual.
        AmbienteT50 Adicionar(int ambienteId, int t50Id, bool ehPrincipal = false);

        // Desvincula um T50 do ambiente (não apaga o T50, só o vínculo).
        void Remover(int ambienteId, int t50Id);

        // Lista todos os T50 vinculados ao ambiente.
        List<DispositivoT50> ListarT50sDoAmbiente(int ambienteId);

        // Lista todos os ambientes que usam este T50.
        List<Ambiente> ListarAmbientesDoT50(int t50Id);

        // Retorna o T50 principal do ambiente, ou null se nenhum tiver ehPrincipal=true.
        DispositivoT50? BuscarPrincipal(int ambienteId);

        // Marca um T50 como principal e desmarca o atual. Atualiza ambiente.DispositivoT50Id também.
        void DefinirPrincipal(int ambienteId, int t50Id);

        // True se já existe vínculo entre esse ambiente e esse T50.
        bool Existe(int ambienteId, int t50Id);

        // True se algum ambiente (qualquer) está vinculado a este T50.
        bool T50EmUso(int t50Id);
    }
}
