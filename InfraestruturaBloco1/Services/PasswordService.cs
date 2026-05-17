using WebAbil8_Sistema_Verificação_dupla.slnx.Services;

namespace InfraestruturaBloco1.Services;

public class PasswordService
{
    private readonly ISenhaRepository _senhaRepo;

    public PasswordService(ISenhaRepository senhaRepo)
    {
        _senhaRepo = senhaRepo;
    }

    public string GerarHash(string senha) =>
        BCrypt.Net.BCrypt.HashPassword(senha, workFactor: 10);

    public bool VerificarHash(string senha, string hash) =>
        BCrypt.Net.BCrypt.Verify(senha, hash);

    public async Task<string> GerarSenhaAleatoriaAsync(int pessoaId)
    {
        var disponivel = await _senhaRepo.BuscarDisponivel(pessoaId);
        return disponivel?.Senha
            ?? throw new InvalidOperationException("Nenhuma senha disponível no banco.");
    }
}