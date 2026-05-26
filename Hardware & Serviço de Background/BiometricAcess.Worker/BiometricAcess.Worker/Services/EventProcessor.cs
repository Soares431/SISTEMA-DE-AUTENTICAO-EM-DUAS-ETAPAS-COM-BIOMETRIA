using BiometricAcess.Worker.Models;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;
using InfraestruturaBloco1.Services;

namespace BiometricAcess.Worker.Services
{
    public class EventProcessor : IEventProcessor
    {
        private readonly IPessoaRepository _pessoaRepository;
        private readonly IAmbientePessoaRepository _ambientePessoaRepository;
        private readonly IDispositivoT50Repository _dispositivoRepository;
        private readonly IAmbienteRepository _ambienteRepository;
        private readonly ITentativaAcessoRepository _tentativaRepository;
        private readonly IConfiguracaoRepository _configuracaoRepository;
        private readonly IAnvizService _anvizService;
        private readonly CameraService _cameraService;

        public EventProcessor(
            IPessoaRepository pessoaRepository,
            IAmbientePessoaRepository ambientePessoaRepository,
            IDispositivoT50Repository dispositivoRepository,
            IAmbienteRepository ambienteRepository,
            ITentativaAcessoRepository tentativaRepository,
            IConfiguracaoRepository configuracaoRepository,
            IAnvizService anvizService,
            CameraService cameraService)
        {
            _pessoaRepository = pessoaRepository;
            _ambientePessoaRepository = ambientePessoaRepository;
            _dispositivoRepository = dispositivoRepository;
            _ambienteRepository = ambienteRepository;
            _tentativaRepository = tentativaRepository;
            _configuracaoRepository = configuracaoRepository;
            _anvizService = anvizService;
            _cameraService = cameraService;
        }

        public async Task Processar(EventoAcesso evento)
        {
            var dispositivo = BuscarDispositivoPorIp(evento.IpDispositivo);
            if (dispositivo == null)
            {
                Console.WriteLine($"Dispositivo não encontrado para IP: {evento.IpDispositivo}");
                return;
            }

            // C2: busca o Ambiente pelo DispositivoT50Id — antes usava o ID do dispositivo como ambienteId
            var ambiente = _ambienteRepository.ListarTodos()
                .FirstOrDefault(a => a.DispositivoT50Id == dispositivo.Id);
            if (ambiente == null)
            {
                Console.WriteLine($"Nenhum ambiente configurado para o dispositivo {dispositivo.Id}");
                return;
            }

            var pessoa = await _pessoaRepository.BuscarPorId(evento.PessoaID);

            if (pessoa == null)
            {
                await FluxoNaoCadastrado(evento, ambiente.Id);
                return;
            }

            if (pessoa.Status == "inativo")
            {
                await FluxoAcessoNegado(evento, pessoa, ambiente.Id, "inativo");
                return;
            }

            if (!_ambientePessoaRepository.PessoaTemAcesso(ambiente.Id, pessoa.Id))
            {
                await FluxoAcessoNegado(evento, pessoa, ambiente.Id, "sem_permissao");
                return;
            }

            if (evento.TipoVerificacao == "senha_id"
                && pessoa.modoAcesso == "digital_e_senha"
                && pessoa.biometriaCadastrada == null)
            {
                await FluxoPrimeiroAcesso(evento, pessoa, ambiente.Id);
                return;
            }

            await FluxoAcessoNormal(evento, pessoa, ambiente);
        }

        private DispositivoT50? BuscarDispositivoPorIp(string ip)
        {
            return _dispositivoRepository.ListarTodos()
                .FirstOrDefault(d => d.EnderecoIP == ip);
        }

        private async Task FluxoPrimeiroAcesso(EventoAcesso evento, Pessoa pessoa, int ambienteId)
        {
            Console.WriteLine($"Primeiro acesso — Pessoa: {pessoa.Id} | Iniciando captura de digital...");

            // I1: EnrollFingerprint retorna o template diretamente — elimina chamada redundante ao DownloadTemplate
            var template = _anvizService.IniciarCapturaDigital((int)pessoa.Id);
            if (template != null)
            {
                await _pessoaRepository.SalvarTemplate(pessoa.Id, template);
                await _pessoaRepository.MarcarBiometriaCadastrada(pessoa.Id);
                _anvizService.AlterarModo((int)pessoa.Id, "digital_id");
                Console.WriteLine($"Biometria cadastrada — Pessoa: {pessoa.Id}");
            }

            await _pessoaRepository.AtualizarUltimoAcesso(pessoa.Id);
            await RegistrarTentativa(evento, pessoa, ambienteId, true, null);
        }

        private async Task FluxoAcessoNormal(EventoAcesso evento, Pessoa pessoa, Ambiente ambiente)
        {
            Console.WriteLine($"Acesso liberado — Pessoa: {pessoa.Id} | Tipo: {evento.TipoVerificacao}");
            await _pessoaRepository.AtualizarUltimoAcesso(pessoa.Id);

            var tentativa = await RegistrarTentativa(evento, pessoa, ambiente.Id, true, null);

            // HW-16 — aguarda gravação da câmera; I5: usa TempoEsperaGravacaoSeg do ambiente
            var gravacaoPath = await _cameraService.MonitorarNovoArquivo(ambiente.Id, evento.DataHora, ambiente.TempoEsperaGravacaoSeg);
            if (gravacaoPath != null)
            {
                // C3: usa Atualizar para evitar registro duplicado (antes chamava Registrar novamente)
                tentativa.GravacaoPath = gravacaoPath;
                _tentativaRepository.Atualizar(tentativa);
                Console.WriteLine($"Gravação associada — Pessoa: {pessoa.Id} | Path: {gravacaoPath}");
            }
        }

        private async Task FluxoNaoCadastrado(EventoAcesso evento, int ambienteId)
        {
            Console.WriteLine($"Acesso negado — Pessoa {evento.PessoaID} não cadastrada no sistema");
            await RegistrarTentativa(evento, null, ambienteId, false, "nao_cadastrado");
        }

        private async Task FluxoAcessoNegado(EventoAcesso evento, Pessoa pessoa, int ambienteId, string motivo)
        {
            Console.WriteLine($"Acesso negado — Pessoa: {pessoa.Id} | Motivo: {motivo}");
            await RegistrarTentativa(evento, pessoa, ambienteId, false, motivo);
        }

        private async Task<TentativaAcesso> RegistrarTentativa(EventoAcesso evento, Pessoa? pessoa, int ambienteId, bool acessoLiberado, string? motivo)
        {
            // C4: preenche DataExpiracao com base na configuração (antes ficava null, job de limpeza nunca removia)
            var config = await _configuracaoRepository.BuscarPorChave();
            var retencaoDias = config?.RetencaoGravacoesTentativasDias ?? 90;

            var tentativa = new TentativaAcesso
            {
                PessoaId = pessoa?.Id,
                AmbienteId = ambienteId,
                DataHora = evento.DataHora,
                AcessoLiberado = acessoLiberado,
                MotivoNegacao = motivo,
                TipoVerificacao = evento.TipoVerificacao,
                DataExpiracao = DateTime.UtcNow.AddDays(retencaoDias)
            };

            _tentativaRepository.Registrar(tentativa);
            return tentativa;
        }
    }
}
