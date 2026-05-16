using MailKit.Net.Smtp;
using MimeKit;

namespace InfraestruturaBloco1.Services
{
    public class EmailService
    {
        private readonly string _smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST")!;
        private readonly int _smtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT")!);
        private readonly string _smtpUser = Environment.GetEnvironmentVariable("SMTP_USER")!;
        private readonly string _smtpPass = Environment.GetEnvironmentVariable("SMTP_PASS")!;

        public async Task SendEmailAsync(string to, string subject, string bodyHtml)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Sistema", _smtpUser));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = bodyHtml };

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(_smtpHost, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtpUser, _smtpPass);
                await client.SendAsync(message);
            }
            catch
            {
                // Fallback: exibir no console/admin
                Console.WriteLine($"[FALLBACK] Email para {to}:\n{bodyHtml}");
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }

        // INT-01: Enviar senha inicial
        public async Task EnviarSenhaInicial(string email, string nome, string senha)
        {
            var corpoHtml = $@"
                <h2>Bem-vindo, {nome}!</h2>
                <p>Sua senha inicial é: <strong>{senha}</strong></p>
                <p>Por favor, altere-a no primeiro acesso.</p>";
            
            await SendEmailAsync(email, "Senha Inicial", corpoHtml);
        }

        // INT-02: Reenviar senha
        public async Task ReenviarSenha(string email, string nome, string senha)
        {
            var corpoHtml = $@"
                <h2>Olá, {nome}!</h2>
                <p>Segue novamente sua senha: <strong>{senha}</strong></p>";
            
            await SendEmailAsync(email, "Reenvio de Senha", corpoHtml);
        }
    }
}

