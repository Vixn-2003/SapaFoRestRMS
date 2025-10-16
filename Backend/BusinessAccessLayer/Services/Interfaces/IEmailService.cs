using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string body);
    }
}


