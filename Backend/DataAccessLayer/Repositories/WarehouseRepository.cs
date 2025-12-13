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
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public WarehouseRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }
        public async Task AddAsync(Warehouse entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            // Validate dữ liệu
            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                throw new ArgumentException("Tên kho không được để trống", nameof(entity.Name));
            }

            // Thêm vào DbContext (giả sử bạn đang dùng Entity Framework)
            await _context.Warehouses.AddAsync(entity);

            // Lưu thay đổi vào database
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            // Tìm warehouse theo id
            var warehouse = await _context.Warehouses.FindAsync(id);

            // Kiểm tra tồn tại
            if (warehouse == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy kho với ID: {id}");
            }

            // Kiểm tra đã bị xóa trước đó chưa
            if (!warehouse.IsActive)
            {
                throw new InvalidOperationException($"Kho với ID {id} đã bị xóa trước đó");
            }

            // Xóa mềm - chỉ set IsActive = false
            warehouse.IsActive = false;

            // Cập nhật vào database
            _context.Warehouses.Update(warehouse);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Warehouse>> GetAllAsync()
        {
            return await _context.Warehouses.Where(x => x.IsActive == true).ToListAsync();
        }

        public async Task<Warehouse?> GetByIdAsync(int id)
        {
            return await _context.Warehouses
                .FirstOrDefaultAsync(x => x.IsActive && x.WarehouseId == id);
        }

        public async Task<int> GetIdByStringAsync(string warehouse)
        {
            if (string.IsNullOrWhiteSpace(warehouse))
                return 0;

            var unit = await _context.Warehouses
                .FirstOrDefaultAsync(u => u.Name == warehouse);

            return unit?.WarehouseId ?? 0;
        }

        public Task SaveChangesAsync()
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Warehouse entity)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<InventoryBatch>> GetBatchesByWarehouseIdAsync(int warehouseId)
        {
            return await _context.InventoryBatches
                .Include(b => b.Ingredient)
                    .ThenInclude(i => i.Unit)
                .Include(b => b.Warehouse)
                .Include(b => b.PurchaseOrderDetail)
                    .ThenInclude(pod => pod.PurchaseOrder)
                .Where(b => b.WarehouseId == warehouseId && b.IsActive)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }
    }
}
