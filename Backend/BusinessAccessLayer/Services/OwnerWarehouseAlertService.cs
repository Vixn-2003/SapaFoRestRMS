using BusinessAccessLayer.DTOs.Owner;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    /// <summary>
    /// Service xử lý business logic cho Owner Warehouse Alert Management
    /// </summary>
    public class OwnerWarehouseAlertService : IOwnerWarehouseAlertService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OwnerWarehouseAlertService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<WarehouseAlertResponseDto> GetWarehouseAlertsAsync(CancellationToken ct = default)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var ingredients = await _unitOfWork.InventoryIngredient.GetAllAsync();

            var lowStockItems = GetLowStockItems(ingredients);
            var nearExpiryItems = GetNearExpiryItems(ingredients, today);
            var expiredItems = GetExpiredItems(ingredients, today);

            var response = new WarehouseAlertResponseDto
            {
                Summary = new AlertSummaryCardsDto
                {
                    LowStockCount = lowStockItems.Count,
                    NearExpiryCount = nearExpiryItems.Count,
                    ExpiredCount = expiredItems.Count,
                    TotalAlertsCount = lowStockItems.Count + nearExpiryItems.Count + expiredItems.Count
                },
                LowStockItems = lowStockItems,
                NearExpiryItems = nearExpiryItems,
                ExpiredItems = expiredItems,
                CategoryDistribution = new AlertCategoryDistributionDto
                {
                    LowStockCount = lowStockItems.Count,
                    NearExpiryCount = nearExpiryItems.Count,
                    ExpiredCount = expiredItems.Count
                },
                StockLevelChart = GetStockLevelChartData(lowStockItems),
                ExpiryTimeline = GetExpiryTimelineData(nearExpiryItems)
            };

            return response;
        }

        private List<LowStockItemDto> GetLowStockItems(IEnumerable<DomainAccessLayer.Models.Ingredient> ingredients)
        {
            var lowStockItems = new List<LowStockItemDto>();

            foreach (var ingredient in ingredients)
            {
                if (!ingredient.ReorderLevel.HasValue)
                    continue;

                var currentQuantity = ingredient.InventoryBatches
                    .Where(b => b.IsActive)
                    .Sum(b => b.Available);

                if (currentQuantity < ingredient.ReorderLevel.Value)
                {
                    var shortage = ingredient.ReorderLevel.Value - currentQuantity;
                    var severityPercent = currentQuantity / ingredient.ReorderLevel.Value;

                    var severity = severityPercent <= 0.25m ? "Critical" 
                                 : severityPercent <= 0.5m ? "Low" 
                                 : "Warning";

                    // Get warehouse name (lấy từ batch đầu tiên)
                    var warehouseName = ingredient.InventoryBatches
                        .Where(b => b.IsActive)
                        .Select(b => b.Warehouse?.Name)
                        .FirstOrDefault() ?? "Unknown";

                    lowStockItems.Add(new LowStockItemDto
                    {
                        IngredientId = ingredient.IngredientId,
                        IngredientName = ingredient.Name,
                        IngredientCode = ingredient.IngredientCode,
                        CurrentQuantity = currentQuantity,
                        ReorderLevel = ingredient.ReorderLevel.Value,
                        Unit = ingredient.Unit?.UnitName ?? "N/A",
                        WarehouseName = warehouseName,
                        ShortageAmount = shortage,
                        Severity = severity
                    });
                }
            }

            return lowStockItems.OrderBy(i => i.Severity == "Critical" ? 0 : i.Severity == "Low" ? 1 : 2)
                                .ThenBy(i => i.CurrentQuantity)
                                .ToList();
        }

        private List<NearExpiryItemDto> GetNearExpiryItems(IEnumerable<DomainAccessLayer.Models.Ingredient> ingredients, DateOnly today)
        {
            var nearExpiryItems = new List<NearExpiryItemDto>();

            foreach (var ingredient in ingredients)
            {
                foreach (var batch in ingredient.InventoryBatches.Where(b => b.IsActive && b.ExpiryDate.HasValue))
                {
                    var daysUntilExpiry = batch.ExpiryDate.Value.DayNumber - today.DayNumber;

                    // Near expiry: 1-7 days
                    if (daysUntilExpiry > 0 && daysUntilExpiry <= 7)
                    {
                        var severity = daysUntilExpiry <= 3 ? "Critical" : "Warning";

                        nearExpiryItems.Add(new NearExpiryItemDto
                        {
                            BatchId = batch.BatchId,
                            IngredientName = ingredient.Name,
                            IngredientCode = ingredient.IngredientCode,
                            Quantity = batch.QuantityRemaining,
                            Unit = ingredient.Unit?.UnitName ?? "N/A",
                            ExpiryDate = batch.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue),
                            DaysUntilExpiry = daysUntilExpiry,
                            WarehouseName = batch.Warehouse?.Name ?? "Unknown",
                            Severity = severity
                        });
                    }
                }
            }

            return nearExpiryItems.OrderBy(i => i.DaysUntilExpiry).ToList();
        }

        private List<ExpiredItemDto> GetExpiredItems(IEnumerable<DomainAccessLayer.Models.Ingredient> ingredients, DateOnly today)
        {
            var expiredItems = new List<ExpiredItemDto>();

            foreach (var ingredient in ingredients)
            {
                foreach (var batch in ingredient.InventoryBatches.Where(b => b.IsActive && b.ExpiryDate.HasValue))
                {
                    var daysExpired = today.DayNumber - batch.ExpiryDate.Value.DayNumber;

                    if (daysExpired >= 0)
                    {
                        expiredItems.Add(new ExpiredItemDto
                        {
                            BatchId = batch.BatchId,
                            IngredientName = ingredient.Name,
                            IngredientCode = ingredient.IngredientCode,
                            Quantity = batch.QuantityRemaining,
                            Unit = ingredient.Unit?.UnitName ?? "N/A",
                            ExpiryDate = batch.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue),
                            DaysExpired = daysExpired,
                            WarehouseName = batch.Warehouse?.Name ?? "Unknown"
                        });
                    }
                }
            }

            return expiredItems.OrderByDescending(i => i.DaysExpired).ToList();
        }

        private List<StockLevelChartDto> GetStockLevelChartData(List<LowStockItemDto> lowStockItems)
        {
            // Lấy top 10 items có mức tồn kho thấp nhất
            return lowStockItems
                .Take(10)
                .Select(i => new StockLevelChartDto
                {
                    IngredientName = i.IngredientName,
                    CurrentQuantity = i.CurrentQuantity,
                    ReorderLevel = i.ReorderLevel
                })
                .ToList();
        }

        private List<ExpiryTimelineDto> GetExpiryTimelineData(List<NearExpiryItemDto> nearExpiryItems)
        {
            // Lấy top 15 items sắp hết hạn
            return nearExpiryItems
                .Take(15)
                .Select(i => new ExpiryTimelineDto
                {
                    IngredientName = i.IngredientName,
                    DaysUntilExpiry = i.DaysUntilExpiry,
                    Quantity = i.Quantity,
                    ExpiryDate = i.ExpiryDate
                })
                .ToList();
        }
    }
}

