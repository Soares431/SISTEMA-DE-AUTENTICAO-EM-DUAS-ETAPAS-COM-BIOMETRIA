using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions
{
    public class PessoaImplemetions : IPessoaRepository

    {
        private AppDbContext _context;

        public PessoaImplemetions(AppDbContext context)
        {
            _context = context;
        }

        public Pessoa Adicionar(Pessoa person)
        {
            _context.Add(person);
            _context.SaveChanges();
            return person;
        }
        public List<Pessoa> ListarTodos()
        {
            return _context.Pessoas.ToList();
        }

        public Pessoa BuscarPorCPF(long id)
        {
            throw new NotImplementedException();
        }

        public Pessoa BuscarPorId(long id)
        {
            return _context.Pessoas.Find(id);
        }



        public Pessoa Atualizar(Pessoa person)
        {
            var existingPerson = _context.Pessoas.Find(person.Id);
            if (existingPerson == null)
            {
                throw new ArgumentNullException("ID not Existing");
            }
            _context.Entry(existingPerson).CurrentValues.SetValues(person);
            _context.SaveChanges();
            return person;
        }

        public void Remover(long id)
        {
            var existingPerson = _context.Pessoas.Find(id);
            if (existingPerson == null)
            {
                throw new ArgumentNullException("ID not Existing");
            }
            _context.Remove(existingPerson);
            _context.SaveChanges();
        }

        public DateTime AtualizarUltimoAcesso()
        {
            throw new NotImplementedException();
        }

        public void AlterarStatus()
        {
            throw new NotImplementedException();
        }

        public DateTime MarcarBiometriaCadastrada()
        {
            throw new NotImplementedException();
        }



        public byte[] SalvarTemplate()
        {
            throw new NotImplementedException();
        }


    }
}