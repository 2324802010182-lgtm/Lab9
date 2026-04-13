using ASC.Web.Configuration;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace ASC.Solution.Services
{
    public class AuthMessageSender : IEmailSender
    {
        private readonly ApplicationSettings _settings;

        public AuthMessageSender(IOptions<ApplicationSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            using var client = new SmtpClient(_settings.SMTPServer, _settings.SMTPPort)
            {
                Credentials = new NetworkCredential(_settings.SMTPAccount, _settings.SMTPPassword),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.SMTPAccount),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            await client.SendMailAsync(mailMessage);
        }
    }
}