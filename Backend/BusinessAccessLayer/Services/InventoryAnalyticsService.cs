using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using Microsoft.EntityFrameworkCore;

namespace BusinessAccessLayer.Services.Inventory
{
    public class InventoryAnalyticsService : IInventoryAnalyticsService
    {
        private readonly SapaFoRestRmsContext _context;

        public InventoryAnalyticsService(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<List<IngredientUsageForecastDto>> GetIngredientUsageForecastAsync(
            int daysWindow = 30,
            CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;
            var fromDate = today.AddDays(-daysWindow);

            // 1. Lấy giao dịch xuất trong 30 ngày qua
            var exports = await _context.StockTransactions
                .AsNoTracking()
                .Where(t => t.Type == "Export"
                            && t.TransactionDate != null
                            && t.TransactionDate.Value.Date >= fromDate
                            && t.TransactionDate.Value.Date <= today)
                .Select(t => new
                {
                    t.IngredientId,
                    t.Quantity
                })
                .ToListAsync(cancellationToken);

            if (!exports.Any())
                return new List<IngredientUsageForecastDto>();

            // 2. Tính tổng xuất theo từng ingredient
            var grouped = exports
                .GroupBy(x => x.IngredientId)
                .Select(g => new
                {
                    IngredientId = g.Key,
                    TotalExport = g.Sum(x => x.Quantity)
                })
                .ToList();

            // 3. Lấy thông tin ingredient
            var ingredientIds = grouped.Select(g => g.IngredientId).ToList();

            var ingredients = await _context.Ingredients
                .Where(i => ingredientIds.Contains(i.IngredientId))
                .ToDictionaryAsync(i => i.IngredientId, i => i, cancellationToken);

            // 4. Lấy tồn kho hiện tại
            var stocks = await _context.InventoryBatches
                .AsNoTracking()
                .Where(b => ingredientIds.Contains(b.IngredientId))
                .GroupBy(b => b.IngredientId)
                .Select(b => new
                {
                    IngredientId = b.Key,
                    Stock = b.Sum(x => x.QuantityRemaining)
                })
                .ToDictionaryAsync(b => b.IngredientId, b => b.Stock, cancellationToken);

            var result = new List<IngredientUsageForecastDto>();

            foreach (var item in grouped)
            {
                var totalExport = item.TotalExport;

                // 5. TÍNH TRUNG BÌNH MỖI NGÀY
                var averageDailyUsage = totalExport / daysWindow;

                // 6. Lấy tồn kho
                stocks.TryGetValue(item.IngredientId, out var currentStock);

                // 7. Tính số ngày còn đủ dùng
                decimal? daysRemaining = averageDailyUsage > 0
                    ? (currentStock / averageDailyUsage)
                    : null;

                // 8. Lấy thông tin ingredient
                ingredients.TryGetValue(item.IngredientId, out var ing);

                result.Add(new IngredientUsageForecastDto
                {
                    IngredientId = item.IngredientId,
                    IngredientName = ing?.Name ?? "N/A",
                    UnitName = ing?.Unit?.UnitName ?? "",

                    //  CHỈ CẦN CÁI NÀY
                    AverageDailyUsage = Math.Round(averageDailyUsage, 2),

                    CurrentStock = currentStock,
                    DaysRemaining = daysRemaining.HasValue
                        ? Math.Round(daysRemaining.Value, 1)
                        : null,

                    DaysWindowUsed = daysWindow
                });
            }

            return result.OrderByDescending(x => x.AverageDailyUsage).ToList();
        }

        //  CẬP NHẬT: Chỉ lưu AverageDailyUsage vào ReorderLevel (tạm thời)
        public async Task<int> RecalculateReorderLevelsAsync(
            int daysWindow = 30,
            CancellationToken cancellationToken = default)
        {
            var forecast = await GetIngredientUsageForecastAsync(daysWindow, cancellationToken);
            if (!forecast.Any()) return 0;

            var ids = forecast.Select(x => x.IngredientId).ToList();

            var ings = await _context.Ingredients
                .Where(i => ids.Contains(i.IngredientId))
                .ToListAsync(cancellationToken);

            foreach (var ing in ings)
            {
                var f = forecast.First(x => x.IngredientId == ing.IngredientId);

                //  LƯU TRUNG BÌNH TIÊU THỤ MỖI NGÀY
                ing.ReorderLevel = f.AverageDailyUsage;
            }

            return await _context.SaveChangesAsync(cancellationToken);
        }

    }
}