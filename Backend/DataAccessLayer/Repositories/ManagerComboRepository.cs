using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    public class ManagerComboRepository : IManagerComboRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public ManagerComboRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public Task AddAsync(Combo entity)
        {
            throw new NotImplementedException();
        }

        public Task CreateAsync(Combo entity)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task DeleteByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Combo>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Combo> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Combo>> GetManagerAllCombos()
        {
            try
            {
                return await _context.Combos
                    .Where(c => c.IsAvailable == true)
                    .Include(c => c.ComboItems)
                        .ThenInclude(ci => ci.MenuItem)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                throw;
            }
        }

        public Task SaveChangesAsync()
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Combo entity)
        {
            throw new NotImplementedException();
        }


        // 1. Query Menu Items (Trả về IQueryable để Service phân trang)
        public IQueryable<MenuItem> GetMenuItemsQuery(
     string search,
     decimal? minPrice,
     decimal? maxPrice,
     string sort,
     bool? comboStatus       
 )
        {
            var query = _context.MenuItems
                .Include(a => a.Category)
                .Include(a => a.OrderDetails)
                    .ThenInclude(a => a.Combo)
                        .ThenInclude(a => a.ComboItems)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var keyword = search.Trim().ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(keyword));
            }

            if (minPrice.HasValue)
                query = query.Where(x => x.Price >= minPrice);

            if (maxPrice.HasValue)
                query = query.Where(x => x.Price <= maxPrice);

            // Lọc theo status của Combo
            if (comboStatus.HasValue)
            {
                query = query.Where(x =>
                    x.OrderDetails.Any(od => od.Combo.IsAvailable == comboStatus.Value)
                );
            }

            query = sort switch
            {
                "price_asc" => query.OrderBy(x => x.Price),
                "price_desc" => query.OrderByDescending(x => x.Price),
                _ => query.OrderBy(x => x.Name)
            };

            return query;
        }


        // 2. Create logic
        public async Task<MenuItem> GetMenuItemByIdAsync(int id) => await _context.MenuItems.FindAsync(id);

        public async Task<int> CreateComboAsync(Combo combo)
        {
            _context.Combos.Add(combo);
            await _context.SaveChangesAsync();
            return combo.ComboId;
        }

        public async Task AddComboItemsAsync(List<ComboItem> items)
        {
            _context.ComboItems.AddRange(items);
            await _context.SaveChangesAsync();
        }

        // 3. Top MenuItems (Trả về Entity và Int)
        public async Task<List<KeyValuePair<MenuItem, int>>> GetTopSellingMenuItemsAsync(int topN)
        {
            // Nhóm theo MenuItemId, đếm tổng Quantity, sau đó join ngược lại để lấy thông tin MenuItem
            var stats = await _context.OrderDetails
                .Where(od => od.Status == "Done" && od.MenuItemId != null)
                .GroupBy(od => od.MenuItemId)
                .Select(g => new { Id = g.Key, Total = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Total)
                .Take(topN)
                .ToListAsync();

            var result = new List<KeyValuePair<MenuItem, int>>();
            foreach (var stat in stats)
            {
                var item = await _context.MenuItems.FindAsync(stat.Id);
                if (item != null) result.Add(new KeyValuePair<MenuItem, int>(item, stat.Total));
            }
            return result;
        }

        // 4. Top Combos (Tương tự trên)
        public async Task<List<KeyValuePair<Combo, int>>> GetTopSellingCombosAsync(int topN)
        {
            var stats = await _context.OrderDetails
                .Where(od => od.Status == "Done" && od.ComboId != null)
                .GroupBy(od => od.ComboId)
                .Select(g => new { Id = g.Key, Total = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Total)
                .Take(topN)
                .ToListAsync();

            var result = new List<KeyValuePair<Combo, int>>();
            foreach (var stat in stats)
            {
                var combo = await _context.Combos.FindAsync(stat.Id);
                if (combo != null) result.Add(new KeyValuePair<Combo, int>(combo, stat.Total));
            }
            return result;
        }

        // 5. Thống kê doanh số theo ngày (Raw Data)
        public async Task<List<Tuple<DateTime, int>>> GetComboSalesByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.OrderDetails
                .Where(x => x.Status == "Done" && x.ComboId != null && x.CreatedAt >= fromDate && x.CreatedAt <= toDate)
                .GroupBy(x => x.CreatedAt.Date) // Group theo ngày
                .Select(g => Tuple.Create(g.Key, g.Sum(x => x.Quantity)))
                .ToListAsync();
        }


        public IQueryable<Combo> GetComboQuery(string search,bool? isAvailable)
        {
            var query = _context.Combos
                .Include(c => c.ComboItems)
                    .ThenInclude(ci => ci.MenuItem)
                .Include(c => c.OrderDetails)   // để tính lượt gọi
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var keyword = search.Trim().ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(keyword));
            }

            if (isAvailable.HasValue)
                query = query.Where(c => c.IsAvailable == isAvailable.Value);

            return query;
        }

        public async Task<Combo?> GetComboByIdWithItemsAsync(int comboId)
        {
            return await _context.Combos
                .Include(c => c.ComboItems)
                    .ThenInclude(ci => ci.MenuItem).ThenInclude(c=>c.Category)
                .FirstOrDefaultAsync(c => c.ComboId == comboId);
        }

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
