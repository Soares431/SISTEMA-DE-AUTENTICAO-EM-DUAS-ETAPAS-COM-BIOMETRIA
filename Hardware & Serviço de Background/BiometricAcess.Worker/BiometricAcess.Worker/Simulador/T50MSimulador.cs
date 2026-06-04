using BiometricAcess.Worker.Models;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;

namespace BiometricAcess.Worker.Simulador
{
    internal class T50MSimulador
    {
        private static readonly Random _random = new Random();

        // Gera evento usando uma Pessoa real do banco (pega CodigoUsuario).
        // Fallback: ID aleatório no range 100000-999999 (vai cair em "nao_cadastrado").
        public static EventoAcesso GerarEventoComBanco(IServiceScopeFactory scopeFactory)
        {
            int pessoaID;
            string ipDispositivo = "192.168.0.218"; // fallback se não houver T50 cadastrado
            try
            {
                using var scope = scopeFactory.CreateScope();
                var pessoaRepo = scope.ServiceProvider.GetRequiredService<IPessoaRepository>();
                var pessoas = pessoaRepo.ListarTodos().GetAwaiter().GetResult()
                    .Where(p => !string.IsNullOrEmpty(p.CodigoUsuario))
                    .ToList();

                if (pessoas.Count > 0 && _random.Next(10) > 1)
                {
                    // 80% das vezes: pessoa real → testa fluxos liberado/inativo/sem_permissao
                    var pessoa = pessoas[_random.Next(pessoas.Count)];
                    pessoaID = int.Parse(pessoa.CodigoUsuario!);
                }
                else
                {
                    // 20% das vezes (ou banco vazio): ID inexistente → testa "nao_cadastrado"
                    pessoaID = _random.Next(100000, 1000000);
                }

                // IP do simulador: pega o IP de um T50 ALEATÓRIO entre os cadastrados.
                // Isso garante que o EventProcessor encontre o T50 e gere a tentativa.
                // Sem isso, o simulador mandava IP hardcoded 192.168.0.218 que não bate
                // com os IPs reais cadastrados pelo admin (ex: 192.168.0.10) e os eventos
                // eram silenciosamente ignorados.
                var dispositivoRepo = scope.ServiceProvider.GetRequiredService<IDispositivoT50Repository>();
                var t50s = dispositivoRepo.ListarTodos();
                if (t50s.Count > 0)
                {
                    ipDispositivo = t50s[_random.Next(t50s.Count)].EnderecoIP;
                }
            }
            catch
            {
                pessoaID = _random.Next(100000, 1000000);
            }

            var tipoVerificacao = _random.Next(2) == 0 ? "digital_id" : "senha_id";

            return new EventoAcesso
            {
                PessoaID = pessoaID,
                TipoVerificacao = tipoVerificacao,
                AcessoLiberado = false, // decidido pelo EventProcessor com base no banco
                DataHora = DateTime.UtcNow,
                IpDispositivo = ipDispositivo
            };
        }

        // Mantido para compatibilidade — gera evento sem consultar banco.
        // ID no range correto (100000-999999) mas provavelmente cairá em "nao_cadastrado".
        public static EventoAcesso gerarEvento()
        {
            return new EventoAcesso
            {
                PessoaID = _random.Next(100000, 1000000),
                TipoVerificacao = _random.Next(2) == 0 ? "digital_id" : "senha_id",
                AcessoLiberado = false,
                DataHora = DateTime.UtcNow,
                IpDispositivo = "192.168.0.218"
            };
        }
    }
}
