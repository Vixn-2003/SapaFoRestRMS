using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.DTOs.Manager;
using DomainAccessLayer.Models;
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
        Task<InventoryIngredientDTO> GetIngredientById(int id);
        Task<IEnumerable<InventoryIngredientDTO>> GetAllIngredientSearch( string search);
        Task<(decimal TImport, decimal TExport, decimal totalFirst)> GetImportExportBatchesId(int id, DateTime? StartDate, DateTime? EndDate);
        Task<IEnumerable<BatchIngredientDTO>> GetBatchesAsync(int id);
        Task<bool> UpdateBatchWarehouse(int idBatch, int idWarehouse);
        Task<int> AddNewIngredient(IngredientDTO ingredient);
        Task<int> AddNewBatch(InventoryBatchDTO batchIngredientDTO);
        Task<(bool success, string message)> UpdateIngredient(int idIngredient, string nameIngredient, int unit);
        
        // Batch reservation methods
        Task<(bool success, string message)> ReserveBatchesForOrderDetailAsync(int orderDetailId);
        Task<(bool success, string message)> ConsumeReservedBatchesForOrderDetailAsync(int orderDetailId);
        Task<(bool success, string message)> ReleaseReservedBatchesForOrderDetailAsync(int orderDetailId);
    }
}
