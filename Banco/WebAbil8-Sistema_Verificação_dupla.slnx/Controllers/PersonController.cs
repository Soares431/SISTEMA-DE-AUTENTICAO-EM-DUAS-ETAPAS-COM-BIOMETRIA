using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;
namespace WebAbil8_Sistema_Verificação_dupla.slnx.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PersonController : ControllerBase
    {
        private IPessoaRepository _pessoaRepository;
        private readonly ILogger<PersonController> _logger;

        public PersonController(IPessoaRepository personServices,
            ILogger<PersonController> logger)
        {
            _pessoaRepository = personServices;
            _logger = logger;
        }

        [HttpGet] // FindALL
        public IActionResult Get()
        {
            _logger.LogInformation("Fetching all persons");
            return Ok(_pessoaRepository.ListarTodos());
        }

        [HttpGet("{id}")] // FindByID
        public IActionResult Get(long id )
        {
            _logger.LogInformation("Fetching person with ID {id}", id);
            var person = _pessoaRepository.BuscarPorId(id);
            if(person == null)
            {
                _logger.LogWarning("Person with ID {id} not found!", id);
                return NotFound();
            }
            return Ok(person);
        }

        [HttpPost] // Create
        public IActionResult Post([FromBody] Pessoa person) 
        {
            _logger.LogInformation("Create new Person: {name}", person.Nome);
            var createad_person = _pessoaRepository.Adicionar(person);
            if (createad_person == null)
            {
                _logger.LogError ("Failed to create person");
                return NotFound();
            }
            _logger.LogDebug("Person Create successfully with name: {name}", createad_person.Nome);
            return Ok(createad_person);
        }

        [HttpPut] // Update
        public IActionResult Put([FromBody] Pessoa person)
        {
            _logger.LogInformation("Update Person with ID {id}", person.Id);
            var createad_person = _pessoaRepository.Atualizar(person);
            if (createad_person == null)
            {
                _logger.LogError("Failed to Update person with ID {id}", person.Id);
                return NotFound();
            }
            _logger.LogDebug("Person Update successfully: {name}", createad_person.Nome);
            return Ok(createad_person);
        }

        [HttpDelete("{id}")] // delete
        public IActionResult Delete(int id)
        {
            _logger.LogInformation("Delete Person with ID {id}", id);
            _pessoaRepository.Remover(id);
            _logger.LogDebug("Person with ID {id} deleted successfully", id);
            return NoContent();
        }
    }

}
