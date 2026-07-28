using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace PremierAuto.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var fromMail = "MailAdd";
            var fromPassword = "SecretKey"; 

            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromMail, fromPassword)
            };

            var mailMessage = new MailMessage(
                from: fromMail,
                to: email,
                subject: subject,
                body: htmlMessage
            )
            {
                IsBodyHtml = true // Permite trimiterea de emailuri formatate HTML
            };

            return client.SendMailAsync(mailMessage);
        }
    }
}