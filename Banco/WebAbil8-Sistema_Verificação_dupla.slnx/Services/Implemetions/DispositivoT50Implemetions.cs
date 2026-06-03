// DispositivoT50Implemetions.cs
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions
{
    public class DispositivoT50Implemetions : IDispositivoT50Repository
    {
        private readonly AppDbContext _context;

        public DispositivoT50Implemetions(AppDbContext context)
        {
            _context = context;
        }

        public DispositivoT50 Adicionar(DispositivoT50 dispositivo)
        {
            _context.DispositivosT50.Add(dispositivo);
            _context.SaveChanges();
            return dispositivo;
        }

        public DispositivoT50 Atualizar(DispositivoT50 dispositivo)
        {
            var existing = _context.DispositivosT50.Find(dispositivo.Id);
            if (existing == null) throw new ArgumentNullException("Dispositivo não encontrado");
            _context.Entry(existing).CurrentValues.SetValues(dispositivo);
            _context.SaveChanges();
            return dispositivo;
        }

        public DispositivoT50 BuscarPorId(int id)
        {
            return _context.DispositivosT50.Find(id);
        }

        public int ContarDigitaisCadastradas(int dispositivoId)
        {
            var dispositivo = _context.DispositivosT50.Find(dispositivoId);
            if (dispositivo == null) throw new ArgumentNullException("Dispositivo não encontrado");
            return dispositivo.DigitaisCadastradas;
        }

        public List<DispositivoT50> ListarTodos()
        {
            return _context.DispositivosT50.ToList();
        }

        public void Remover(int id)
        {
            var existing = _context.DispositivosT50.Find(id);
            if (existing == null) throw new ArgumentNullException("Dispositivo não encontrado");
            _context.DispositivosT50.Remove(existing);
            _context.SaveChanges();
        }

        public bool TemVagaDigital(int dispositivoId)
        {
            var dispositivo = _context.DispositivosT50.Find(dispositivoId);
            if (dispositivo == null) throw new ArgumentNullException("Dispositivo não encontrado");
            return dispositivo.DigitaisCadastradas < 1000; // T50 suporta até 1000 digitais
        }

        public void RegistrarHeartbeat(string enderecoIP)
        {
            // No-op silencioso se o dispositivo não está cadastrado — pode acontecer durante
            // primeira inicialização ou troca de IP. Não queremos quebrar o Worker por isso.
            var dispositivo = _context.DispositivosT50.FirstOrDefault(d => d.EnderecoIP == enderecoIP);
            if (dispositivo == null) return;
            dispositivo.UltimaConexao = DateTime.UtcNow;
            _context.SaveChanges();
        }
    }
}