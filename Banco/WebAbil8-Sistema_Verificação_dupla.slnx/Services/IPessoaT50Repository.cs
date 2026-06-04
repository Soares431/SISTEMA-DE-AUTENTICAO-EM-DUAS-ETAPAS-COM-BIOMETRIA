using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface IPessoaT50Repository
    {
        // Cadastra a biometria de uma pessoa em um T50. Incrementa o contador do T50.
        // Idempotente: se já existe, retorna o existente.
        PessoaT50 Adicionar(long pessoaId, int t50Id);

        // Remove o vínculo e decrementa o contador. No-op se não existe.
        void Remover(long pessoaId, int t50Id);

        // Lista todos os T50 onde a pessoa está cadastrada.
        List<DispositivoT50> ListarT50sDaPessoa(long pessoaId);

        // Lista todas as pessoas cadastradas no T50.
        List<Pessoa> ListarPessoasDoT50(int t50Id);

        // True se a pessoa está cadastrada neste T50.
        bool EstaCadastrada(long pessoaId, int t50Id);

        // Conta quantas pessoas estão cadastradas no T50 (deve bater com DigitaisCadastradas).
        int ContarPessoasNoT50(int t50Id);
    }
}
