using System.Runtime.ExceptionServices;
using System.Xml.Linq;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Service.Impletions
{
    public class PersonServicesImplemetions : IPersonServices
    {
        private AppDbContext _context;
         
  
        public PersonServicesImplemetions(AppDbContext context)
        {
            _context = context; 
        }

        public List<Pessoa> GetAll()
        {

            return _context.Pessoas.ToList();
        }

        public Pessoa FidnById(long id)
        {
            return _context.Pessoas.Find(id);
        }



        public Pessoa Create(Pessoa person)
        {
            _context.Add(person);
            _context.SaveChanges();
            return person;
        }

        public Pessoa Update(Pessoa person)
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
        public void Delete(long id)
        {
            var existingPerson = _context.Pessoas.Find(id);

            if (existingPerson == null)
            {
                throw new ArgumentNullException("ID not Existing");
            }
            _context.Remove(existingPerson);
            _context.SaveChanges();

        }

  
    }
}
