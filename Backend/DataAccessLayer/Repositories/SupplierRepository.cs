using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public SupplierRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<List<Supplier>> GetAllSuppliersAsync()
        {
            // Chỉ lấy Supplier, không cần Include gì cả
            return await _context.Suppliers.Where(x => x.IsActive == true).ToListAsync();
        }

        public async Task<List<PurchaseOrder>> GetSupplierPurchaseOrdersAsync(int supplierId)
        {
            // Lấy PurchaseOrders và các chi tiết cần thiết để Service tính toán
            return await _context.PurchaseOrders
                .Where(po => po.SupplierId == supplierId)
                .Include(po => po.PurchaseOrderDetails) // Cần chi tiết để tính Total/Items
                .OrderByDescending(po => po.OrderDate)
                .ToListAsync();
        }

        public Task<IQueryable<PurchaseOrderDetail>> GetSupplierOrderDetailsQuery(int supplierId)
        {
            // Trả về IQueryable để Service GroupBy
            var query = _context.PurchaseOrderDetails
                // Thêm Where để lọc theo SupplierId
                .Where(pod => pod.PurchaseOrder.SupplierId == supplierId)
                // Cần Include để có thể truy cập OrderDate trong Service
                .Include(pod => pod.PurchaseOrder)
                // Bỏ .AsQueryable() vì query đã là IQueryable, nhưng cần ép kiểu để phù hợp
                .AsQueryable();
            return Task.FromResult<IQueryable<PurchaseOrderDetail>>(query);
        }

        public async Task<bool> DeleteSoftAsync(int supplierId)
        {
            var supplier = await _context.Suppliers.FindAsync(supplierId);
            supplier.IsActive = false; 
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
