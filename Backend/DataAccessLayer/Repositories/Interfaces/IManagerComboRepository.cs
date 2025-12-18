using DomainAccessLayer.Models;
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

        Task<Combo?> GetComboWithItemsAsync(int id);
        Task<List<MenuItem>> GetMenuItemsByIdsAsync(List<int> menuItemIds);
        IQueryable<MenuItem> QueryMenuItems();
        Task UpdateComboAsync(Combo combo, List<ComboItem> newItems);
        Task AddComboAsync(Combo combo, List<ComboItem> items);
        //topnew
        Task<List<MenuItem>> GetTop5NewMenuItemsAsync();

        Task ChangeStatusComboAsync(int idcombo, bool status);
    }
}
