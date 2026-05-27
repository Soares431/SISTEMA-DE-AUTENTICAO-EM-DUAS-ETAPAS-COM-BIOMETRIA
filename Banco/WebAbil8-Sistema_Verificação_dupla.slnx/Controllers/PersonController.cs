using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;
namespace WebAbil8_Sistema_Verificação_dupla.slnx.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PersonController : ControllerBase
    {
        private IPessoaRepository _pessoaRepository;
        private readonly ILogger<PersonController> _logger;

        public PersonController(IPessoaRepository pessoaRepository,
            ILogger<PersonController> logger)
        {
            _pessoaRepository = pessoaRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            _logger.LogInformation("Fetching all persons");
            return Ok(await _pessoaRepository.ListarTodos());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            _logger.LogInformation("Fetching person with ID {id}", id);
            var person = await _pessoaRepository.BuscarPorId(id);
            if (person == null)
            {
                _logger.LogWarning("Person with ID {id} not found!", id);
                return NotFound();
            }
            return Ok(person);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Pessoa person)
        {
            _logger.LogInformation("Create new Person: {name}", person.Nome);
            var createdPerson = await _pessoaRepository.Adicionar(person);
            if (createdPerson == null)
            {
                _logger.LogError("Failed to create person");
                return NotFound();
            }
            _logger.LogDebug("Person created successfully: {name}", createdPerson.Nome);
            return Ok(createdPerson);
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] Pessoa person)
        {
            _logger.LogInformation("Update Person with ID {id}", person.Id);
            var updatedPerson = await _pessoaRepository.Atualizar(person);
            if (updatedPerson == null)
            {
                _logger.LogError("Failed to update person with ID {id}", person.Id);
                return NotFound();
            }
            _logger.LogDebug("Person updated successfully: {name}", updatedPerson.Nome);
            return Ok(updatedPerson);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("Delete Person with ID {id}", id);
            await _pessoaRepository.Remover(id);
            _logger.LogDebug("Person with ID {id} deleted successfully", id);
            return NoContent();
        }
    }

}
