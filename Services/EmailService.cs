using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration; // OK na ito

namespace TaskOrganizer.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public void SendRegistrationEmail(string to, string name)
        {
            // ❗ SAFETY CHECK at SAFE PARSING ❗
            var fromEmail = _config["EmailSettings:FromEmail"];
            var password = _config["EmailSettings:Password"];
            var smtp = _config["EmailSettings:SmtpServer"];

            // Safe parsing ng Port: Subukan basahin ang Port, kung hindi, gamitin ang 587.
            if (!int.TryParse(_config["EmailSettings:Port"], out int port))
            {
                port = 587; // Default value kung hindi mahanap sa config
            }

            if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(smtp))
                throw new InvalidOperationException("One or more required Email settings are missing or invalid in appsettings.json.");

            var body = $"Hi {name},\n\nYour account has been successfully created.\nYou may now log in using your registered email.\n\nThank you!";

            var client = new SmtpClient(smtp, port)
            {
                Credentials = new NetworkCredential(fromEmail, password),
                EnableSsl = true
            };

            var mail = new MailMessage(fromEmail, to)
            {
                Subject = "Registration Successful",
                Body = body
            };

            // Gumamit ng SendMailAsync() kung maaari, pero dahil void ang method, OK lang ang Send()
            client.Send(mail);
        }
    }
}