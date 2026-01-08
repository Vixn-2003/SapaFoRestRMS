using AutoMapper;
using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class PurchaseOrderDetailService : IPurchaseOrderDetailService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PurchaseOrderDetailService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PurchaseOrderDetailWithSupplierDTO>> GetByIngredientIdAsync(int ingredientId)
        {
            var details = await _unitOfWork.PurchaseOrderDetail.GetByIngredientIdWithDetailsAsync(ingredientId);
            return _mapper.Map<IEnumerable<PurchaseOrderDetailWithSupplierDTO>>(details);
        }

        public async Task<IEnumerable<SupplierComparisonDTO>> GetSupplierComparisonAsync(int ingredientId, string compareBy = "price")
        {
            var details = await _unitOfWork.PurchaseOrderDetail.GetByIngredientIdWithDetailsAsync(ingredientId);

            if (!details.Any())
            {
                return Enumerable.Empty<SupplierComparisonDTO>();
            }

            // Group theo SupplierId và tính toán
            var comparison = details
                .GroupBy(d => new
                {
                    SupplierId = d.PurchaseOrder.SupplierId,
                    SupplierName = d.PurchaseOrder.Supplier.Name,
                    SupplierCode = d.PurchaseOrder.Supplier.CodeSupplier,
                    Unit = d.Unit
                })
                .Select(g => new SupplierComparisonDTO
                {
                    SupplierId = g.Key.SupplierId,
                    SupplierName = g.Key.SupplierName,
                    SupplierCode = g.Key.SupplierCode,
                    Unit = g.Key.Unit ?? "",
                    RecentPrice = g.OrderByDescending(d => d.PurchaseOrder.OrderDate)
                                   .First().UnitPrice,
                    TotalQuantity = g.Sum(d => d.Quantity),
                    TransactionCount = g.Count(),
                    LastOrderDate = g.Max(d => d.PurchaseOrder.OrderDate)
                })
                .ToList();

            // Sắp xếp theo tiêu chí
            if (compareBy.ToLower() == "quantity")
            {
                comparison = comparison.OrderByDescending(c => c.TotalQuantity).ToList();
            }
            else // price
            {
                comparison = comparison.OrderBy(c => c.RecentPrice).ToList();
            }

            return comparison;
        }
    }
}
