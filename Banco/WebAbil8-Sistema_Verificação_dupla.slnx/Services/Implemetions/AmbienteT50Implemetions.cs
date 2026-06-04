using Microsoft.EntityFrameworkCore;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions
{
    public class AmbienteT50Implemetions : IAmbienteT50Repository
    {
        private readonly AppDbContext _context;

        public AmbienteT50Implemetions(AppDbContext context)
        {
            _context = context;
        }

        public AmbienteT50 Adicionar(int ambienteId, int t50Id, bool ehPrincipal = false)
        {
            var existente = _context.AmbientesT50.FirstOrDefault(at => at.AmbienteId == ambienteId && at.DispositivoT50Id == t50Id);
            if (existente != null)
            {
                if (ehPrincipal && !existente.EhPrincipal)
                {
                    DefinirPrincipal(ambienteId, t50Id);
                }
                return existente;
            }

            if (ehPrincipal)
            {
                // Desmarca o principal atual antes de inserir o novo
                var atuaisPrincipais = _context.AmbientesT50.Where(at => at.AmbienteId == ambienteId && at.EhPrincipal).ToList();
                foreach (var p in atuaisPrincipais) p.EhPrincipal = false;
            }

            var vinculo = new AmbienteT50
            {
                AmbienteId = ambienteId,
                DispositivoT50Id = t50Id,
                DataVinculo = DateTime.UtcNow,
                EhPrincipal = ehPrincipal
            };
            _context.AmbientesT50.Add(vinculo);
            _context.SaveChanges();

            if (ehPrincipal)
            {
                var ambiente = _context.Ambientes.Find(ambienteId);
                if (ambiente != null)
                {
                    ambiente.DispositivoT50Id = t50Id;
                    _context.SaveChanges();
                }
            }
            return vinculo;
        }

        public void Remover(int ambienteId, int t50Id)
        {
            var vinculo = _context.AmbientesT50.FirstOrDefault(at => at.AmbienteId == ambienteId && at.DispositivoT50Id == t50Id);
            if (vinculo == null) return;
            _context.AmbientesT50.Remove(vinculo);
            _context.SaveChanges();

            // Se era o principal, promove qualquer outro restante a principal
            if (vinculo.EhPrincipal)
            {
                var restante = _context.AmbientesT50.FirstOrDefault(at => at.AmbienteId == ambienteId);
                if (restante != null)
                {
                    restante.EhPrincipal = true;
                    var ambiente = _context.Ambientes.Find(ambienteId);
                    if (ambiente != null) ambiente.DispositivoT50Id = restante.DispositivoT50Id;
                    _context.SaveChanges();
                }
            }
        }

        public List<DispositivoT50> ListarT50sDoAmbiente(int ambienteId)
        {
            return _context.AmbientesT50
                .Where(at => at.AmbienteId == ambienteId)
                .Include(at => at.Dispositivo)
                .OrderByDescending(at => at.EhPrincipal)
                .ThenBy(at => at.DataVinculo)
                .Select(at => at.Dispositivo!)
                .Where(d => d != null)
                .ToList();
        }

        public List<Ambiente> ListarAmbientesDoT50(int t50Id)
        {
            return _context.AmbientesT50
                .Where(at => at.DispositivoT50Id == t50Id)
                .Include(at => at.Ambiente)
                .Select(at => at.Ambiente!)
                .Where(a => a != null)
                .ToList();
        }

        public DispositivoT50? BuscarPrincipal(int ambienteId)
        {
            return _context.AmbientesT50
                .Where(at => at.AmbienteId == ambienteId && at.EhPrincipal)
                .Include(at => at.Dispositivo)
                .Select(at => at.Dispositivo)
                .FirstOrDefault();
        }

        public void DefinirPrincipal(int ambienteId, int t50Id)
        {
            var vinculos = _context.AmbientesT50.Where(at => at.AmbienteId == ambienteId).ToList();
            foreach (var v in vinculos)
            {
                v.EhPrincipal = (v.DispositivoT50Id == t50Id);
            }
            var ambiente = _context.Ambientes.Find(ambienteId);
            if (ambiente != null) ambiente.DispositivoT50Id = t50Id;
            _context.SaveChanges();
        }

        public bool Existe(int ambienteId, int t50Id)
        {
            return _context.AmbientesT50.Any(at => at.AmbienteId == ambienteId && at.DispositivoT50Id == t50Id);
        }

        public bool T50EmUso(int t50Id)
        {
            return _context.AmbientesT50.Any(at => at.DispositivoT50Id == t50Id);
        }
    }
}
