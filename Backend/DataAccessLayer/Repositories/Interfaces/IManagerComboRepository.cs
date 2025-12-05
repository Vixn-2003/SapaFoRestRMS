using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IManagerComboRepository : IRepository<Combo>
    {
        Task<IEnumerable<Combo>> GetManagerAllCombos();


        // 1. Lọc và tìm kiếm Menu (Trả về Entity)
        IQueryable<MenuItem> GetMenuItemsQuery(
 string search,
 decimal? minPrice,
 decimal? maxPrice,
 string sort,
 bool? comboStatus
);

        // 2. Các hàm CRUD cơ bản cho Combo
        Task<MenuItem> GetMenuItemByIdAsync(int id);
        Task<int> CreateComboAsync(Combo combo);
        Task AddComboItemsAsync(List<ComboItem> items);

        // 3 & 4. Top Seller (Trả về KeyValuePair: Món ăn - Số lượng bán)
        // Key là Entity MenuItem, Value là tổng số lượng bán
        Task<List<KeyValuePair<MenuItem, int>>> GetTopSellingMenuItemsAsync(int topN);
        Task<List<KeyValuePair<Combo, int>>> GetTopSellingCombosAsync(int topN);

        // 5. Thống kê (Trả về Dictionary: Thời gian - Số lượng)
        // Group theo ngày/tháng trả về dạng Tuple hoặc Dictionary chuẩn của C#
        Task<List<Tuple<DateTime, int>>> GetComboSalesByDateRangeAsync(DateTime fromDate, DateTime toDate);

        IQueryable<Combo> GetComboQuery(string? search, bool? isAvailable);

        Task<Combo?> GetComboByIdWithItemsAsync(int comboId);
        Task UpdateComboAsync(Combo combo, List<ComboItem> newItems);

        public async Task UpdateComboAsync(Combo combo, List<ComboItem> newItems)
        {
            // 1. Xóa toàn bộ các món cũ trong bảng trung gian của Combo này
            // Lưu ý: combo.ComboItems là list cũ đã được Load từ hàm GetComboByIdWithItemsAsync
            if (combo.ComboItems != null && combo.ComboItems.Any())
            {
                _context.ComboItems.RemoveRange(combo.ComboItems);
            }

            // 2. Gán danh sách món mới vào
            // EF Core sẽ tự động nhận biết đây là các bản ghi mới cần Insert
            foreach (var item in newItems)
            {
                item.ComboId = combo.ComboId; // Đảm bảo Foreign Key đúng
                _context.ComboItems.Add(item);
            }

            // 3. Update thông tin cơ bản của Combo (Name, Price...)
            // Vì 'combo' là object đang được tracking, ta chỉ cần gọi Update hoặc để EF tự detect
            _context.Combos.Update(combo);

            // 4. Lưu tất cả thay đổi trong 1 Transaction
            await _context.SaveChangesAsync();
        }
    }
}
