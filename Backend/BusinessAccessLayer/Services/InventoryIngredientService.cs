using AutoMapper;
using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.DTOs.Kitchen;
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

        public async Task<bool> UpdateBatchWarehouse(int idBatch, int idWarehouse, bool isActive)
        {
            var result = await _unitOfWork.InventoryIngredient.UpdateBatchWarehouse(idBatch,idWarehouse, isActive);
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

        public async Task<(bool success, string message)> ReserveBatchesForOrderDetailAsync(int orderDetailId)
        {
            try
            {
                // Get order detail with menu item and recipes
                var orderDetail = await _unitOfWork.OrderDetails.GetByIdWithMenuItemAsync(orderDetailId);
                if (orderDetail == null)
                {
                    return (false, "Không tìm thấy món ăn");
                }

                // Nếu OrderDetail không gắn với MenuItem (ví dụ: dòng combo), bỏ qua inventory
                if (orderDetail.MenuItem == null)
                {
                    return (true, "Món này không cần nguyên liệu");
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

        public async Task<(bool success, string message)> ConsumeReservedBatchesForOrderDetailAsync(int orderDetailId)
        {
            // Get order detail with menu item and recipes
            var orderDetail = await _unitOfWork.OrderDetails.GetByIdWithMenuItemAsync(orderDetailId);
            if (orderDetail == null)
            {
                return (false, "Không tìm thấy món ăn");
            }

            // Sử dụng Quantity để consume
            return await ConsumeReservedBatchesForOrderDetailWithQuantityAsync(orderDetailId, orderDetail.Quantity);
        }

        public async Task<(bool success, string message)> ConsumeReservedBatchesForOrderDetailWithQuantityAsync(int orderDetailId, int quantityToConsume)
        {
            try
            {
                // Get order detail with menu item and recipes
                var orderDetail = await _unitOfWork.OrderDetails.GetByIdWithMenuItemAsync(orderDetailId);
                if (orderDetail == null)
                {
                    return (false, "Không tìm thấy món ăn");
                }

                // Nếu OrderDetail không gắn với MenuItem (ví dụ: dòng combo), bỏ qua inventory
                if (orderDetail.MenuItem == null)
                {
                    return (true, "Món này không cần nguyên liệu");
                }

                // Get recipes for this menu item
                var recipes = await _unitOfWork.MenuItem.GetRecipeByMenuItem(orderDetail.MenuItem.MenuItemId);
                if (!recipes.Any())
                {
                    return (true, "Món này không cần nguyên liệu");
                }

                var orderQuantity = quantityToConsume;

                // For each ingredient in the recipe, consume from reserved batches
                foreach (var recipe in recipes)
                {
                    var totalNeeded = recipe.QuantityNeeded * orderQuantity;
                    
                    // Get batches with reserved quantity for this ingredient (FEFO - First Expiry First Out)
                    var batchesList = await _unitOfWork.InventoryIngredient.GetReservedBatchesByIngredientAsync(recipe.IngredientId);
                    
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
        public async Task<List<IngredientPickupDTO>> GetIngredientPickupListAsync(string? categoryName = null)
        {
            var result = new List<IngredientPickupDTO>();

            // Lấy tất cả active orders
            var activeOrders = await _unitOfWork.Orders.GetActiveOrdersForStationAsync();

            // Lấy tất cả OrderDetails có status = "Cooking" hoặc "Late"
            var cookingOrderDetails = activeOrders
                .SelectMany(o => o.OrderDetails)
                .Where(od => od.Status != null && 
                             (od.Status.Equals("Cooking", StringComparison.OrdinalIgnoreCase) || 
                              od.Status.Equals("Late", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // Filter theo CategoryMenu nếu có
            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                // Decode HTML entities
                var decodedCategoryName = System.Net.WebUtility.HtmlDecode(categoryName);
                if (decodedCategoryName.Contains("&#"))
                {
                    decodedCategoryName = System.Text.RegularExpressions.Regex.Replace(
                        decodedCategoryName,
                        @"&#x([0-9A-Fa-f]+);",
                        m => {
                            var hex = m.Groups[1].Value;
                            var code = Convert.ToInt32(hex, 16);
                            return char.ConvertFromUtf32(code);
                        }
                    );
                    decodedCategoryName = System.Text.RegularExpressions.Regex.Replace(
                        decodedCategoryName,
                        @"&#(\d+);",
                        m => {
                            var dec = int.Parse(m.Groups[1].Value);
                            return char.ConvertFromUtf32(dec).ToString();
                        }
                    );
                }
                decodedCategoryName = decodedCategoryName.Trim();

                cookingOrderDetails = cookingOrderDetails
                    .Where(od => od.MenuItem != null && 
                                 od.MenuItem.Category != null &&
                                 od.MenuItem.Category.CategoryName != null &&
                                 od.MenuItem.Category.CategoryName.Equals(decodedCategoryName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Với mỗi OrderDetail, lấy recipes và tính toán nguyên liệu cần lấy
            foreach (var orderDetail in cookingOrderDetails)
            {
                if (orderDetail.MenuItem == null) continue;

                // Tìm Order chứa OrderDetail này để lấy thông tin Table
                var parentOrder = activeOrders.FirstOrDefault(o => o.OrderId == orderDetail.OrderId);
                var tableName = parentOrder?.Reservation?.ReservationTables?.FirstOrDefault()?.Table?.TableNumber;

                // Lấy recipes cho menu item này
                var recipes = await _unitOfWork.MenuItem.GetRecipeByMenuItem(orderDetail.MenuItem.MenuItemId);
                if (!recipes.Any()) continue;

                var orderQuantity = orderDetail.Quantity;

                // Với mỗi recipe, tính toán nguyên liệu cần và lấy batches đã reserve
                foreach (var recipe in recipes)
                {
                    if (recipe.Ingredient == null) continue;

                    var totalNeeded = recipe.QuantityNeeded * orderQuantity;

                    // Lấy các batches đã reserve cho ingredient này (FEFO)
                    var reservedBatches = await _unitOfWork.InventoryIngredient.GetReservedBatchesByIngredientAsync(recipe.IngredientId);
                    
                    if (!reservedBatches.Any()) continue;

                    // Phân bổ số lượng cần lấy từ mỗi batch (theo logic FEFO, giống như khi reserve)
                    decimal remainingToAllocate = totalNeeded;

                    foreach (var batch in reservedBatches)
                    {
                        if (remainingToAllocate <= 0) break;

                        // Số lượng có thể lấy từ batch này (không vượt quá số đã reserve)
                        var availableFromBatch = Math.Min(batch.QuantityReserved, remainingToAllocate);
                        
                        if (availableFromBatch <= 0) continue;

                        // Lấy thông tin warehouse
                        var warehouse = batch.Warehouse;

                        // Tạo DTO
                        var pickupDto = new IngredientPickupDTO
                        {
                            OrderDetailId = orderDetail.OrderDetailId,
                            MenuItemName = orderDetail.MenuItem.Name,
                            OrderQuantity = orderQuantity,
                            OrderId = orderDetail.OrderId,
                            TableName = tableName,
                            
                            IngredientId = recipe.IngredientId,
                            IngredientName = recipe.Ingredient.Name,
                            UnitName = recipe.Ingredient.Unit?.UnitName,
                            
                            BatchId = batch.BatchId,
                            WarehouseId = warehouse?.WarehouseId ?? 0,
                            WarehouseName = warehouse?.Name ?? "Không xác định",
                            ExpiryDate = batch.ExpiryDate,
                            QuantityToPick = availableFromBatch,
                            QuantityReserved = batch.QuantityReserved,
                            IsUrgent = orderDetail.IsUrgent
                        };

                        result.Add(pickupDto);
                        remainingToAllocate -= availableFromBatch;
                    }
                }
            }

            // Sắp xếp: món ưu tiên (IsUrgent = true) hiển thị trước, sau đó theo tên món
            return result
                .OrderByDescending(r => r.IsUrgent)
                .ThenBy(r => r.MenuItemName)
                .ThenBy(r => r.IngredientName)
                .ToList();
        }

        public async Task<List<IngredientShortageDTO>> GetIngredientShortageListAsync()
        {
            var result = new List<IngredientShortageDTO>();

            // Lấy tất cả active orders
            var activeOrders = await _unitOfWork.Orders.GetActiveOrdersForStationAsync();

            // Lấy tất cả OrderDetails có status = "Cooking" hoặc "Late"
            var cookingOrderDetails = activeOrders
                .SelectMany(o => o.OrderDetails)
                .Where(od => od.Status != null && 
                             (od.Status.Equals("Cooking", StringComparison.OrdinalIgnoreCase) || 
                              od.Status.Equals("Late", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // Với mỗi OrderDetail, kiểm tra thiếu nguyên liệu
            foreach (var orderDetail in cookingOrderDetails)
            {
                if (orderDetail.MenuItem == null) continue;

                // Tìm Order chứa OrderDetail này để lấy thông tin Table
                var parentOrder = activeOrders.FirstOrDefault(o => o.OrderId == orderDetail.OrderId);
                var tableName = parentOrder?.Reservation?.ReservationTables?.FirstOrDefault()?.Table?.TableNumber;

                // Lấy recipes cho menu item này
                var recipes = await _unitOfWork.MenuItem.GetRecipeByMenuItem(orderDetail.MenuItem.MenuItemId);
                if (!recipes.Any()) continue;

                var orderQuantity = orderDetail.Quantity;

                // Với mỗi recipe, kiểm tra thiếu nguyên liệu
                foreach (var recipe in recipes)
                {
                    if (recipe.Ingredient == null) continue;

                    var totalNeeded = recipe.QuantityNeeded * orderQuantity;

                    // Lấy các batches đã reserve cho ingredient này
                    var reservedBatches = await _unitOfWork.InventoryIngredient.GetReservedBatchesByIngredientAsync(recipe.IngredientId);
                    
                    // Tính tổng số lượng đã reserve
                    var totalReserved = reservedBatches.Sum(b => b.QuantityReserved);

                    // Nếu số lượng cần lớn hơn số lượng đã reserve thì thiếu
                    if (totalNeeded > totalReserved)
                    {
                        var shortageQuantity = totalNeeded - totalReserved;

                        var shortageDto = new IngredientShortageDTO
                        {
                            OrderDetailId = orderDetail.OrderDetailId,
                            MenuItemName = orderDetail.MenuItem.Name,
                            OrderId = orderDetail.OrderId,
                            TableName = tableName,
                            
                            IngredientId = recipe.IngredientId,
                            IngredientName = recipe.Ingredient.Name,
                            UnitName = recipe.Ingredient.Unit?.UnitName,
                            
                            RequiredQuantity = totalNeeded,
                            ReservedQuantity = totalReserved,
                            ShortageQuantity = shortageQuantity,
                            IsUrgent = orderDetail.IsUrgent
                        };

                        result.Add(shortageDto);
                    }
                }
            }

            // Sắp xếp: món ưu tiên (IsUrgent = true) hiển thị trước, sau đó theo tên món
            return result
                .OrderByDescending(r => r.IsUrgent)
                .ThenBy(r => r.MenuItemName)
                .ThenBy(r => r.IngredientName)
                .ToList();
        }
    }
}
