using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class SupplierListDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Contact { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } // Giả định trường này được quản lý

        // Thống kê tổng hợp
        public int TotalOrders { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime? LastOrder { get; set; }
    }
}
