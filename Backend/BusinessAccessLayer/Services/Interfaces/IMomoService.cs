using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IMomoService
    {
        Task<string> CreatePaymentAsync(decimal amount, string orderId, string orderInfo);
    }
}
