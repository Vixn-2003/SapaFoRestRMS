using BusinessAccessLayer.DTOs.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface ISupplierManagerService
    {
        // Lấy danh sách nhà cung cấp kèm thống kê tổng hợp (List View)
        Task<List<SupplierListDto>> GetSuppliersSummaryAsync();

        // Lấy lịch sử đơn hàng của nhà cung cấp (Detail Tab: Orders)
        Task<List<OrderHistoryDto>> GetHistoryAsync(int supplierId);

        // Lấy danh mục nguyên liệu/sản phẩm nhà cung cấp cung cấp (Detail Tab: Products)
        Task<List<SupplierIngredientDto>> GetProductsAsync(int supplierId);

        // Lấy Top Suppliers (Dashboard)
        Task<List<TopSupplierDto>> GetTopSuppliersAsync(DateTime startDate, DateTime endDate);
        Task<bool> SoftDeleteSupplierAsync(int id);
    }
}
