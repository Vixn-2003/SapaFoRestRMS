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
    public class InventoryIngredientRepository : IInventoryIngredientRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public InventoryIngredientRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public Task AddAsync(Ingredient entity)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Ingredient>> GetAllAsync()
        {
            return await _context.Ingredients
                .Include(i => i.Unit)
                .Include(i => i.InventoryBatches)
                .Include(i => i.StockTransactions)
                .ToListAsync();
        }


        public async Task<(decimal totalImport, decimal totalExport, decimal totalFirst)> GetTotalImportExportBatches( int BatchesId, DateTime? startDate, DateTime? endDate)
        {
            if (endDate == null)
            {
                endDate = DateTime.Now; 
            }

            if (startDate == null)
            {
                startDate = endDate.Value.AddDays(-7);
            }

            var transactions = await _context.StockTransactions
                .Where(t => t.BatchId == BatchesId
                            && t.TransactionDate >= startDate
                            && t.TransactionDate <= endDate)
                .ToListAsync();

            decimal totalImport = transactions
                .Where(t => t.Type == "Import")
                .Sum(t => t.Quantity);

            decimal totalExport = transactions
                .Where(t => t.Type == "Export")
                .Sum(t => t.Quantity);

            var transactionExist = await _context.StockTransactions
                .Where(t => t.BatchId == BatchesId
                            && t.TransactionDate <= startDate)
                .ToListAsync();

            decimal totalImportE = transactionExist
                .Where(t => t.Type == "Import")
                .Sum(t => t.Quantity);

            decimal totalExportE = transactionExist
                .Where(t => t.Type == "Export")
                .Sum(t => t.Quantity);

            decimal totalFirst = totalImportE - totalExportE;

            return (totalImport, totalExport, totalFirst);
        }


        public Task<Ingredient?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task SaveChangesAsync()
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Ingredient entity)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<InventoryBatch>> getBatchById(int id)
        {
            return await _context.InventoryBatches.Include(x => x.Warehouse)
                .Include(i => i.Ingredient)                
                .Include(i => i.PurchaseOrderDetail)
                    .ThenInclude(p => p.PurchaseOrder)
                        .ThenInclude(o => o.Supplier)
                .Where(i => i.IngredientId == id)
                .ToListAsync();
        }

        public async Task<bool> UpdateBatchWarehouse(int idBatch, int idWarehouse)
        {
            var batch = await _context.InventoryBatches
                .FirstOrDefaultAsync(b => b.BatchId == idBatch);

            if (batch == null)
                return false;


            batch.WarehouseId = idWarehouse;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<Ingredient>> GetAllIngredientSearch(string search)
        {
            return await _context.Ingredients.Where(x => x.IngredientCode.Contains(search) || x.Name.Contains(search)).Include(i => i.Unit)
                .Include(i => i.InventoryBatches)
                    .ThenInclude(b => b.StockTransactions)
                .ToListAsync();
        }

        public async Task<int> AddNewIngredient(Ingredient ingredient)
        {
            try
            {
                // Thêm entity vào DbSet
                await _context.Ingredients.AddAsync(ingredient);

                // Lưu vào DB
                await _context.SaveChangesAsync();

                // Sau khi SaveChanges, EF sẽ tự gán IngredientId
                return ingredient.IngredientId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi khi thêm nguyên liệu mới: {ex.Message}");
                return 0; // Trả về 0 nghĩa là thêm thất bại
            }
        }

        public async Task<int> AddNewBatch(InventoryBatch inventoryBatch)
        {
            try
            {
                await _context.InventoryBatches.AddAsync(inventoryBatch);
                await _context.SaveChangesAsync();

                // EF Core sẽ tự gán BatchId sau khi SaveChanges
                return inventoryBatch.BatchId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi khi thêm lô mới: {ex.Message}");
                return 0; // 0 = thất bại
            }
        }

        public async Task<Ingredient> GetIngredientById(int id)
        {

           var ingredient = await _context.Ingredients.Include(i => i.Unit).FirstOrDefaultAsync(p => p.IngredientId == id);

            if (ingredient != null)
            {
                return ingredient;
            }
            return null;

           
        }

        public async Task<(bool success, string message)> UpdateInforIngredient(int idIngredient, string nameIngredient, int unit)
        {
            try
            {
                var ingredient = await _context.Ingredients
                    .FirstOrDefaultAsync(p => p.IngredientId == idIngredient);

                if (ingredient == null)
                {
                    return (false, "Không tìm thấy nguyên liệu");
                }

                // Kiểm tra có thay đổi không
                if (ingredient.Name == nameIngredient && ingredient.UnitId.Equals(unit))
                {
                    return (false, "Không có thay đổi nào");
                }

                // Kiểm tra tên trùng với nguyên liệu khác
                var duplicateName = await _context.Ingredients
                    .AnyAsync(p => p.Name == nameIngredient && p.IngredientId != idIngredient);

                if (duplicateName)
                {
                    return (false, "Tên nguyên liệu đã tồn tại");
                }

                // Cập nhật thông tin
                ingredient.Name = nameIngredient;
                ingredient.UnitId = unit;
                // ingredient.UpdatedAt = DateTime.Now; // Nếu có field này

                _context.Ingredients.Update(ingredient);
                await _context.SaveChangesAsync();

                return (true, "Cập nhật thành công");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateInforIngredient: {ex.Message}");
                return (false, $"Có lỗi xảy ra: {ex.Message}");
            }
        }
    }
}
