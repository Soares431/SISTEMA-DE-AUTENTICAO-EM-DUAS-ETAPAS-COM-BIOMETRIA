using BiometricAcess.Worker.Models;
using BiometricAcess.Worker.Services;

namespace BiometricAcess.Worker.Simulador
{
    public class EventProcessorSimulador : IEventProcessor
    {
        // Dados mockados — substituir pelos repositórios do banco quando estiver pronto
        private readonly List<int> _pessoasAtivas = new List<int> { 1, 2, 3, 4, 5 };
        private readonly List<int> _pessoasInativas = new List<int> { 6, 7 };
        private readonly List<int> _pessoasComBiometria = new List<int> { 1, 2, 3 };

        public Task Processar(EventoAcesso evento)
        {
            Console.WriteLine($"Processando evento — Pessoa: {evento.PessoaID} | Tipo: {evento.TipoVerificacao}");

            if (_pessoasInativas.Contains(evento.PessoaID))
            {
                FluxoAcessoNegado(evento, "inativo");
                return Task.CompletedTask;
            }

            if (!_pessoasAtivas.Contains(evento.PessoaID))
            {
                FluxoNaoCadastrado(evento);
                return Task.CompletedTask;
            }

            if (evento.TipoVerificacao == "senha_id" && !_pessoasComBiometria.Contains(evento.PessoaID))
            {
                FluxoPrimeiroAcesso(evento);
                return Task.CompletedTask;
            }

            FluxoAcessoNormal(evento);
            return Task.CompletedTask;
        }

        private void FluxoPrimeiroAcesso(EventoAcesso evento)
        {
            Console.WriteLine($"Primeiro acesso — Pessoa: {evento.PessoaID} | Biometria será cadastrada");
            _pessoasComBiometria.Add(evento.PessoaID);
        }

        private void FluxoAcessoNormal(EventoAcesso evento)
        {
            Console.WriteLine($"Acesso liberado — Pessoa: {evento.PessoaID} | Tipo: {evento.TipoVerificacao}");
        }

        private void FluxoNaoCadastrado(EventoAcesso evento)
        {
            Console.WriteLine($"Acesso negado — Pessoa {evento.PessoaID} não cadastrada no sistema");
        }

        private void FluxoAcessoNegado(EventoAcesso evento, string motivo)
        {
            Console.WriteLine($"Acesso negado — Pessoa: {evento.PessoaID} | Motivo: {motivo}");
        }
    }
}