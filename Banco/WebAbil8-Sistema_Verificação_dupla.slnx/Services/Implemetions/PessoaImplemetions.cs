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

        public Pessoa BuscarPorCPF(string cpf)
        {
            return _context.Pessoas
                .FirstOrDefault(p => p.Cpf == cpf);
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

        public Pessoa AtualizarUltimoAcesso(long pessoaId)
        {
            var pessoa = _context.Pessoas.Find(pessoaId);
            if (pessoa == null) throw new ArgumentNullException("Erro ao Atualizar cadastro, Usuario Inexistente");
            pessoa.dataUltimoAcesso = DateTime.Now;
            _context.SaveChanges();
            return pessoa;
        }

        public void AlterarStatus(long pessoaId, bool status)
        {
            var pessoa = _context.Pessoas.Find(pessoaId);
            if (pessoa == null) throw new ArgumentNullException("Erro ao Alterar Status, Usuario Inexistente");
            pessoa.Status = status ? "ativo" : "inativo";
            _context.SaveChanges();
        }

        public Pessoa MarcarBiometriaCadastrada(long pessoaId)
        {
            var pessoa = _context.Pessoas.Find(pessoaId);
            if (pessoa == null) throw new ArgumentNullException("Erro ao Marcar Biometrica, Usuario Inexistente");
            pessoa.biometriaCadastrada = DateTime.Now;
            _context.SaveChanges();
            return pessoa;
        }



        public Pessoa SalvarTemplate(long pessoaId, byte[] template)
        {
            var pessoa = _context.Pessoas.Find(pessoaId);
            if (pessoa == null) throw new ArgumentNullException("Erro ao Salvar template. Usuario Inexistente.");
            pessoa.templateBackup = template;
            _context.SaveChanges();
            return pessoa;
        }

    }
}