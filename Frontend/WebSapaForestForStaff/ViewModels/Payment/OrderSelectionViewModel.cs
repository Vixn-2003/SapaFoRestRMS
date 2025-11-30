using System.Collections.Generic;
using WebSapaForestForStaff.DTOs.Payment;

namespace WebSapaForestForStaff.ViewModels.Payment
{
    /// <summary>
    /// ViewModel cho màn hình Order Selection của thu ngân.
    /// Chứa hai danh sách: đơn chờ thanh toán và đơn đã thanh toán.
    /// </summary>
    public class OrderSelectionViewModel
    {
        public DateOnly SelectedDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        public List<OrderDto> PendingOrders { get; set; } = new();

        public List<OrderDto> PaidOrders { get; set; } = new();
    }
}

