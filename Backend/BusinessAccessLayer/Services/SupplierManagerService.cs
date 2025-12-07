// File: BusinessAccessLayer.Services/SupplierManagerService.cs (Đã Chỉnh Sửa)

using AutoMapper;
using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DataAccessLayer.UnitOfWork.Interfaces; // Cần thiết cho các hàm LINQ

namespace BusinessAccessLayer.Services
{
    public class SupplierManagerService : ISupplierManagerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SupplierManagerService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // --- 1. Lấy danh sách tổng hợp (List View) ---
        public async Task<List<SupplierListDto>> GetSuppliersSummaryAsync()
        {
            var suppliers = await _unitOfWork.SupplierManager.GetAllSuppliersAsync();

            // BƯỚC 1: Mapping cơ bản (Supplier Model -> SupplierListDto)
            var dtoList = _mapper.Map<List<SupplierListDto>>(suppliers);

            // BƯỚC 2: Tính toán các chỉ số phức tạp
            foreach (var dto in dtoList)
            {
                // Lấy các đơn hàng của nhà cung cấp
                var orders = await _unitOfWork.SupplierManager.GetSupplierPurchaseOrdersAsync(dto.Id);

                // TÍNH TOÁN
                var totalValue = orders.SelectMany(o => o.PurchaseOrderDetails).Sum(pod => (decimal?)pod.Subtotal);
                var lastOrderDate = orders.Max(o => o.OrderDate);

                // **ĐÃ XÓA LOGIC TÍNH TOÁN ONTIMERATE**

                // GÁN DỮ LIỆU TỔNG HỢP VÀO DTO
                dto.TotalOrders = orders.Count;
                dto.TotalValue = totalValue ?? 0m;
                dto.LastOrder = lastOrderDate;
            }

            return dtoList;
        }

        // --- 2. Lịch sử Đơn hàng (Detail Tab: Orders) ---
        public async Task<List<OrderHistoryDto>> GetHistoryAsync(int supplierId)
        {
            var orders = await _unitOfWork.SupplierManager.GetSupplierPurchaseOrdersAsync(supplierId);

            // BƯỚC 1: Mapping cơ bản (PurchaseOrder -> OrderHistoryDto)
            var dtoList = _mapper.Map<List<OrderHistoryDto>>(orders);

            // BƯỚC 2: Tính toán các trường Total và Items
            for (int i = 0; i < orders.Count; i++)
            {
                // Sử dụng dữ liệu đã được Include sẵn từ Repository
                dtoList[i].Total = orders[i].PurchaseOrderDetails.Sum(d => d.Subtotal);
                dtoList[i].Items = orders[i].PurchaseOrderDetails.Count;
            }

            return dtoList;
        }

        // --- 3. Danh mục Nguyên liệu (Detail Tab: Products) ---
        public async Task<List<SupplierIngredientDto>> GetProductsAsync(int supplierId)
        {
            // Lấy IQueryable để thực hiện Projection (GroupBy) trên DB
            var orderDetailsQuery = await _unitOfWork.SupplierManager.GetSupplierOrderDetailsQuery(supplierId);

            // THỰC HIỆN TÍNH TOÁN VÀ PROJECTION TRÊN DATABASE
            var result = await orderDetailsQuery
                .GroupBy(pod => new { pod.IngredientId, pod.IngredientName, pod.Unit })
                .Where(g => g.Key.IngredientId.HasValue)
                .Select(g => new SupplierIngredientDto
                {
                    IngredientId = g.Key.IngredientId.Value,
                    Name = g.Key.IngredientName!,
                    Unit = g.Key.Unit!,
                    Frequency = g.Count(),

                    LastPrice = g.OrderByDescending(x => x.PurchaseOrder.OrderDate)
                                 .Select(x => x.UnitPrice)
                                 .First(),
                    AvgPrice = g.Average(x => x.UnitPrice)
                })
                .ToListAsync();

            return result;
        }

        // --- 4. Lấy Top Suppliers (Dashboard) ---
        // Thêm triển khai cho hàm này
        public Task<List<TopSupplierDto>> GetTopSuppliersAsync(DateTime startDate, DateTime endDate)
        {

            throw new NotImplementedException("Hàm GetTopSuppliersAsync cần được triển khai trong Repository để tối ưu hóa truy vấn.");
        }

        public async Task<bool> SoftDeleteSupplierAsync(int supplierId)
        {
            var supplier = await _unitOfWork.SupplierManager.DeleteSoftAsync(supplierId);
            return supplier;
        }
    }
}