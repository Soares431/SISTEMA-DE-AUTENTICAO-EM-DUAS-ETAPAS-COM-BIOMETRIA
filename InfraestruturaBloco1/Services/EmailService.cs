using MailKit.Net.Smtp;
using MimeKit;

namespace InfraestruturaBloco1.Services
{
    public class EmailService
    {
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Sistema", "no-reply@empresa.com"));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.zimbra.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync("usuario@empresa.com", "senha");
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
