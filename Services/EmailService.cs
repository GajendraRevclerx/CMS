using CMS.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace CMS.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            using (var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort))
            {
                
                client.EnableSsl = false;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPass);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_settings.SmtpUser, "CCMS Automated Report"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(to);

                await client.SendMailAsync(mailMessage);
            }
        }
    }
}
