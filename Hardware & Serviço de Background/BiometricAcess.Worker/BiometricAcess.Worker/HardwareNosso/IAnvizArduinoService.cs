namespace BiometricAcess.Worker.HardwareNosso;

public interface IAnvizArduinoService
{
    void NotificarPedirSenha(int pessoaId);
    // slotAs608: o slot interno do sensor (1-127) onde a digital vai ser armazenada.
    // O Worker calcula isso a partir do Pessoa.Id pra caber no AS608 (limite de 127 templates).
    void NotificarPrimeiroAcesso(int pessoaId, int slotAs608);
    void NotificarVerificarDigital(int pessoaId);
    void NotificarAcessoNegado(int pessoaId, string motivo);
}