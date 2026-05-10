using BCrypt.Net;

namespace InfraestruturaBloco1.Services
{
    public class PasswordService
    {
        // SEC-01: Gerar hash com BCrypt
        public string HashPassword(string plainPassword)
        {
            return BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 10);
        }

        // SEC-02: Verificar hash
        public bool VerifyPassword(string plainPassword, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
        }

        // SEC-04: Gerar senha aleatória única de 6 dígitos
        public string GerarSenhaAleatoria()
        {
            var random = new Random();
            string senha;

            do
            {
                senha = random.Next(100000, 999999).ToString();
            }
            while (SenhaEhTrivial(senha));

            return senha;
        }

        // Método auxiliar para evitar senhas triviais
        private bool SenhaEhTrivial(string senha)
        {
            var triviais = new List<string>
            {
                "123456", "654321", "111111", "000000", "121212", "222222"
            };

            return triviais.Contains(senha);
        }
    }
}

