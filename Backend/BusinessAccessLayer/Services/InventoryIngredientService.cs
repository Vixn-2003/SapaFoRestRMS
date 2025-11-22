using AutoMapper;
using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.Services.Interfaces;
using CloudinaryDotNet;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class InventoryIngredientService : IInventoryIngredientService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public InventoryIngredientService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<InventoryIngredientDTO>> GetAllIngredient()
        {
            var ingredients = await _unitOfWork.InventoryIngredient.GetAllAsync();
                return _mapper.Map<IEnumerable<InventoryIngredientDTO>>(ingredients);
        }

        public Task<(decimal TImport, decimal TExport, decimal totalFirst)> GetImportExportBatchesId(int id, DateTime? StartDate , DateTime? EndDate)
        {
            var totalExIm = _unitOfWork.InventoryIngredient.GetTotalImportExportBatches(id, StartDate, EndDate);
            return totalExIm;
        }

        public async Task<IEnumerable<BatchIngredientDTO>> GetBatchesAsync(int ingredientId)
        {
            var batches = await _unitOfWork.InventoryIngredient.getBatchById(ingredientId);
            return _mapper.Map<IEnumerable<BatchIngredientDTO>>(batches);
        }

        public async Task<bool> UpdateBatchWarehouse(int idBatch, int idWarehouse)
        {
            var result = await _unitOfWork.InventoryIngredient.UpdateBatchWarehouse(idBatch,idWarehouse);
            return result;
        }

        public async Task<IEnumerable<InventoryIngredientDTO>> GetAllIngredientSearch(string search)
        {
            var ingredients = await _unitOfWork.InventoryIngredient.GetAllIngredientSearch(search);
            return _mapper.Map<IEnumerable<InventoryIngredientDTO>>(ingredients);
        }

        public async Task<int> AddNewIngredient(IngredientDTO ingredient)
        {
            var ingre = _mapper.Map<Ingredient>(ingredient);
            var result = await _unitOfWork.InventoryIngredient.AddNewIngredient(ingre);
            return result;
        }

        public async Task<int> AddNewBatch(InventoryBatchDTO batchIngredientDTO)
        {
            var batch = _mapper.Map<InventoryBatch>(batchIngredientDTO);
            var result = await _unitOfWork.InventoryIngredient.AddNewBatch(batch);
            return result;
        }

        public async Task<InventoryIngredientDTO> GetIngredientById(int id)
        {
            var ingredients = await _unitOfWork.InventoryIngredient.GetIngredientById(id);
            return _mapper.Map<InventoryIngredientDTO>(ingredients);
        }

        public async Task<(bool success, string message)> UpdateIngredient(int idIngredient, string nameIngredient, int unit)
        {
            var ingredients = await _unitOfWork.InventoryIngredient.UpdateInforIngredient(idIngredient, nameIngredient, unit);
            return ingredients;
        }

        /// <summary>
        /// Reserve batches for an order detail when Bếp phó duyệt (status changes to Cooking)
        /// </summary>
        public async Task<(bool success, string message)> ReserveBatchesForOrderDetailAsync(int orderDetailId)
        {
            try
            {
                // Get order detail with menu item and recipes
                var orderDetail = await _unitOfWork.OrderDetails.GetByIdWithMenuItemAsync(orderDetailId);
                if (orderDetail == null || orderDetail.MenuItem == null)
                {
                    return (false, "Không tìm thấy món ăn");
                }

                // Get recipes for this menu item
                var recipes = await _unitOfWork.MenuItem.GetRecipeByMenuItem(orderDetail.MenuItem.MenuItemId);
                if (!recipes.Any())
                {
                    // No ingredients needed, return success
                    return (true, "Món này không cần nguyên liệu");
                }

                var orderQuantity = orderDetail.Quantity;

                // For each ingredient in the recipe, reserve batches
                foreach (var recipe in recipes)
                {
                    var totalNeeded = recipe.QuantityNeeded * orderQuantity;
                    
                    // Get available batches for this ingredient (FEFO - First Expiry First Out)
                    var availableBatches = await _unitOfWork.InventoryIngredient.GetAvailableBatchesByIngredientAsync(recipe.IngredientId);
                    
                    decimal remainingToReserve = totalNeeded;
                    
                    foreach (var batch in availableBatches)
                    {
                        if (remainingToReserve <= 0) break;
                        
                        var available = batch.QuantityRemaining - batch.QuantityReserved;
                        if (available <= 0) continue;
                        
                        var toReserve = Math.Min(available, remainingToReserve);
                        batch.QuantityReserved += toReserve;
                        remainingToReserve -= toReserve;
                        
                        await _unitOfWork.InventoryIngredient.UpdateBatchAsync(batch);
                    }
                    
                    if (remainingToReserve > 0)
                    {
                        return (false, $"Không đủ nguyên liệu: {recipe.Ingredient?.Name ?? "N/A"}. Thiếu: {remainingToReserve}");
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                return (true, "Đã dành riêng nguyên liệu thành công");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi dành riêng nguyên liệu: {ex.Message}");
            }
        }

        /// <summary>
        /// Consume reserved batches when Nấu xong (status changes to Done)
        /// </summary>
        public async Task<(bool success, string message)> ConsumeReservedBatchesForOrderDetailAsync(int orderDetailId)
        {
            try
            {
                // Get order detail with menu item and recipes
                var orderDetail = await _unitOfWork.OrderDetails.GetByIdWithMenuItemAsync(orderDetailId);
                if (orderDetail == null || orderDetail.MenuItem == null)
                {
                    return (false, "Không tìm thấy món ăn");
                }

                // Get recipes for this menu item
                var recipes = await _unitOfWork.MenuItem.GetRecipeByMenuItem(orderDetail.MenuItem.MenuItemId);
                if (!recipes.Any())
                {
                    return (true, "Món này không cần nguyên liệu");
                }

                var orderQuantity = orderDetail.Quantity;

                // For each ingredient in the recipe, consume from reserved batches
                foreach (var recipe in recipes)
                {
                    var totalNeeded = recipe.QuantityNeeded * orderQuantity;
                    
                    // Get batches with reserved quantity for this ingredient
                    // Filter to only get batches with QuantityReserved > 0
                    var batches = await _unitOfWork.InventoryIngredient.getBatchById(recipe.IngredientId);
                    var batchesList = batches
                        .Where(b => b.QuantityReserved > 0)
                        .OrderBy(b => b.ExpiryDate ?? DateOnly.MaxValue)
                        .ThenBy(b => b.CreatedAt)
                        .ToList();
                    
                    if (!batchesList.Any())
                    {
                        return (false, $"Không tìm thấy nguyên liệu đã được dành riêng cho {recipe.Ingredient?.Name ?? "N/A"}. Vui lòng đảm bảo món đã được bếp phó duyệt (status = Cooking) trước khi hoàn thành.");
                    }
                    
                    decimal remainingToConsume = totalNeeded;
                    
                    foreach (var batch in batchesList)
                    {
                        if (remainingToConsume <= 0) break;
                        
                        var toConsume = Math.Min(batch.QuantityReserved, remainingToConsume);
                        
                        // Consume from reserved and remaining
                        batch.QuantityReserved -= toConsume;
                        batch.QuantityRemaining -= toConsume;
                        remainingToConsume -= toConsume;
                        
                        // Create StockTransaction for export (don't save yet)
                        var stockTransaction = new StockTransaction
                        {
                            IngredientId = recipe.IngredientId,
                            BatchId = batch.BatchId,
                            Quantity = toConsume,
                            Type = "Export",
                            TransactionDate = DateTime.Now,
                            Note = $"Xuất kho cho món {orderDetail.MenuItem.Name} (OrderDetailId: {orderDetailId})"
                        };
                        
                        // Add to context but don't save yet (will save at the end)
                        await _unitOfWork.StockTransaction.AddNewStockTransaction(stockTransaction);
                        await _unitOfWork.InventoryIngredient.UpdateBatchAsync(batch);
                    }
                    
                    if (remainingToConsume > 0)
                    {
                        return (false, $"Lỗi: Không đủ nguyên liệu đã dành riêng để tiêu thụ cho {recipe.Ingredient?.Name ?? "N/A"}. Cần: {totalNeeded}, Đã có: {totalNeeded - remainingToConsume}");
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                return (true, "Đã tiêu thụ nguyên liệu thành công");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi tiêu thụ nguyên liệu: {ex.Message}");
            }
        }

        /// <summary>
        /// Release reserved batches when Hủy món (cancel order detail)
        /// </summary>
        public async Task<(bool success, string message)> ReleaseReservedBatchesForOrderDetailAsync(int orderDetailId)
        {
            try
            {
                // Get order detail with menu item and recipes
                var orderDetail = await _unitOfWork.OrderDetails.GetByIdWithMenuItemAsync(orderDetailId);
                if (orderDetail == null || orderDetail.MenuItem == null)
                {
                    return (false, "Không tìm thấy món ăn");
                }

                // Get recipes for this menu item
                var recipes = await _unitOfWork.MenuItem.GetRecipeByMenuItem(orderDetail.MenuItem.MenuItemId);
                if (!recipes.Any())
                {
                    return (true, "Món này không cần nguyên liệu");
                }

                var orderQuantity = orderDetail.Quantity;

                // For each ingredient in the recipe, release reserved batches
                foreach (var recipe in recipes)
                {
                    var totalToRelease = recipe.QuantityNeeded * orderQuantity;
                    
                    // Get batches with reserved quantity for this ingredient
                    var batches = await _unitOfWork.InventoryIngredient.getBatchById(recipe.IngredientId);
                    var batchesList = batches.ToList();
                    
                    decimal remainingToRelease = totalToRelease;
                    
                    foreach (var batch in batchesList.OrderBy(b => b.ExpiryDate ?? DateOnly.MaxValue).ThenBy(b => b.CreatedAt))
                    {
                        if (remainingToRelease <= 0) break;
                        
                        if (batch.QuantityReserved <= 0) continue;
                        
                        var toRelease = Math.Min(batch.QuantityReserved, remainingToRelease);
                        batch.QuantityReserved -= toRelease;
                        remainingToRelease -= toRelease;
                        
                        await _unitOfWork.InventoryIngredient.UpdateBatchAsync(batch);
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                return (true, "Đã giải phóng nguyên liệu đã dành riêng thành công");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi giải phóng nguyên liệu: {ex.Message}");
            }
        }
    }
}
