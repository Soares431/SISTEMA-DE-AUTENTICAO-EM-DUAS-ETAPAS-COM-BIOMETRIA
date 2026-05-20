using BiometricAcess.Worker.Models;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;

namespace BiometricAcess.Worker.Services
{
    public class EventProcessor : IEventProcessor
    {
        private readonly IPessoaRepository _pessoaRepository;
        private readonly IAmbientePessoaRepository _ambientePessoaRepository;
        private readonly IDispositivoT50Repository _dispositivoRepository;
        private readonly ITentativaAcessoRepository _tentativaRepository;
        private readonly IAnvizService _anvizService;

        public EventProcessor(
            IPessoaRepository pessoaRepository,
            IAmbientePessoaRepository ambientePessoaRepository,
            IDispositivoT50Repository dispositivoRepository,
            ITentativaAcessoRepository tentativaRepository,
            IAnvizService anvizService)
        {
            _pessoaRepository = pessoaRepository;
            _ambientePessoaRepository = ambientePessoaRepository;
            _dispositivoRepository = dispositivoRepository;
            _tentativaRepository = tentativaRepository;
            _anvizService = anvizService;
        }

        public async Task Processar(EventoAcesso evento)
        {
            var dispositivo = BuscarDispositivoPorIp(evento.IpDispositivo);
            if (dispositivo == null)
            {
                Console.WriteLine($"Dispositivo não encontrado para IP: {evento.IpDispositivo}");
                return;
            }

            var ambiente = dispositivo.Id;
            var pessoa = await _pessoaRepository.BuscarPorId(evento.PessoaID);

            if (pessoa == null)
            {
                FluxoNaoCadastrado(evento, ambiente);
                return;
            }

            if (pessoa.Status == "inativo")
            {
                FluxoAcessoNegado(evento, pessoa, ambiente, "inativo");
                return;
            }

            if (!_ambientePessoaRepository.PessoaTemAcesso(ambiente, pessoa.Id))
            {
                FluxoAcessoNegado(evento, pessoa, ambiente, "sem_permissao");
                return;
            }

            if (evento.TipoVerificacao == "senha_id"
                && pessoa.modoAcesso == "digital_e_senha"
                && pessoa.biometriaCadastrada == null)
            {
                await FluxoPrimeiroAcesso(evento, pessoa, ambiente);
                return;
            }

            await FluxoAcessoNormal(evento, pessoa, ambiente);
        }

        private DispositivoT50 BuscarDispositivoPorIp(string ip)
        {
            var dispositivos = _dispositivoRepository.ListarTodos();
            foreach (var dispositivo in dispositivos)
            {
                if (dispositivo.EnderecoIP == ip)
                    return dispositivo;
            }
            return null;
        }

        private async Task FluxoPrimeiroAcesso(EventoAcesso evento, Pessoa pessoa, int ambienteId)
        {
            Console.WriteLine($"Primeiro acesso — Pessoa: {pessoa.Id} | Iniciando captura de digital...");

            _anvizService.IniciarCapturaDigital((int)pessoa.Id);

            var template = _anvizService.DownloadTemplate((int)pessoa.Id);
            if (template != null)
            {
                await _pessoaRepository.SalvarTemplate(pessoa.Id, template);
                await _pessoaRepository.MarcarBiometriaCadastrada(pessoa.Id);
                _anvizService.AlterarModo((int)pessoa.Id, "digital_id");
                Console.WriteLine($"Biometria cadastrada — Pessoa: {pessoa.Id}");
            }

            await _pessoaRepository.AtualizarUltimoAcesso(pessoa.Id);
            RegistrarTentativa(evento, pessoa, ambienteId, true, null);
        }

        private async Task FluxoAcessoNormal(EventoAcesso evento, Pessoa pessoa, int ambienteId)
        {
            Console.WriteLine($"Acesso liberado — Pessoa: {pessoa.Id} | Tipo: {evento.TipoVerificacao}");
            await _pessoaRepository.AtualizarUltimoAcesso(pessoa.Id);
            RegistrarTentativa(evento, pessoa, ambienteId, true, null);
        }

        private void FluxoNaoCadastrado(EventoAcesso evento, int ambienteId)
        {
            Console.WriteLine($"Acesso negado — Pessoa {evento.PessoaID} não cadastrada no sistema");
            RegistrarTentativa(evento, null, ambienteId, false, "nao_cadastrado");
        }

        private void FluxoAcessoNegado(EventoAcesso evento, Pessoa pessoa, int ambienteId, string motivo)
        {
            Console.WriteLine($"Acesso negado — Pessoa: {pessoa.Id} | Motivo: {motivo}");
            RegistrarTentativa(evento, pessoa, ambienteId, false, motivo);
        }

        private void RegistrarTentativa(EventoAcesso evento, Pessoa pessoa, int ambienteId, bool acessoLiberado, string motivo)
        {
            var tentativa = new TentativaAcesso
            {
                PessoaId = pessoa != null ? (int)pessoa.Id : null,
                AmbienteId = ambienteId,
                DataHora = evento.DataHora,
                AcessoLiberado = acessoLiberado,
                MotivoNegacao = motivo,
                TipoVerificacao = evento.TipoVerificacao
            };

            _tentativaRepository.Registrar(tentativa);
        }
    }
}