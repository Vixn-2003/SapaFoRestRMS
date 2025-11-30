using WebSapaForestForStaff.DTOs.Payment;

namespace WebSapaForestForStaff.ViewModels.Payment
{
    public class PaymentConfirmViewModel
    {
        public int OrderId { get; set; }
        public PaymentSessionDto Session { get; set; } = new();
    }
}

