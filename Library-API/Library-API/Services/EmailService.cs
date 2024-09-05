using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Library_API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void SendEmail(string toEmail, string subject, string body)
        {
            var fromEmail = "aishakrez@gmail.com"; 
            var appPassword = _configuration.GetSection("Constants:AppPassword").Value ?? string.Empty;

            try
            {
                var message = new MailMessage()
                {
                    From = new MailAddress(fromEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(toEmail);

                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(fromEmail, appPassword),
                    EnableSsl = true,
                };

                smtpClient.SendCompleted += (s, e) =>
                {
                    if (e.Error != null)
                    {
                        _logger.LogError($"Error sending email: {e.Error.Message}");
                    }
                    else
                    {
                        _logger.LogInformation("Email sent successfully.");
                    }
                };

                smtpClient.SendAsync(message, null);
            }
            catch (SmtpException smtpEx)
            {
                // التعامل مع الاستثناءات الخاصة بالبريد الإلكتروني
                _logger.LogError($"SMTP Exception: {smtpEx.Message}");
            }
            catch (Exception ex)
            {
                // التعامل مع الاستثناءات العامة
                _logger.LogError($"Exception: {ex.Message}");
            }
        }
    }
}
