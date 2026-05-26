// AmbientePessoaImplemetions.cs
using Microsoft.EntityFrameworkCore;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions
{
    public class AmbientePessoaImplemetions : IAmbientePessoaRepository
    {
        private readonly AppDbContext _context;

        public AmbientePessoaImplemetions(AppDbContext context)
        {
            _context = context;
        }

        public AmbientePessoa AdicionarPessoa(AmbientePessoa person)
        {
            _context.AmbientesPessoas.Add(person);
            _context.SaveChanges();
            return person;
        }

        public AmbientePessoa Atualizar(AmbientePessoa person)
        {
            var existing = _context.AmbientesPessoas.Find(person.AmbienteId, person.PessoaId);
            if (existing == null) throw new ArgumentNullException("Relação não encontrada");
            _context.Entry(existing).CurrentValues.SetValues(person);
            _context.SaveChanges();
            return person;
        }

        public AmbientePessoa BuscarPorId(int ambienteId, long pessoaId)
        {
            return _context.AmbientesPessoas.Find(ambienteId, pessoaId);
        }

        public int ContarPessoasPorAmbiente(int ambienteId)
        {
            return _context.AmbientesPessoas.Count(ap => ap.AmbienteId == ambienteId);
        }

        public List<Ambiente> ListarAmbientesDaPessoa(long pessoaId)
        {
            return _context.AmbientesPessoas
                .Include(ap => ap.Ambiente)
                .Where(ap => ap.PessoaId == pessoaId)
                .Select(ap => ap.Ambiente)
                .ToList();
        }

        public List<Pessoa> ListarPessoasDoAmbiente(int ambienteId)
        {
            return _context.AmbientesPessoas
                .Include(ap => ap.Pessoa)
                .Where(ap => ap.AmbienteId == ambienteId)
                .Select(ap => ap.Pessoa)
                .ToList();
        }

        public List<AmbientePessoa> ListarTodos()
        {
            return _context.AmbientesPessoas
                .Include(ap => ap.Pessoa)
                .Include(ap => ap.Ambiente)
                .ToList();
        }

        public bool PessoaTemAcesso(int ambienteId, long pessoaId)
        {
            return _context.AmbientesPessoas
                .Any(ap => ap.AmbienteId == ambienteId && ap.PessoaId == pessoaId);
        }

        public void RemoverPessoa(int ambienteId, long pessoaId)
        {
            var existing = _context.AmbientesPessoas.Find(ambienteId, pessoaId);
            if (existing == null) throw new ArgumentNullException("Relação não encontrada");
            _context.AmbientesPessoas.Remove(existing);
            _context.SaveChanges();
        }
    }
}