namespace BiometricAcess.Worker.HardwareNosso;

public interface IAnvizArduinoService
{
    void NotificarPrimeiroAcesso(int pessoaId);
    void NotificarVerificarDigital(int pessoaId);
    void NotificarAcessoNegado(int pessoaId, string motivo);
}