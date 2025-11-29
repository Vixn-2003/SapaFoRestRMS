using BusinessAccessLayer.DTOs.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IInventoryAnalyticsService
    {
        /// <summary>
        /// Tính ADU, SafetyStock, ReorderLevel & Forecast cho từng nguyên liệu.
        /// </summary>
        Task<List<IngredientUsageForecastDto>> GetIngredientUsageForecastAsync(
            int daysWindow = 30,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tính lại ReorderLevel và update trực tiếp vào bảng Ingredients.
        /// Return: số nguyên liệu được update.
        /// </summary>
        Task<int> RecalculateReorderLevelsAsync(
            int daysWindow = 30,
            CancellationToken cancellationToken = default);
    }
}