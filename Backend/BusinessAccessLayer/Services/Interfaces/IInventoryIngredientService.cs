using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.DTOs.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IInventoryIngredientService
    {
        Task<IEnumerable<InventoryIngredientDTO>> GetAllIngredient();
        Task<(decimal TImport, decimal TExport, decimal totalFirst)> GetImportExportBatchesId(int id, DateTime? StartDate, DateTime? EndDate);
        Task<IEnumerable<BatchIngredientDTO>> GetBatchesAsync(int id);
        Task<bool> UpdateBatchWarehouse(int idBatch, int idWarehouse);

    }
}
