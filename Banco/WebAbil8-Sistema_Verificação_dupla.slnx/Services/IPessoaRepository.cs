using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface IPessoaRepository
    {
        Task<Pessoa> Adicionar(Pessoa pessoa);
        Task<Pessoa> BuscarPorId(long id);
        Task<Pessoa?> BuscarPorCodigoUsuario(string codigoUsuario);
        Task<Pessoa> BuscarPorCPF(string cpf);
        Task<List<Pessoa>> ListarTodos();
        Task<Pessoa> Atualizar(Pessoa pessoa);
        Task Remover(long id);
        Task AlterarStatus(long pessoaId, bool status);
        Task<Pessoa> MarcarBiometriaCadastrada(long pessoaId);
        Task<Pessoa> SalvarTemplate(long pessoaId, byte[] template);
        Task<Pessoa> AtualizarUltimoAcesso(long pessoaId);
        Task<Pessoa> AtualizarSenha(long pessoaId, string novaSenhaClear, string novoSenhaHash);

        // ── Slots AS608 (Arduino) ────────────────────────────────────────
        // Retorna o primeiro slot 1-127 não ocupado por nenhuma pessoa ativa.
        // Retorna null se sensor estiver lotado.
        Task<int?> AlocarSlotAs608Livre();
        // Persiste o slot alocado pra pessoa.
        Task DefinirSlotAs608(long pessoaId, int slot);
        // Move SlotAs608 → SlotAs608ParaApagar e zera SlotAs608. Worker drena depois.
        Task EnfileirarSlotParaApagar(long pessoaId);
        // Lista pessoas com SlotAs608ParaApagar preenchido — Worker consome.
        Task<List<Pessoa>> ListarSlotsPendentesApagar();
        // Worker chama após Arduino confirmar delete via EVT|FINGER|DELETED.
        Task LimparSlotPendenteApagar(long pessoaId);
    }
}
