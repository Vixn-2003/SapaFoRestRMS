using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSapaForestForStaff.DTOs.Inventory;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class InventoryIngredientDTO
    {
        public int IngredientId { get; set; }
        public string IngredientCode { get; set; } = string.Empty!;
        public string Name { get; set; } = null!;
        public int? UnitId { get; set; }
        public decimal? ReorderLevel { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SelectedUnit { get; set; }
        public string? SearchIngredent { get; set; }
        public List<InventoryBatchDTO> Batches { get; set; } = new();
        public UnitDTO Unit { get; set; } = new();

        public decimal TotalQuantity => Batches.Sum(b => b.QuantityRemaining);
        public decimal TotalExport { get; set; }
        public decimal TotalImport { get; set; }
        public decimal OriginalQuantity { get; set; }

        public bool HasExpiringSoon
        {
            get
            {
                var today = DateTime.Now.Date;
                return Batches.Any(b =>
                {
                    if (!b.ExpiryDate.HasValue || !b.CreatedAt.HasValue)
                        return false;
                    var createdDate = b.CreatedAt.Value.Date;
                    var expiryDate = b.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue);
                    var totalDays = (expiryDate - createdDate).TotalDays;
                    var twoThirdsPoint = createdDate.AddDays(totalDays * 2 / 3);
                    return today >= twoThirdsPoint && today < expiryDate;
                });
            }
        }

        public bool IsExpired
        {
            get
            {
                var today = DateTime.Now.Date;
                return Batches.Any(b => b.ExpiryDate.HasValue &&
                                       b.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) < today);
            }
        }

        public bool IsBelowReorderLevel => ReorderLevel.HasValue && TotalQuantity < ReorderLevel.Value;
        public bool IsLowStock => ReorderLevel.HasValue && TotalQuantity < (ReorderLevel.Value / 2);

        // ✅ THÊM MỚI: Đếm số lượng lô hết hạn
        public int ExpiredBatchCount
        {
            get
            {
                var today = DateTime.Now.Date;
                return Batches.Count(b => b.ExpiryDate.HasValue &&
                                         b.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) < today);
            }
        }

        // ✅ THÊM MỚI: Đếm số lượng lô sắp hết hạn
        public int ExpiringSoonBatchCount
        {
            get
            {
                var today = DateTime.Now.Date;
                return Batches.Count(b =>
                {
                    if (!b.ExpiryDate.HasValue || !b.CreatedAt.HasValue)
                        return false;

                    var createdDate = b.CreatedAt.Value.Date;
                    var expiryDate = b.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue);

                    // Chỉ tính nếu chưa hết hạn
                    if (today >= expiryDate)
                        return false;

                    var totalDays = (expiryDate - createdDate).TotalDays;
                    var twoThirdsPoint = createdDate.AddDays(totalDays * 2 / 3);

                    return today >= twoThirdsPoint;
                });
            }
        }

        public string Status
        {
            get
            {
                if (IsExpired) return "expired";
                if (IsLowStock) return "low-stock";
                if (IsBelowReorderLevel) return "below-reorder";
                if (HasExpiringSoon) return "expiring-soon";
                return "normal";
            }
        }

        public string StatusText
        {
            get
            {
                if (IsExpired) return "Hết hạn";
                if (IsLowStock) return "Sắp hết hàng";
                if (IsBelowReorderLevel) return "Dưới tối thiểu";
                if (HasExpiringSoon) return "Sắp hết hạn";
                return "Bình thường";
            }
        }
    }
}
