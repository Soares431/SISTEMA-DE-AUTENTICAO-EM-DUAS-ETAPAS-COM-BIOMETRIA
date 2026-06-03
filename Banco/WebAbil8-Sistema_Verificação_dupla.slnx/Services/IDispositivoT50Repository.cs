using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface IDispositivoT50Repository
    {
        DispositivoT50 Adicionar(DispositivoT50 person);
        DispositivoT50 BuscarPorId(int id);
        List<DispositivoT50> ListarTodos();
        DispositivoT50 Atualizar(DispositivoT50 person);
        void Remover(int id);

        int ContarDigitaisCadastradas(int dispositivoId);
        bool TemVagaDigital(int dispositivoId);

        // Heartbeat: o Worker chama isso ao conectar/receber evento para marcar o dispositivo como online.
        // Busca por IP em vez de Id porque o Worker conhece IP, não Id.
        void RegistrarHeartbeat(string enderecoIP);
    }
}
