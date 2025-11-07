using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class SupplierStatisticDTO
    {
        public SupplierDTO Supplier { get; set; } = new SupplierDTO();

        // Thông tin thống kê
        public decimal TotalQuantityForIngredient { get; set; }   // Tổng nhập của nguyên liệu được tìm kiếm
        public string? MainIngredientName { get; set; }            // Nguyên liệu chuyên (nhiều nhất)
        public decimal MainIngredientTotalQuantity { get; set; }   // Tổng nhập của nguyên liệu chuyên
    }
}
