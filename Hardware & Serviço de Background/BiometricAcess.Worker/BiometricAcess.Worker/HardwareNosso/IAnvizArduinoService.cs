namespace BiometricAcess.Worker.HardwareNosso;

public interface IAnvizArduinoService
{
    // primeiroAcesso=true → pessoa digital_e_senha sem biometria ainda (LCD "1o Acesso").
    // primeiroAcesso=false → pessoa somente_senha (LCD "Acesso por senha"), evita confundir
    // o usuário fazendo achar que é cadastro novo a cada acesso.
    void NotificarPedirSenha(int pessoaId, bool primeiroAcesso);
    // slotAs608: o slot interno do sensor (1-127) onde a digital vai ser armazenada.
    // O Worker calcula isso a partir do Pessoa.Id pra caber no AS608 (limite de 127 templates).
    void NotificarPrimeiroAcesso(int pessoaId, int slotAs608);
    void NotificarVerificarDigital(int pessoaId);
    void NotificarAcessoNegado(int pessoaId, string motivo);
    // Dispara buzzer de OK + abre o relé/solenoide pelo tempo informado.
    // Centraliza a abertura da porta — disparado pelo EventProcessor quando o acesso
    // é confirmado (digital OK, 1º acesso concluído, ou somente_senha validada).
    void NotificarAcessoLiberado(int duracaoSegundos = 5);
    // Pede ao Arduino pra apagar o template do slot informado no AS608.
    // Worker chama isso quando admin reseta biometria ou inativa pessoa —
    // libera o slot pra reuso (sensor tem só 127).
    void NotificarApagarDigital(int slotAs608);
}