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

                    // Lấy các batch khả dụng và BỎ QUA batch đang được kiểm kê (AuditStatus = processing)
                    var rawAvailableBatches = await _unitOfWork.InventoryIngredient.GetAvailableBatchesByIngredientAsync(recipe.IngredientId);
                    var availableBatches = new List<InventoryBatch>();
                    foreach (var b in rawAvailableBatches)
                    {
                        var processingAuditId = await _unitOfWork.AuditRepository.CheckExitsAuditStatusRe(b.BatchId);
                        if (!string.IsNullOrEmpty(processingAuditId))
                        {
                            // Bỏ qua batch đang kiểm kê, không reserve QuantityReserved và không trừ thật
                            continue;
                        }
                        availableBatches.Add(b);
                    }

                    decimal remainingToReserve = totalNeeded;
                    var firstBatch = availableBatches.FirstOrDefault();

                    // Reserve từ các batch có available > 0
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

                    // ✅ CHO PHÉP AVAILABLE ÂM: Nếu còn thiếu, reserve thêm vào batch đầu tiên để available có thể âm
                    // Điều này cho phép biết chính xác số lượng thiếu
                    if (remainingToReserve > 0)
                    {
                        // Nếu không có batch nào, cần tạo batch mới hoặc lấy batch đầu tiên (kể cả available <= 0)
                        if (firstBatch == null)
                        {
                            // Lấy tất cả batches (kể cả available <= 0) để có thể reserve vào,
                            // nhưng vẫn bỏ qua batch đang kiểm kê (processing)
                            var allBatchesRaw = await _unitOfWork.InventoryIngredient.GetAllBatchesByIngredientAsync(recipe.IngredientId);
                            var allBatches = new List<InventoryBatch>();
                            foreach (var b in allBatchesRaw)
                            {
                                var processingAuditId = await _unitOfWork.AuditRepository.CheckExitsAuditStatusRe(b.BatchId);
                                if (!string.IsNullOrEmpty(processingAuditId))
                                {
                                    continue;
                                }
                                allBatches.Add(b);
                            }

                            firstBatch = allBatches
                                .OrderBy(b => b.ExpiryDate ?? DateOnly.MaxValue)
                                .ThenBy(b => b.CreatedAt)
                                .FirstOrDefault();
                        }

                        if (firstBatch != null)
                        {
                            // Reserve phần thiếu vào batch đầu tiên (available sẽ âm)
                            firstBatch.QuantityReserved += remainingToReserve;
                            await _unitOfWork.InventoryIngredient.UpdateBatchAsync(firstBatch);

                            // ✅ QUAN TRỌNG: Save changes trước khi return để đảm bảo available âm được lưu vào DB
                            await _unitOfWork.SaveChangesAsync();

                            // Tính available sau khi reserve (sẽ âm)
                            var finalAvailable = firstBatch.QuantityRemaining - firstBatch.QuantityReserved;

                            // Return false với message thiếu, nhưng đã reserve để available có thể âm
                            return (false, $"Không đủ nguyên liệu: {recipe.Ingredient?.Name ?? "N/A"}. Thiếu: {remainingToReserve} {recipe.Ingredient?.Unit?.UnitName ?? ""}. Available: {finalAvailable}");
                        }
                        else
                        {
                            // Không có batch nào cả, không thể reserve
                            return (false, $"Không đủ nguyên liệu: {recipe.Ingredient?.Name ?? "N/A"}. Thiếu: {remainingToReserve} {recipe.Ingredient?.Unit?.UnitName ?? ""}. Không có batch nào.");
                        }
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
                    // và bỏ qua các batch đang có AuditStatus = processing
                    var rawBatches = await _unitOfWork.InventoryIngredient.GetReservedBatchesByIngredientAsync(recipe.IngredientId);
                    var batchesList = new List<InventoryBatch>();
                    foreach (var b in rawBatches)
                    {
                        var processingAuditId = await _unitOfWork.AuditRepository.CheckExitsAuditStatusRe(b.BatchId);
                        if (!string.IsNullOrEmpty(processingAuditId))
                        {
                            continue;
                        }
                        batchesList.Add(b);
                    }

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

                // Trước đây chỉ release khi status là Pending hoặc Cooking.
                // YÊU CẦU MỚI: Dù hủy món từ đâu, chỉ cần gọi hàm này thì vẫn phải trả lại QuantityReserved.
                // Vì vậy không chặn theo status nữa.

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
                    if (recipe.Ingredient == null) continue;
                    
                    //  Số lượng cần release = số lượng đã reserve cho món này
                    // Ví dụ: món gà luộc cần 100 gà, thì khi hủy phải release 100 gà
                    var totalToRelease = recipe.QuantityNeeded * orderQuantity;
                    
                    //  Get ALL batches for this ingredient (kể cả available <= 0)
                    // Để có thể release từ batch đã reserve (có thể available âm)
                    var batches = await _unitOfWork.InventoryIngredient.GetAllBatchesByIngredientAsync(recipe.IngredientId);
                    var batchesList = batches.OrderBy(b => b.ExpiryDate ?? DateOnly.MaxValue).ThenBy(b => b.CreatedAt).ToList();
                    
                    var totalReservedBefore = batchesList.Sum(b => b.QuantityReserved);
                    
                    if (totalReservedBefore <= 0)
                    {
                        continue; // Skip ingredient này
                    }
                    
                    decimal remainingToRelease = totalToRelease;
                    bool hasChanges = false;
                    
                    //  Release từ các batch có reserved > 0 (theo FEFO)
                    // Ưu tiên release từ batch có expiry sớm nhất trước
                    // QUAN TRỌNG: Chỉ release đúng số lượng totalToRelease, không release tất cả
                    //  QUAN TRỌNG: batchesList đã được track từ GetAllBatchesByIngredientAsync
                    // Chỉ cần thay đổi property, EF Core sẽ tự động detect và save khi SaveChangesAsync
                    foreach (var batch in batchesList)
                    {
                        if (remainingToRelease <= 0) break;
                        
                        if (batch.QuantityReserved <= 0) continue;
                        
                        // Release từ batch này (tối đa là reserved của batch hoặc phần còn lại)
                        // Ví dụ: hủy 1 món gà cần 10000 → chỉ release 10000, không release tất cả reserved
                        var toRelease = Math.Min(batch.QuantityReserved, remainingToRelease);
                        var reservedBefore = batch.QuantityReserved;
                        batch.QuantityReserved -= toRelease;
                        remainingToRelease -= toRelease;
                        hasChanges = true;
                    }
                }

                //  Save changes ngay sau khi update tất cả batches
                // Đảm bảo thay đổi được lưu vào DB
                try
                {
                    await _unitOfWork.SaveChangesAsync();
                }
                catch (Exception saveEx)
                {
                    return (false, $"Lỗi khi lưu thay đổi: {saveEx.Message}");
                }
                
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

            // Lấy OrderDetails trạng thái đang nấu/trễ (Pending, Cooking, Late) để hiển thị nguyên liệu cần thiết
            var cookingOrderDetails = activeOrders
                .SelectMany(o => o.OrderDetails)
                .Where(od =>
                {
                    var status = (od.Status ?? "").Trim().ToLowerInvariant();
                    return status == "pending" || status == "cooking" || status == "late";
                })
                .ToList();

            // Filter theo CategoryMenu nếu có
            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                // Decode HTML entities (bao gồm cả hex entities như &#x1ECB;)
                var decodedCategoryName = System.Net.WebUtility.HtmlDecode(categoryName);
                if (decodedCategoryName.Contains("&#"))
                {
                    // Decode hex entities như &#x1ECB; -> ị
                    decodedCategoryName = System.Text.RegularExpressions.Regex.Replace(
                        decodedCategoryName,
                        @"&#x([0-9A-Fa-f]+);",
                        m => {
                            var hex = m.Groups[1].Value;
                            var code = Convert.ToInt32(hex, 16);
                            return char.ConvertFromUtf32(code);
                        }
                    );
                    // Decode decimal entities như &#1234;
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

            // Lấy tất cả OrderDetails có status = "Pending", "Cooking" hoặc "Late"
            var relatedOrderDetails = activeOrders
                .SelectMany(o => o.OrderDetails)
                .Where(od => od.Status != null &&
                             (od.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase) ||
                              od.Status.Equals("Cooking", StringComparison.OrdinalIgnoreCase) ||
                              od.Status.Equals("Late", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // ✅ Gom nhu cầu theo từng nguyên liệu (không tính thiếu lặp lại cho từng món)
            var ingredientNeeds = new Dictionary<int, (decimal totalNeeded, bool isUrgent, HashSet<string> dishes)>();

            foreach (var orderDetail in relatedOrderDetails)
            {
                if (orderDetail.MenuItem == null) continue;

                var recipes = await _unitOfWork.MenuItem.GetRecipeByMenuItem(orderDetail.MenuItem.MenuItemId);
                if (!recipes.Any()) continue;

                var orderQuantity = orderDetail.Quantity;

                foreach (var recipe in recipes)
                {
                    if (recipe.Ingredient == null) continue;

                    var neededForThisDetail = recipe.QuantityNeeded * orderQuantity;

                    if (!ingredientNeeds.TryGetValue(recipe.IngredientId, out var agg))
                    {
                        agg = (0m, false, new HashSet<string>());
                    }

                    agg.totalNeeded += neededForThisDetail;
                    agg.isUrgent = agg.isUrgent || orderDetail.IsUrgent;
                    agg.dishes.Add(orderDetail.MenuItem.Name);

                    ingredientNeeds[recipe.IngredientId] = agg;
                }
            }

            // ✅ Với mỗi nguyên liệu, tính thiếu 1 lần dựa trên tổng nhu cầu và tồn kho
            foreach (var kvp in ingredientNeeds)
            {
                var ingredientId = kvp.Key;
                var totalNeeded = kvp.Value.totalNeeded;
                var isUrgent = kvp.Value.isUrgent;
                var dishes = kvp.Value.dishes;

                // Lấy thông tin ingredient & batches
                var activeBatches = await _unitOfWork.InventoryIngredient.GetAllBatchesByIngredientAsync(ingredientId);
                if (!activeBatches.Any()) continue;

                var ingredient = activeBatches.First().Ingredient;

                // Available có thể âm (đã dùng vượt), nhưng khi tính thiếu cho các món mới
                // ta không muốn "nhân đôi" phần âm này, nên chỉ tính thiếu thêm so với max(available, 0)
                var availableQuantity = activeBatches.Sum(b => b.QuantityRemaining - b.QuantityReserved);
                var effectiveAvailable = Math.Max(availableQuantity, 0);

                var shortageQuantity = totalNeeded - effectiveAvailable;
                if (shortageQuantity <= 0) continue;

                var totalReserved = activeBatches.Sum(b => b.QuantityReserved);

                var shortageDto = new IngredientShortageDTO
                {
                    // Đây là cảnh báo tổng theo nguyên liệu, không gắn với 1 OrderDetail cụ thể
                    OrderDetailId = 0,
                    MenuItemName = dishes.FirstOrDefault() ?? string.Empty,
                    OrderId = 0,
                    TableName = null,

                    IngredientId = ingredientId,
                    IngredientName = ingredient.Name,
                    UnitName = ingredient.Unit?.UnitName,

                    RequiredQuantity = totalNeeded,
                    ReservedQuantity = totalReserved,
                    ShortageQuantity = shortageQuantity,
                    IsUrgent = isUrgent
                };

                result.Add(shortageDto);
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
