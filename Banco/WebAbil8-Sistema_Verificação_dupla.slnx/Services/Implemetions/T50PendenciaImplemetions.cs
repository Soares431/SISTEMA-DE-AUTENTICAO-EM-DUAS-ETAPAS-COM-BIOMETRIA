using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions
{
    public class T50PendenciaImplemetions : IT50PendenciaRepository
    {
        private readonly AppDbContext _context;

        public T50PendenciaImplemetions(AppDbContext context)
        {
            _context = context;
        }

        public T50Pendencia Enfileirar(string acao, long pessoaId, int dispositivoT50Id)
        {
            var pendencia = new T50Pendencia
            {
                Acao = acao,
                PessoaId = pessoaId,
                DispositivoT50Id = dispositivoT50Id,
                CriadoEm = DateTime.UtcNow,
                Sincronizado = false
            };
            _context.T50Pendencias.Add(pendencia);
            _context.SaveChanges();
            return pendencia;
        }

        public List<T50Pendencia> ListarPendentes(int max = 50)
        {

            return _context.T50Pendencias
                .Where(p => !p.Sincronizado && p.TentativasFalhas < 5)
                .OrderBy(p => p.CriadoEm)
                .Take(max)
                .ToList();
        }

        public void MarcarSincronizado(int id)
        {
            var p = _context.T50Pendencias.Find(id);
            if (p == null) return;
            p.Sincronizado = true;
            p.SincronizadoEm = DateTime.UtcNow;
            _context.SaveChanges();
        }

        public void RegistrarErro(int id, string erro)
        {
            var p = _context.T50Pendencias.Find(id);
            if (p == null) return;
            p.TentativasFalhas++;
            p.ErroUltimaTentativa = erro.Length > 500 ? erro[..500] : erro;
            _context.SaveChanges();
        }
    }
}

