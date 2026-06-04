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

        // Vincula um T50 ao ambiente. Por convenção:
        // - Se for o primeiro T50 do ambiente: vincula vazio (sem copiar nada).
        // - Se já há outros T50 no ambiente: COPIA todas as pessoas do ambiente para o novo T50.
        //   Antes da cópia, valida a capacidade do T50 destino (1000 digitais).
        //   Lança InvalidOperationException se a capacidade não for suficiente.
        // - Mantém ambiente.DispositivoT50Id apontando para algum T50 vinculado (compat).
        public AmbienteT50 Adicionar(int ambienteId, int t50Id, bool ehPrincipal = false)
        {
            var existente = _context.AmbientesT50.FirstOrDefault(at => at.AmbienteId == ambienteId && at.DispositivoT50Id == t50Id);
            if (existente != null) return existente;

            var t50 = _context.DispositivosT50.Find(t50Id)
                ?? throw new InvalidOperationException("T50 não encontrado.");

            // Pessoas do ambiente que ainda não estão neste T50 (para copiar)
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

            // Verifica capacidade do T50 destino
            var capacidadeRestante = 1000 - t50.DigitaisCadastradas;
            if (paraCopiar.Count > capacidadeRestante)
            {
                throw new InvalidOperationException(
                    $"T50 '{t50.Nome}' não tem capacidade suficiente. " +
                    $"Restam {capacidadeRestante} slots, mas o ambiente tem {paraCopiar.Count} pessoa(s) a copiar.");
            }

            // Cria o vínculo do T50 ao ambiente
            var vinculo = new AmbienteT50
            {
                AmbienteId = ambienteId,
                DispositivoT50Id = t50Id,
                DataVinculo = DateTime.UtcNow,
                EhPrincipal = ehPrincipal // mantido por compat, mas a UI não diferencia mais
            };
            _context.AmbientesT50.Add(vinculo);

            // Copia as pessoas do ambiente para este T50
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

            // Se for o primeiro vinculado, atualiza Ambiente.DispositivoT50Id para apontar pra ele
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

            // Remove as pessoas deste ambiente do T50 (mas só as que estão APENAS por causa deste ambiente —
            // se a pessoa também é de outro ambiente que usa o mesmo T50, mantém).
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

            // Se o T50 removido era o referenciado por Ambiente.DispositivoT50Id, aponta pra qualquer outro restante
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
            // Filtra ambientes soft-deletados — eles continuam no banco apenas para
            // preservar referência em tentativas históricas, não devem aparecer em UI operacional.
            return _context.AmbientesT50
                .Where(at => at.DispositivoT50Id == t50Id)
                .Include(at => at.Ambiente)
                .Select(at => at.Ambiente!)
                .Where(a => a != null && !a.Excluido)
                .ToList();
        }

        // Mantido para compatibilidade com código antigo — retorna o T50 referenciado por Ambiente.DispositivoT50Id.
        public DispositivoT50? BuscarPrincipal(int ambienteId)
        {
            var ambiente = _context.Ambientes.Find(ambienteId);
            if (ambiente == null || ambiente.DispositivoT50Id == 0) return null;
            return _context.DispositivosT50.Find(ambiente.DispositivoT50Id);
        }

        public void DefinirPrincipal(int ambienteId, int t50Id)
        {
            // Mantido por compat — só atualiza Ambiente.DispositivoT50Id.
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
