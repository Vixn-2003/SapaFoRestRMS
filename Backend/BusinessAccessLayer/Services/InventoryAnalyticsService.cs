using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using DomainAccessLayer.Models;
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

            // 1. Lấy giao dịch xuất
            var exports = await _context.StockTransactions
                .AsNoTracking()
                .Where(t => t.Type == "Export"
                            && t.TransactionDate != null
                            && t.TransactionDate.Value.Date >= fromDate
                            && t.TransactionDate.Value.Date <= today)
                .Select(t => new
                {
                    t.IngredientId,
                    t.Quantity,
                    Date = t.TransactionDate!.Value.Date
                })
                .ToListAsync(cancellationToken);

            if (!exports.Any())
                return new List<IngredientUsageForecastDto>();

            // 2. Group theo ingredient & theo ngày
            var grouped = exports
                .GroupBy(x => x.IngredientId)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(t => t.Date)
                          .Select(d => new { Date = d.Key, Total = d.Sum(x => x.Quantity) })
                          .OrderBy(x => x.Date)
                          .ToList()
                );

            // 3. Lấy thông tin ingredient
            var ingredientIds = grouped.Keys.ToList();

            var ingredients = await _context.Ingredients
                .Where(i => ingredientIds.Contains(i.IngredientId))
                .ToDictionaryAsync(i => i.IngredientId, i => i, cancellationToken);

            // 4. Lấy tồn kho (sử dụng QuantityRemaining từ Batch)
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

            foreach (var kvp in grouped)
            {
                var ingId = kvp.Key;
                var dailyList = kvp.Value;

                var total = dailyList.Sum(x => x.Total);

                var minDate = dailyList.First().Date;
                var maxDate = dailyList.Last().Date;

                var dateRange = (maxDate - minDate).TotalDays + 1;
                if (dateRange <= 0) dateRange = 1;

                var adu = (decimal)total / (decimal)dateRange;

                // --- Tính CV (Coefficient of Variation) ---
                decimal? cv = null;
                var arr = dailyList.Select(x => x.Total).ToList();
                if (arr.Count > 1)
                {
                    var mean = (decimal)arr.Average();
                    if (mean > 0)
                    {
                        var variance = arr.Sum(v => (decimal)Math.Pow((double)(v - mean), 2)) / (arr.Count - 1);
                        var std = (decimal)Math.Sqrt((double)variance);
                        cv = std / mean;
                    }
                }

                // --- SafetyDays dùng theo CV ---
                decimal safetyDays;
                if (cv == null)
                {
                    safetyDays = 2;
                }
                else
                {
                    safetyDays = 2 + (decimal)cv * 5;
                    safetyDays = Math.Clamp(safetyDays, 2, 10);
                }

                var reorder = adu * safetyDays;

                stocks.TryGetValue(ingId, out var stock);

                decimal? daysRemain = adu > 0 ? (stock / adu) : null;

                ingredients.TryGetValue(ingId, out var ing);

                result.Add(new IngredientUsageForecastDto
                {
                    IngredientId = ingId,
                    IngredientName = ing?.Name ?? "N/A",
                    UnitName = ing?.Unit?.UnitName ?? "",

                    AverageDailyUsage = Math.Round(adu, 2),
                    SafetyDays = Math.Round(safetyDays, 2),
                    SafetyStockQuantity = Math.Round(reorder, 2),
                    ReorderLevel = Math.Round(reorder, 2),


                    CurrentStock = stock,

                    DaysRemaining = daysRemain.HasValue
        ? Math.Round(daysRemain.Value, 1)
        : null,

                    DaysWindowUsed = (int)dateRange,

                    DistinctUsedDays = dailyList?.Count ?? 0,

                    CoefficientOfVariation = cv.HasValue? Math.Round(cv.Value, 3)
        : null
                });

            }

            return result.OrderByDescending(x => x.AverageDailyUsage).ToList();
        }

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
                ing.ReorderLevel = f.ReorderLevel;
            }

            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
