using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IInventoryIngredientRepository : IRepository<Ingredient>
    {
        Task<(decimal totalImport, decimal totalExport, decimal totalFirst)> GetTotalImportExportBatches(int ingredientId, DateTime? startDate, DateTime? endDate);

        Task<IEnumerable<InventoryBatch>> getBatchById(int id);
        Task<InventoryBatch> getBatchByBatchId(int id);
        Task<bool> UpdateBatchByBatch(InventoryBatch inventoryBatch);

        Task<bool> UpdateBatchWarehouse(int idBatch, int idWarehouse, bool isActive);
        Task<int> AddNewIngredient(Ingredient ingredient);

        Task<IEnumerable<Ingredient>> GetAllIngredientSearch(string search);
        Task<int> AddNewBatch(InventoryBatch inventoryBatch);
        Task<Ingredient> GetIngredientById(int id);
        Task<(bool success, string message)> UpdateInforIngredient(int idIngredient, string nameIngredient, int unit);
        Task<InventoryBatch?> GetBatchByIdAsync(int batchId);
        Task<bool> UpdateBatchAsync(InventoryBatch batch);
        Task<List<InventoryBatch>> GetAvailableBatchesByIngredientAsync(int ingredientId);
        Task<List<InventoryBatch>> GetAllBatchesByIngredientAsync(int ingredientId); // Lấy tất cả batches (kể cả available <= 0)
        Task<List<InventoryBatch>> GetReservedBatchesByIngredientAsync(int ingredientId);
    }
}
