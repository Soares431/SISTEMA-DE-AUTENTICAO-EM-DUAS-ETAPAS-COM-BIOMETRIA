using BiometricAcess.Worker.Models;
namespace BiometricAcess.Worker.Simulador{
    internal class T50MSimulador{
        private static readonly Random _random = new Random();

        public static EventoAcesso gerarEvento()
        {


            int pessoaID = _random.Next(1, 6);
            string tipoVerificacao;
            if (_random.Next(2) == 0)
            {
                tipoVerificacao = "digital_id";
            }
            else
            {
                tipoVerificacao = "senha_id";
            }
            bool acessoLiberado;

            if (_random.Next(2) == 0)
            {
                acessoLiberado = true;
            }
            else
            {
                acessoLiberado = false;
            }

            return new EventoAcesso
            {
                PessoaID = pessoaID,
                TipoVerificacao = tipoVerificacao,
                AcessoLiberado = acessoLiberado,
                DataHora = DateTime.Now,
                IpDispositivo = "192.168.0.218"
            };


        }
    }
}
