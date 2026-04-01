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
        Task SendEmailWithAttachmentAsync(string to, string subject, string body, byte[] attachment, string fileName);
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
            await SendEmailInternalAsync(to, subject, body, null, null);
        }

        public async Task SendEmailWithAttachmentAsync(string to, string subject, string body, byte[] attachment, string fileName)
        {
            await SendEmailInternalAsync(to, subject, body, attachment, fileName);
        }

        private async Task SendEmailInternalAsync(string to, string subject, string body, byte[]? attachmentData, string? fileName)
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

                if (attachmentData != null && !string.IsNullOrEmpty(fileName))
                {
                    var ms = new System.IO.MemoryStream(attachmentData);
                    var attachment = new Attachment(ms, fileName, "text/csv");
                    mailMessage.Attachments.Add(attachment);
                }

                await client.SendMailAsync(mailMessage);
                
                // Note: MailMessage should be disposed, but client.SendMailAsync uses it asynchronously.
                // In SmtpClient, after SendMailAsync completes, you can dispose the message.
                // However, modern SmtpClient usage often avoids disposing if it causes issues.
            }
        }
    }
}

