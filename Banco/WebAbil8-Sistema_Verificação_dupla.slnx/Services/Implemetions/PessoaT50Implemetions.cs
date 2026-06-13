using Microsoft.EntityFrameworkCore;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions
{
    public class PessoaT50Implemetions : IPessoaT50Repository
    {
        private readonly AppDbContext _context;

        public PessoaT50Implemetions(AppDbContext context)
        {
            _context = context;
        }

        public PessoaT50 Adicionar(long pessoaId, int t50Id)
        {
            var existente = _context.PessoasT50.FirstOrDefault(pt => pt.PessoaId == pessoaId && pt.DispositivoT50Id == t50Id);
            if (existente != null) return existente;

            var vinculo = new PessoaT50
            {
                PessoaId = pessoaId,
                DispositivoT50Id = t50Id,
                DataCadastro = DateTime.UtcNow
            };
            _context.PessoasT50.Add(vinculo);

            var t50 = _context.DispositivosT50.Find(t50Id);
            if (t50 != null) t50.DigitaisCadastradas++;

            _context.SaveChanges();
            return vinculo;
        }

        public void Remover(long pessoaId, int t50Id)
        {
            var vinculo = _context.PessoasT50.FirstOrDefault(pt => pt.PessoaId == pessoaId && pt.DispositivoT50Id == t50Id);
            if (vinculo == null) return;
            _context.PessoasT50.Remove(vinculo);

            var t50 = _context.DispositivosT50.Find(t50Id);
            if (t50 != null && t50.DigitaisCadastradas > 0) t50.DigitaisCadastradas--;

            _context.SaveChanges();
        }

        public List<DispositivoT50> ListarT50sDaPessoa(long pessoaId)
        {
            return _context.PessoasT50
                .Where(pt => pt.PessoaId == pessoaId)
                .Include(pt => pt.Dispositivo)
                .Select(pt => pt.Dispositivo!)
                .Where(d => d != null)
                .ToList();
        }

        public List<Pessoa> ListarPessoasDoT50(int t50Id)
        {
            return _context.PessoasT50
                .Where(pt => pt.DispositivoT50Id == t50Id)
                .Include(pt => pt.Pessoa)
                .Select(pt => pt.Pessoa!)
                .Where(p => p != null)
                .ToList();
        }

        public bool EstaCadastrada(long pessoaId, int t50Id)
        {
            return _context.PessoasT50.Any(pt => pt.PessoaId == pessoaId && pt.DispositivoT50Id == t50Id);
        }

        public int ContarPessoasNoT50(int t50Id)
        {
            return _context.PessoasT50.Count(pt => pt.DispositivoT50Id == t50Id);
        }
    }
}

