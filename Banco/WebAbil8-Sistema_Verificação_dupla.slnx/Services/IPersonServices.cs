using Microsoft.AspNetCore.Identity.Data;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Service
{
    public interface IPersonServices
    { 
        Pessoa Create (Pessoa person);
        Pessoa FidnById(long id); 
        List<Pessoa> GetAll();
        Pessoa Update(Pessoa person);
        void Delete(long id);
    }
}
