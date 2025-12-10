using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class SupplierDetailDto : SupplierListDto
    {
        public double AvgOrderPerMonth { get; set; }
        public int TotalProductsSupplied { get; set; }
        public decimal TotalValueYTD { get; set; }
    }

    public class OrderHistoryDto
    {
        public string Id { get; set; } = null!; // PurchaseOrderId
        public DateTime? Date { get; set; }
        public decimal Total { get; set; }
        public string? Status { get; set; }
        public int Items { get; set; } // Số lượng mặt hàng
    }

    // File: Application/DTOs/Supplier/SupplierIngredientDto.cs
    public class SupplierIngredientDto
    {
        public int IngredientId { get; set; }
        public string Name { get; set; } = null!;
        public string Unit { get; set; } = null!; // Đơn vị snapshot
        public decimal LastPrice { get; set; }
        public decimal AvgPrice { get; set; }
        public int Frequency { get; set; } // Tần suất mua (Count PurchaseOrderDetails)
    }

    // File: Application/DTOs/Supplier/TopSupplierDto.cs
    public class TopSupplierDto
    {
        public string Name { get; set; } = null!;
        public decimal Value { get; set; }
        public int Orders { get; set; }
    }
}
