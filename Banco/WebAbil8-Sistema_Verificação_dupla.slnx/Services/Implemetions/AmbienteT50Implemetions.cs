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
            if (existente != null) return existente;

            var t50 = _context.DispositivosT50.Find(t50Id)
                ?? throw new InvalidOperationException("T50 não encontrado.");

            var pessoasDoAmbiente = _context.AmbientesPessoas
                .Where(ap => ap.AmbienteId == ambienteId)
                .Select(ap => ap.PessoaId)
                .Distinct()
                .ToList();
            var jaCadastradas = _context.PessoasT50
                .Where(pt => pt.DispositivoT50Id == t50Id && pessoasDoAmbiente.Contains(pt.PessoaId))
                .Select(pt => pt.PessoaId)
                .ToHashSet();
            var paraCopiar = pessoasDoAmbiente.Where(pid => !jaCadastradas.Contains(pid)).ToList();

            var capacidadeRestante = 1000 - t50.DigitaisCadastradas;
            if (paraCopiar.Count > capacidadeRestante)
            {
                throw new InvalidOperationException(
                    $"T50 '{t50.Nome}' não tem capacidade suficiente. " +
                    $"Restam {capacidadeRestante} slots, mas o ambiente tem {paraCopiar.Count} pessoa(s) a copiar.");
            }

            var vinculo = new AmbienteT50
            {
                AmbienteId = ambienteId,
                DispositivoT50Id = t50Id,
                DataVinculo = DateTime.UtcNow,
                EhPrincipal = ehPrincipal
            };
            _context.AmbientesT50.Add(vinculo);

            foreach (var pessoaId in paraCopiar)
            {
                _context.PessoasT50.Add(new PessoaT50
                {
                    PessoaId = pessoaId,
                    DispositivoT50Id = t50Id,
                    DataCadastro = DateTime.UtcNow
                });
                t50.DigitaisCadastradas++;
            }

            var ambiente = _context.Ambientes.Find(ambienteId);
            if (ambiente != null && (ambiente.DispositivoT50Id == 0
                || !_context.AmbientesT50.Any(at => at.AmbienteId == ambienteId && at.DispositivoT50Id == ambiente.DispositivoT50Id)))
            {
                ambiente.DispositivoT50Id = t50Id;
            }

            _context.SaveChanges();
            return vinculo;
        }

        public void Remover(int ambienteId, int t50Id)
        {
            var vinculo = _context.AmbientesT50.FirstOrDefault(at => at.AmbienteId == ambienteId && at.DispositivoT50Id == t50Id);
            if (vinculo == null) return;

            var pessoasDoAmbiente = _context.AmbientesPessoas
                .Where(ap => ap.AmbienteId == ambienteId)
                .Select(ap => ap.PessoaId)
                .Distinct()
                .ToList();

            var outrosAmbientesDoT50 = _context.AmbientesT50
                .Where(at => at.DispositivoT50Id == t50Id && at.AmbienteId != ambienteId)
                .Select(at => at.AmbienteId)
                .ToList();

            var pessoasEmOutrosAmbientesDoT50 = _context.AmbientesPessoas
                .Where(ap => outrosAmbientesDoT50.Contains(ap.AmbienteId) && pessoasDoAmbiente.Contains(ap.PessoaId))
                .Select(ap => ap.PessoaId)
                .ToHashSet();

            var paraRemoverDoT50 = pessoasDoAmbiente.Where(pid => !pessoasEmOutrosAmbientesDoT50.Contains(pid)).ToList();
            var t50 = _context.DispositivosT50.Find(t50Id);
            foreach (var pessoaId in paraRemoverDoT50)
            {
                var vp = _context.PessoasT50.FirstOrDefault(pt => pt.PessoaId == pessoaId && pt.DispositivoT50Id == t50Id);
                if (vp != null)
                {
                    _context.PessoasT50.Remove(vp);
                    if (t50 != null && t50.DigitaisCadastradas > 0) t50.DigitaisCadastradas--;
                }
            }

            _context.AmbientesT50.Remove(vinculo);
            _context.SaveChanges();

            var ambiente = _context.Ambientes.Find(ambienteId);
            if (ambiente != null && ambiente.DispositivoT50Id == t50Id)
            {
                var restante = _context.AmbientesT50.FirstOrDefault(at => at.AmbienteId == ambienteId);
                ambiente.DispositivoT50Id = restante?.DispositivoT50Id ?? 0;
                _context.SaveChanges();
            }
        }

        public List<DispositivoT50> ListarT50sDoAmbiente(int ambienteId)
        {
            return _context.AmbientesT50
                .Where(at => at.AmbienteId == ambienteId)
                .Include(at => at.Dispositivo)
                .OrderBy(at => at.DataVinculo)
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
                .Where(a => a != null && !a.Excluido)
                .ToList();
        }

        public DispositivoT50? BuscarPrincipal(int ambienteId)
        {
            var ambiente = _context.Ambientes.Find(ambienteId);
            if (ambiente == null || ambiente.DispositivoT50Id == 0) return null;
            return _context.DispositivosT50.Find(ambiente.DispositivoT50Id);
        }

        public void DefinirPrincipal(int ambienteId, int t50Id)
        {

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

