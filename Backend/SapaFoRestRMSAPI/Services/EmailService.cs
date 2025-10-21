using System.Threading.Tasks;
using BusinessAccessLayer.Services.Interfaces;

namespace SapaFoRestRMSAPI.Services
{
    public class EmailService : IEmailService
    {
        public async Task SendAsync(string toEmail, string subject, string body)
        {
            // TODO: Plug in real email provider (SMTP/SendGrid, etc.)
            await Task.CompletedTask;
        }
    }
}


