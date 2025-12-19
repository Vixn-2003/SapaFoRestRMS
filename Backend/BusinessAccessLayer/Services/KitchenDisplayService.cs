using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.UnitOfWork.Interfaces;
using BusinessAccessLayer.DTOs.Kitchen;
using DomainAccessLayer.Models;
using BusinessAccessLayer.Services.Interfaces;
using DomainAccessLayer.Enums;

namespace BusinessAccessLayer.Services
{
    public class KitchenDisplayService : IKitchenDisplayService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IInventoryIngredientService _inventoryService;

        public KitchenDisplayService(IUnitOfWork unitOfWork, IInventoryIngredientService inventoryService)
        {
            _unitOfWork = unitOfWork;
            _inventoryService = inventoryService;
        }

        public async Task<List<KitchenOrderCardDto>> GetActiveOrdersAsync(string? statusFilter = null)
        {
            var now = DateTime.Now;

            var activeOrders = await _unitOfWork.Orders.GetActiveOrdersAsync();

            var result = new List<KitchenOrderCardDto>();

            foreach (var order in activeOrders)
            {
                // Get all order details directly
                var orderDetails = order.OrderDetails.ToList();
                if (!orderDetails.Any()) continue;

                //  HIỂN THỊ TẤT CẢ: Bao gồm cả Ready và Done
                // Map OrderDetail (món lẻ + các món trong combo) sang KitchenOrderItemDto
                var items = new List<KitchenOrderItemDto>();

                foreach (var od in orderDetails)
                {
                    var currentStatus = od.Status ?? "Pending";
                    var (calculatedStatus, lateMinutes) = CalculateItemStatus(
                        currentStatus,
                        od.StartedAt,
                        od.MenuItem?.TimeCook ?? 0,
                        now);

                    // 1) Món lẻ (MenuItem trực tiếp trên OrderDetail)
                    if (od.MenuItem != null &&
                        (od.MenuItem.BillingType == ItemBillingType.Unspecified ||
                         od.MenuItem.BillingType == ItemBillingType.KitchenPrepared))
                    {
                        items.Add(new KitchenOrderItemDto
                        {
                            OrderDetailId = od.OrderDetailId,
                            MenuItemName = od.MenuItem?.Name ?? "Unknown",
                            Quantity = od.Quantity,
                            Status = calculatedStatus, // Sử dụng trạng thái đã tính toán
                            Notes = od.Notes,
                            CourseType = od.MenuItem?.CourseType ?? "Other",
                            StartedAt = od.StartedAt,
                            CompletedAt = od.Status == "Done" ? od.CreatedAt : null,
                            ReadyAt = od.ReadyAt,
                            IsUrgent = od.IsUrgent,
                            TimeCook = od.MenuItem?.TimeCook ?? 0, // Thời gian nấu (phút)
                            BatchSize = od.MenuItem?.BatchSize ?? 0,
                            LateMinutes = lateMinutes
                        });
                    }

                    // 2) Các món nằm trong Combo:
                    //    Ưu tiên lấy từ OrderComboItems (nếu đã có record chi tiết),
                    //    nếu chưa có thì fallback sang Combo.ComboItems để vẫn hiển thị được món trong combo.
                    if (od.ComboId.HasValue)
                    {
                        // Case 1: Đã có OrderComboItems -> dùng status riêng từng món con
                        if (od.OrderComboItems != null && od.OrderComboItems.Any())
                        {
                            foreach (var orderComboItem in od.OrderComboItems)
                            {
                                var mi = orderComboItem.MenuItem;
                                if (mi == null) continue;

                                // Chỉ lấy món có BillingType = 0 hoặc 2 (không lấy 1 - ConsumptionBased)
                                if (mi.BillingType != ItemBillingType.Unspecified &&
                                    mi.BillingType != ItemBillingType.KitchenPrepared)
                                {
                                    continue;
                                }

                                // Lấy status từ OrderComboItem
                                var comboItemStatus = orderComboItem.Status ?? "Pending";
                                var (comboCalculatedStatus, comboLateMinutes) = CalculateItemStatus(
                                    comboItemStatus,
                                    orderComboItem.StartedAt,
                                    mi.TimeCook ?? 0,
                                    now);

                                var itemQuantity = od.Quantity * orderComboItem.Quantity;

                                items.Add(new KitchenOrderItemDto
                                {
                                    OrderDetailId = od.OrderDetailId, // vẫn dùng OrderDetailId của combo
                                    OrderComboItemId = orderComboItem.OrderComboItemId, // ID của món con trong combo
                                    MenuItemName = mi.Name,
                                    Quantity = itemQuantity,
                                    Status = comboCalculatedStatus, // Status riêng của từng món con
                                    Notes = orderComboItem.Notes ?? od.Notes, // Ghi chú riêng hoặc của combo
                                    CourseType = mi.CourseType ?? "Other",
                                    StartedAt = orderComboItem.StartedAt,
                                    CompletedAt = comboItemStatus == "Done" ? orderComboItem.CreatedAt : null,
                                    ReadyAt = orderComboItem.ReadyAt,
                                    IsUrgent = orderComboItem.IsUrgent || od.IsUrgent,
                                    TimeCook = mi.TimeCook ?? 0,
                                    BatchSize = mi.BatchSize ?? 0,
                                    LateMinutes = comboLateMinutes
                                });
                            }
                        }
                        // Case 2: Chưa migrate / chưa tạo OrderComboItems -> fallback sang Combo.ComboItems (trạng thái chung)
                        else if (od.Combo != null && od.Combo.ComboItems != null)
                        {
                            foreach (var comboItem in od.Combo.ComboItems)
                            {
                                var mi = comboItem.MenuItem;
                                if (mi == null) continue;

                                if (mi.BillingType != ItemBillingType.Unspecified &&
                                    mi.BillingType != ItemBillingType.KitchenPrepared)
                                {
                                    continue;
                                }

                                var itemQuantity = od.Quantity * comboItem.Quantity;

                                items.Add(new KitchenOrderItemDto
                                {
                                    OrderDetailId = od.OrderDetailId,
                                    // OrderComboItemId = null vì chưa có record chi tiết
                                    MenuItemName = mi.Name,
                                    Quantity = itemQuantity,
                                    Status = calculatedStatus, // dùng status chung của dòng combo
                                    Notes = od.Notes,
                                    CourseType = mi.CourseType ?? "Other",
                                    StartedAt = od.StartedAt,
                                    CompletedAt = od.Status == "Done" ? od.CreatedAt : null,
                                    ReadyAt = od.ReadyAt,
                                    IsUrgent = od.IsUrgent,
                                    TimeCook = mi.TimeCook ?? 0,
                                    BatchSize = mi.BatchSize ?? 0,
                                    LateMinutes = lateMinutes
                                });
                            }
                        }
                    }
                }

                // ✅ ẨN HOÀN TOÀN các đơn đã Completed khỏi khu vực đơn đang xử lý trong bếp
                // Đơn Completed chỉ hiển thị ở panel "Đơn vừa hoàn thành" bên trái (nếu bật), không hiển thị ở các filter trạng thái
                var normalizedOrderStatus = (order.Status ?? string.Empty).Trim();
                var isCompletedOrder =
                    string.Equals(normalizedOrderStatus, "Completed", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(normalizedOrderStatus, "Hoàn thành", StringComparison.OrdinalIgnoreCase);

                if (isCompletedOrder)
                {
                    // Đơn đã hoàn thành – không hiển thị trong khu vực đơn đang có trong bếp
                    continue;
                }

                // ✅ Filter by status nếu có
                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    items = items.Where(i => i.Status == statusFilter).ToList();
                }

                //  THÊM: Sort items by course type (Khai vị -> Món chính -> Tráng miệng)
                items = SortItemsByCourseType(items);

                //  SỬA: Chỉ bỏ qua order nếu không có items nào (kể cả Done)
                if (!items.Any())
                {
                    continue;
                }

                var waitingMinutes = (int)((now - (order.CreatedAt ?? now)).TotalMinutes);

                //  Đếm các trạng thái (bao gồm cả Done)
                var lateCount = items.Count(i => i.Status == "Late");
                var readyCount = items.Count(i => i.Status == "Ready");
                var doneCount = items.Count(i => i.Status == "Done");

                var card = new KitchenOrderCardDto
                {
                    OrderId = order.OrderId,
                    OrderNumber = $"A{order.OrderId:D2}", // Format: A01, A02...
                    TableNumber = GetTableNumber(order),
                    NumberOfGuests = GetNumberOfGuests(order), // Số lượng người của bàn
                    CreatedAt = order.CreatedAt ?? DateTime.Now,
                    WaitingMinutes = waitingMinutes,
                    PriorityLevel = GetPriorityLevel(waitingMinutes),
                    TotalItems = items.Count,
                    CompletedItems = readyCount + doneCount, //  SỬA: Ready + Done = Completed
                    LateItems = lateCount,
                    ReadyItems = readyCount,
                    Items = items
                };

                result.Add(card);
            }

            return result;
        }

        public async Task<List<KitchenOrderCardDto>> GetOrdersByCourseTypeAsync(string courseType)
        {
            var allOrders = await GetActiveOrdersAsync();

            // Filter items by course type
            foreach (var order in allOrders)
            {
                order.Items = order.Items
                    .Where(i => i.CourseType.Equals(courseType, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Remove orders with no items of this course type
            return allOrders.Where(o => o.Items.Any()).ToList();
        }

        public async Task<StatusUpdateResponse> UpdateItemStatusAsync(UpdateItemStatusRequest request)
        {
            try
            {
                // Validate request
                if (request == null)
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "Request is required"
                    };
                }

                if (request.OrderDetailId <= 0)
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "OrderDetailId is required and must be greater than 0"
                    };
                }

                if (string.IsNullOrWhiteSpace(request.NewStatus))
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "NewStatus is required"
                    };
                }

                //  Nếu có OrderComboItemId, cập nhật OrderComboItem (món trong combo)
                if (request.OrderComboItemId.HasValue && request.OrderComboItemId.Value > 0)
                {
                    return await UpdateOrderComboItemStatusAsync(request);
                }

                //  Nếu không có OrderComboItemId, cập nhật OrderDetail (món lẻ)
                var orderDetail = await _unitOfWork.OrderDetails.GetByIdWithMenuItemAsync(request.OrderDetailId);

                if (orderDetail == null)
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "Item not found"
                    };
                }

                //  A3: Kiểm tra order có bị hủy hoặc completed từ trạm khác không
                var order = await _unitOfWork.Orders.GetByIdAsync(orderDetail.OrderId);
                if (order == null)
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "Order đã thay đổi từ hệ thống. Vui lòng kiểm tra lại."
                    };
                }

                if (order.Status == "Cancelled" || order.Status == "Hủy")
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "Order đã thay đổi từ hệ thống. Đơn hàng đã bị hủy, vui lòng kiểm tra lại."
                    };
                }

                if (order.Status == "Completed" || order.Status == "Hoàn thành")
                {
                    // Cho phép update item trong order completed (ví dụ: unfulfill)
                    // Không block ở đây
                }

                //  LOGIC MỚI: Nếu là combo và chưa có OrderComboItems, tự động tạo khi fire
                if (orderDetail.ComboId.HasValue && 
                    (orderDetail.OrderComboItems == null || !orderDetail.OrderComboItems.Any()))
                {
                    // Load Combo với ComboItems
                    var orderDetailWithCombo = await _unitOfWork.Payments.GetOrderDetailByIdAsync(request.OrderDetailId);
                    if (orderDetailWithCombo?.Combo?.ComboItems != null && orderDetailWithCombo.Combo.ComboItems.Any())
                    {
                        // Tạo OrderComboItems cho tất cả món trong combo (chỉ món KitchenPrepared)
                        foreach (var comboItem in orderDetailWithCombo.Combo.ComboItems)
                        {
                            var menuItem = comboItem.MenuItem;
                            if (menuItem == null) continue;

                            // Chỉ tạo OrderComboItem cho món có BillingType = KitchenPrepared hoặc Unspecified
                            if (menuItem.BillingType != ItemBillingType.Unspecified &&
                                menuItem.BillingType != ItemBillingType.KitchenPrepared)
                            {
                                continue;
                            }

                            var orderComboItem = new OrderComboItem
                            {
                                OrderDetailId = orderDetail.OrderDetailId,
                                MenuItemId = comboItem.MenuItemId,
                                Quantity = comboItem.Quantity,
                                Status = "Pending", // Sẽ được cập nhật thành Cooking ngay sau đó
                                Notes = orderDetail.Notes,
                                IsUrgent = orderDetail.IsUrgent,
                                CreatedAt = DateTime.UtcNow
                            };

                            await _unitOfWork.OrderComboItems.AddAsync(orderComboItem);
                        }
                        await _unitOfWork.SaveChangesAsync();
                        
                        // Reload OrderComboItems để có trong orderDetail
                        var newOrderComboItems = await _unitOfWork.OrderComboItems.GetByOrderDetailIdAsync(request.OrderDetailId);
                        orderDetail.OrderComboItems = newOrderComboItems;
                    }
                }

                // Validate status transition: Pending → Cooking → Ready/Done
                var currentStatus = (orderDetail.Status ?? "Pending").Trim();
                var newStatus = request.NewStatus.Trim();

                // Normalize status for comparison (handle both English và Vietnamese)
                var normalizedCurrentStatus = NormalizeStatus(currentStatus);
                var normalizedNewStatus = NormalizeStatus(newStatus);

                // Nếu trạng thái mới giống trạng thái hiện tại → coi như thành công, không làm gì thêm (idempotent)
                if (normalizedCurrentStatus == normalizedNewStatus)
                {
                    return new StatusUpdateResponse
                    {
                        Success = true,
                        Message = "Trạng thái món đã ở đúng trạng thái hiện tại",
                        ReservationId = (int)order.ReservationId,
                    };
                }
                
                // Validate status transitions
                if (normalizedCurrentStatus == "Pending")
                {
                    // From Pending, only allow transition to Cooking
                    if (normalizedNewStatus != "Cooking")
                    {
                        return new StatusUpdateResponse
                        {
                            Success = false,
                            Message = $"Không thể chuyển từ trạng thái 'Chờ' sang '{newStatus}'. Phải chuyển sang 'Đang nấu' trước."
                        };
                    }
                    
                    //  VALIDATION: Kiểm tra available quantity trước khi cho phép chuyển sang Cooking
                    // Nếu available < 0 (thiếu nguyên liệu), không cho phép chuyển sang Cooking
                    if (orderDetail.MenuItem != null)
                    {
                        var recipes = await _unitOfWork.MenuItem.GetRecipeByMenuItem(orderDetail.MenuItem.MenuItemId);
                        if (recipes.Any())
                        {
                            var orderQuantity = orderDetail.Quantity;
                            var shortageMessages = new List<string>();
                            
                            foreach (var recipe in recipes)
                            {
                                if (recipe.Ingredient == null) continue;
                                
                                // Lấy tất cả batches (kể cả available <= 0) để kiểm tra available thực tế
                                var allBatches = await _unitOfWork.InventoryIngredient.GetAllBatchesByIngredientAsync(recipe.IngredientId);
                                
                                // Tính available thực tế (có thể âm)
                                var availableQuantity = allBatches.Sum(b => b.QuantityRemaining - b.QuantityReserved);
                                
                                // Tính số lượng cần cho món này
                                var totalNeeded = recipe.QuantityNeeded * orderQuantity;
                                
                                // Nếu available < 0 hoặc available < totalNeeded, thì thiếu nguyên liệu
                                if (availableQuantity < 0 || availableQuantity < totalNeeded)
                                {
                                    var shortage = totalNeeded - availableQuantity;
                                    if (shortage > 0)
                                    {
                                        shortageMessages.Add($"{recipe.Ingredient.Name}: Thiếu {shortage} {recipe.Ingredient.Unit?.UnitName ?? ""}");
                                    }
                                }
                            }
                            
                            // Nếu có nguyên liệu thiếu, không cho phép chuyển sang Cooking
                            if (shortageMessages.Any())
                            {
                                return new StatusUpdateResponse
                                {
                                    Success = false,
                                    Message = $"Không đủ nguyên liệu để nấu món này. {string.Join("; ", shortageMessages)}"
                                };
                            }
                        }
                    }
                    
                    // Lưu thời gian bắt đầu nấu
                    orderDetail.StartedAt = DateTime.Now;
                    
                    //  Nếu là combo và đã có OrderComboItems, cập nhật status của tất cả món con sang Cooking
                    if (orderDetail.ComboId.HasValue && 
                        orderDetail.OrderComboItems != null && 
                        orderDetail.OrderComboItems.Any())
                    {
                        foreach (var orderComboItem in orderDetail.OrderComboItems)
                        {
                            orderComboItem.Status = "Cooking";
                            orderComboItem.StartedAt = DateTime.Now;
                            await _unitOfWork.OrderComboItems.UpdateAsync(orderComboItem);
                        }
                    }
                }
                else if (normalizedCurrentStatus == "Cooking" || normalizedCurrentStatus == "Late")
                {
                    // From Cooking/Late, allow transition to Ready or Done
                    if (normalizedNewStatus != "Ready" && normalizedNewStatus != "Done")
                    {
                        return new StatusUpdateResponse
                        {
                            Success = false,
                            Message = $"Không thể chuyển từ trạng thái '{currentStatus}' sang '{newStatus}'. Chỉ có thể chuyển sang 'Sẵn sàng' hoặc 'Hoàn thành'."
                        };
                    }
                    
                    // ✅ KIỂM TRA: Nếu ReadyAt đã có giá trị → món đã từng Ready/Done (đã consume inventory)
                    // → Không consume lại để tránh trừ nguyên liệu 2 lần khi recall
                    var hasBeenConsumed = orderDetail.ReadyAt.HasValue;
                    
                    // Khi rời Cooking/Late sang Ready/Done → consume nguyên liệu thật (chỉ nếu chưa consume)
                    if (!hasBeenConsumed && orderDetail.MenuItem?.BillingType != ItemBillingType.ConsumptionBased)
                    {
                        var consumeResult = await _inventoryService.ConsumeReservedBatchesForOrderDetailAsync(request.OrderDetailId);
                        if (!consumeResult.success)
                        {
                            return new StatusUpdateResponse
                            {
                                Success = false,
                                Message = consumeResult.message
                            };
                        }
                    }

                    // Nếu chuyển sang Ready, lưu thời gian (hoặc giữ nguyên nếu đã có)
                    if (normalizedNewStatus == "Ready" && !orderDetail.ReadyAt.HasValue)
                    {
                        orderDetail.ReadyAt = DateTime.Now;
                    }
                }
                else if (normalizedCurrentStatus == "Ready")
                {
                    // From Ready, only allow transition to Done
                    // Không cho phép quay lại Cooking từ nút "bắt đầu nấu" vì món đã sẵn sàng
                    // Nếu cần "hủy sẵn sàng", cần có nút/chức năng riêng
                    if (normalizedNewStatus == "Done")
                    {
                        // Đã consume khi chuyển sang Cooking, Done chỉ là bước hoàn tất hiển thị
                    }
                    else
                    {
                        return new StatusUpdateResponse
                        {
                            Success = false,
                            Message = $"Không thể chuyển từ trạng thái 'Sẵn sàng' sang '{newStatus}'. Món đã sẵn sàng, chỉ có thể chuyển sang 'Hoàn thành'."
                        };
                    }
                }
                else if (normalizedCurrentStatus == "Done")
                {
                    // From Done, only allow transition back to Cooking (unfulfill)
                    if (normalizedNewStatus != "Cooking")
                    {
                        return new StatusUpdateResponse
                        {
                            Success = false,
                            Message = $"Không thể chuyển từ trạng thái 'Hoàn thành' sang '{newStatus}'. Chỉ có thể quay lại 'Đang nấu'."
                        };
                    }
                    // Reset StartedAt khi quay lại Cooking
                    // ✅ Reset ReadyAt = null để đánh dấu cần consume lại inventory khi chuyển sang Ready
                    // (Khác với Recall: Recall giữ nguyên ReadyAt để không consume lại)
                    orderDetail.StartedAt = DateTime.Now;
                    orderDetail.ReadyAt = null;
                }

                // Update status trên OrderDetail (nguồn chính) - luôn lưu bằng tiếng Anh
                orderDetail.Status = normalizedNewStatus;

                await _unitOfWork.OrderDetails.UpdateAsync(orderDetail);
                await _unitOfWork.SaveChangesAsync();

                // Đồng bộ trạng thái cấp đơn sau khi món lẻ được cập nhật
                await UpdateKitchenOrderStatusAsync(orderDetail.OrderId);

                return new StatusUpdateResponse
                {
                    Success = true,
                    Message = "Status updated successfully",
                    UpdatedItem = new KitchenOrderItemDto
                    {
                        OrderDetailId = orderDetail.OrderDetailId,
                        MenuItemName = orderDetail.MenuItem.Name,
                        Quantity = orderDetail.Quantity,
                        Status = orderDetail.Status ?? "Pending",
                        Notes = orderDetail.Notes,
                        CourseType = orderDetail.MenuItem.CourseType ?? "Other",
                        StartedAt = orderDetail.StartedAt,
                        CompletedAt = orderDetail.Status == "Done" ? DateTime.Now : null,
                        ReadyAt = orderDetail.ReadyAt,
                        IsUrgent = orderDetail.IsUrgent,
                        TimeCook = orderDetail.MenuItem.TimeCook, // Thời gian nấu (phút)
                        BatchSize = orderDetail.MenuItem.BatchSize,
                        LateMinutes = null
                    }
                };
            }
            catch (Exception ex)
            {
                return new StatusUpdateResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Start cooking with specific quantity (split order detail if quantity < total)
        /// </summary>
        public async Task<StatusUpdateResponse> StartCookingWithQuantityAsync(StartCookingWithQuantityRequest request)
        {
            try
            {
                var orderDetail = await _unitOfWork.OrderDetails.GetByIdWithMenuItemAsync(request.OrderDetailId);
                if (orderDetail == null)
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy món ăn"
                    };
                }

                var currentStatus = NormalizeStatus(orderDetail.Status ?? "Pending");
                if (currentStatus != "Pending")
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = $"Không thể bắt đầu nấu món với trạng thái '{orderDetail.Status}'. Chỉ có thể bắt đầu nấu món đang chờ."
                    };
                }

                var totalQuantity = orderDetail.Quantity;
                var cookingQuantity = request.Quantity;
                
                //  VALIDATION: Kiểm tra available quantity trước khi cho phép start cooking
                if (orderDetail.MenuItem != null)
                {
                    var recipes = await _unitOfWork.MenuItem.GetRecipeByMenuItem(orderDetail.MenuItem.MenuItemId);
                    if (recipes.Any())
                    {
                        var shortageMessages = new List<string>();
                        
                        foreach (var recipe in recipes)
                        {
                            if (recipe.Ingredient == null) continue;
                            
                            // Tính số lượng cần cho số lượng nấu
                            var totalNeeded = recipe.QuantityNeeded * cookingQuantity;
                            
                            // Lấy tất cả batches (kể cả available <= 0) để kiểm tra available thực tế
                            var allBatches = await _unitOfWork.InventoryIngredient.GetAllBatchesByIngredientAsync(recipe.IngredientId);
                            
                            // Tính available thực tế (có thể âm)
                            var availableQuantity = allBatches.Sum(b => b.QuantityRemaining - b.QuantityReserved);
                            
                            // Nếu available < 0 hoặc available < totalNeeded, thì thiếu nguyên liệu
                            if (availableQuantity < 0 || availableQuantity < totalNeeded)
                            {
                                var shortage = totalNeeded - availableQuantity;
                                if (shortage > 0)
                                {
                                    shortageMessages.Add($"{recipe.Ingredient.Name}: Thiếu {shortage} {recipe.Ingredient.Unit?.UnitName ?? ""}");
                                }
                            }
                        }
                        
                        // Nếu có nguyên liệu thiếu, không cho phép start cooking
                        if (shortageMessages.Any())
                        {
                            return new StatusUpdateResponse
                            {
                                Success = false,
                                Message = $"Không đủ nguyên liệu để nấu món này. {string.Join("; ", shortageMessages)}"
                            };
                        }
                    }
                }

                if (cookingQuantity <= 0 || cookingQuantity > totalQuantity)
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = $"Số lượng nấu ({cookingQuantity}) phải lớn hơn 0 và không vượt quá số lượng đơn ({totalQuantity})"
                    };
                }

                // Nếu số lượng nấu = tổng số lượng, update status (chưa consume, sẽ consume khi chuyển Ready)
                if (cookingQuantity == totalQuantity)
                {
                    orderDetail.Status = "Cooking";
                    orderDetail.StartedAt = DateTime.Now;

                    await _unitOfWork.OrderDetails.UpdateAsync(orderDetail);
                    await _unitOfWork.SaveChangesAsync();

                    await UpdateKitchenOrderStatusAsync(orderDetail.OrderId);

                    return new StatusUpdateResponse
                    {
                        Success = true,
                        Message = "Đã bắt đầu nấu",
                        UpdatedItem = new KitchenOrderItemDto
                        {
                            OrderDetailId = orderDetail.OrderDetailId,
                            MenuItemName = orderDetail.MenuItem.Name,
                            Quantity = orderDetail.Quantity,
                            Status = "Cooking",
                            Notes = orderDetail.Notes,
                            CourseType = orderDetail.MenuItem.CourseType ?? "Other",
                            StartedAt = orderDetail.StartedAt,
                            ReadyAt = orderDetail.ReadyAt,
                            IsUrgent = orderDetail.IsUrgent,
                            TimeCook = orderDetail.MenuItem.TimeCook,
                            BatchSize = orderDetail.MenuItem.BatchSize
                        }
                    };
                }

                // Nếu số lượng nấu < tổng số lượng, cần split order detail
                // Tạo order detail mới với số lượng đã chọn, status = Pending (sẽ reserve, rồi chuyển sang Cooking và consume)
                var newOrderDetail = new OrderDetail
                {
                    OrderId = orderDetail.OrderId,
                    MenuItemId = orderDetail.MenuItemId,
                    ComboId = orderDetail.ComboId,
                    Quantity = cookingQuantity,
                    UnitPrice = orderDetail.UnitPrice,
                    Status = "Pending", // Tạo với Pending để reserve trước
                    Notes = orderDetail.Notes,
                    IsUrgent = orderDetail.IsUrgent,
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.OrderDetails.AddAsync(newOrderDetail);
                await _unitOfWork.SaveChangesAsync();

                // Reserve inventory cho order detail mới (vì status = Pending)
                var newReserveResult = await _inventoryService.ReserveBatchesForOrderDetailAsync(newOrderDetail.OrderDetailId);
                if (!newReserveResult.success)
                {
                    // Rollback: xóa order detail mới
                    await _unitOfWork.OrderDetails.DeleteAsync(newOrderDetail.OrderDetailId);
                    await _unitOfWork.SaveChangesAsync();
                    
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = newReserveResult.message
                    };
                }

                //  LOGIC MỚI: Chuyển sang Cooking (consume sẽ diễn ra khi chuyển sang Ready/Done)
                newOrderDetail.Status = "Cooking";
                newOrderDetail.StartedAt = DateTime.Now;

                await _unitOfWork.OrderDetails.UpdateAsync(newOrderDetail);
                await _unitOfWork.SaveChangesAsync();

                // Giảm số lượng của order detail gốc (vẫn giữ status Pending)
                orderDetail.Quantity = totalQuantity - cookingQuantity;
                await _unitOfWork.OrderDetails.UpdateAsync(orderDetail);
                await _unitOfWork.SaveChangesAsync();

                await UpdateKitchenOrderStatusAsync(orderDetail.OrderId);

                return new StatusUpdateResponse
                {
                    Success = true,
                    Message = $"Đã bắt đầu nấu {cookingQuantity}/{totalQuantity} món. Còn lại {orderDetail.Quantity} món đang chờ.",
                    UpdatedItem = new KitchenOrderItemDto
                    {
                        OrderDetailId = newOrderDetail.OrderDetailId,
                        MenuItemName = orderDetail.MenuItem.Name,
                        Quantity = newOrderDetail.Quantity,
                        Status = "Cooking",
                        Notes = newOrderDetail.Notes,
                        CourseType = orderDetail.MenuItem.CourseType ?? "Other",
                        StartedAt = newOrderDetail.StartedAt,
                        ReadyAt = null,
                        IsUrgent = newOrderDetail.IsUrgent,
                        TimeCook = orderDetail.MenuItem.TimeCook,
                        BatchSize = orderDetail.MenuItem.BatchSize
                    }
                };
            }
            catch (Exception ex)
            {
                return new StatusUpdateResponse
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        public async Task<StatusUpdateResponse> CompleteOrderAsync(CompleteOrderRequest request)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetByIdWithDetailsAsync(request.OrderId);

                if (order == null)
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "Order not found"
                    };
                }

                if (!order.OrderDetails.Any())
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "Order has no items"
                    };
                }

                // ✅ SỬA: Kiểm tra tất cả món còn "đang phục vụ" đều Ready hoặc Done (bao gồm cả OrderComboItems)
                // Bỏ qua các món đã hủy / trả (Cancelled / Returned)
                var allItemsReadyOrDone = true;
                
                foreach (var od in order.OrderDetails)
                {
                    // Nếu có OrderComboItems, kiểm tra từng món con
                    if (od.ComboId.HasValue && od.OrderComboItems != null && od.OrderComboItems.Any())
                    {
                        foreach (var oci in od.OrderComboItems)
                        {
                            var status = (oci.Status ?? "Pending").Trim();

                            // Bỏ qua món con đã hủy / trả
                            var statusLower = status.ToLowerInvariant();
                            if (statusLower.Contains("cancelled") || statusLower.Contains("hủy") ||
                                statusLower.Contains("returned") || statusLower.Contains("trả"))
                            {
                                continue;
                            }

                            if (status != "Ready" && status != "Sẵn sàng" && 
                                status != "Done" && status != "Hoàn thành" && status != "Xong")
                            {
                                allItemsReadyOrDone = false;
                                break;
                            }
                        }
                        if (!allItemsReadyOrDone) break;
                    }
                    // Món lẻ
                    else if (od.MenuItemId.HasValue)
                    {
                        var status = (od.Status ?? "Pending").Trim();

                        // Bỏ qua món đã hủy / trả
                        var statusLower = status.ToLowerInvariant();
                        if (statusLower.Contains("cancelled") || statusLower.Contains("hủy") ||
                            statusLower.Contains("returned") || statusLower.Contains("trả"))
                        {
                            continue;
                        }

                        if (status != "Ready" && status != "Sẵn sàng" && 
                            status != "Done" && status != "Hoàn thành" && status != "Xong")
                        {
                            allItemsReadyOrDone = false;
                            break;
                        }
                    }
                }

                if (!allItemsReadyOrDone)
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "Chưa phải tất cả món đều sẵn sàng hoặc hoàn thành"
                    };
                }

                // ✅ Giữ lại logic cũ: sau khi bếp phó ấn "Sẵn sàng", chuyển trạng thái đơn sang "Completed"
                // để thể hiện đơn đã được hoàn tất ở phía bếp.
                order.Status = "Completed";

                await _unitOfWork.Orders.UpdateAsync(order);
                await _unitOfWork.SaveChangesAsync();

                return new StatusUpdateResponse
                {
                    Success = true,
                    Message = "Order completed successfully"
                };
            }
            catch (Exception ex)
            {
                return new StatusUpdateResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        public async Task<List<string>> GetCourseTypesAsync()
        {
            return await _unitOfWork.MenuItem.GetCourseTypesAsync();
        }

        public async Task<List<GroupedMenuItemDto>> GetGroupedItemsByMenuItemAsync(string? statusFilter = null)
        {
            var now = DateTime.Now;

            // Lấy tất cả active orders với order details
            var activeOrders = await _unitOfWork.Orders.GetActiveOrdersForGroupingAsync();

            // Flatten tất cả order details từ tất cả orders
            // Sử dụng structure mới để lưu cả OrderComboItem
            var allItems = new List<(Order Order, OrderDetail OrderDetail, MenuItem MenuItem, OrderComboItem? OrderComboItem)>();

            foreach (var order in activeOrders)
            {
                foreach (var orderDetail in order.OrderDetails)
                {
                    //  XỬ LÝ COMBO: Nếu có OrderComboItems, lấy từng món con
                    if (orderDetail.ComboId.HasValue && 
                        orderDetail.OrderComboItems != null && 
                        orderDetail.OrderComboItems.Any())
                    {
                        foreach (var orderComboItem in orderDetail.OrderComboItems)
                        {
                            var mi = orderComboItem.MenuItem;
                            if (mi == null) continue;

                            // Chỉ lấy món có BillingType = 0 hoặc 2
                            if (mi.BillingType != ItemBillingType.Unspecified &&
                                mi.BillingType != ItemBillingType.KitchenPrepared)
                            {
                                continue;
                            }

                            var comboItemStatus = orderComboItem.Status ?? "Pending";
                            
                            //  THÊM: Filter by status nếu có
                            if (!string.IsNullOrWhiteSpace(statusFilter) && comboItemStatus != statusFilter)
                            {
                                continue;
                            }

                            allItems.Add((order, orderDetail, mi, orderComboItem));
                        }
                    }
                    //  MÓN LẺ: Xử lý OrderDetail trực tiếp
                    else if (orderDetail.MenuItem != null)
                    {
                        //  HIỂN THỊ TẤT CẢ: Bao gồm cả Ready và Done
                        var status = (orderDetail.Status ?? "Pending").Trim();
                        
                        //  THÊM: Filter by status nếu có
                        if (!string.IsNullOrWhiteSpace(statusFilter) && status != statusFilter)
                        {
                            continue;
                        }
                        
                        //  Lấy tất cả các status (Pending, Cooking, Late, Ready, Done)
                        // Chỉ lấy món có BillingType = 0 hoặc 2 (không lấy 1 - ConsumptionBased)
                        if (orderDetail.MenuItem.BillingType == ItemBillingType.Unspecified || 
                            orderDetail.MenuItem.BillingType == ItemBillingType.KitchenPrepared)
                        {
                            allItems.Add((order, orderDetail, orderDetail.MenuItem, null));
                        }
                    }
                }
            }

            // Nhóm theo MenuItemId
            // Lưu ý: allItems chứa những orderDetail có status = Pending, Cooking, Late, Ready (không có Done)
            // TotalQuantity chỉ tính tổng số lượng của các món đang chờ (Pending) thôi
            var grouped = allItems
                .GroupBy(item => new
                {
                    item.MenuItem.MenuItemId,
                    item.MenuItem.Name,
                    item.MenuItem.ImageUrl,
                    item.MenuItem.CourseType,
                    item.MenuItem.TimeCook,
                    item.MenuItem.BatchSize
                })
                .Select(g => new GroupedMenuItemDto
                {
                    MenuItemId = g.Key.MenuItemId,
                    MenuItemName = g.Key.Name,
                    ImageUrl = g.Key.ImageUrl,
                    CourseType = g.Key.CourseType ?? "Other",
                    TimeCook = g.Key.TimeCook, // Thời gian nấu (phút)
                    BatchSize = g.Key.BatchSize,
                    // TotalQuantity chỉ tính tổng số lượng của các món đang chờ (Pending) thôi
                    TotalQuantity = g.Where(item => {
                        var itemStatus = item.OrderComboItem != null 
                            ? (item.OrderComboItem.Status ?? "Pending").Trim()
                            : (item.OrderDetail.Status ?? "Pending").Trim();
                        var normalizedStatus = NormalizeStatus(itemStatus);
                        return normalizedStatus == "Pending";
                    }).Sum(item => item.OrderComboItem != null 
                        ? item.OrderDetail.Quantity * item.OrderComboItem.Quantity 
                        : item.OrderDetail.Quantity),
                    ItemDetails = g.Select(item => new GroupedItemDetailDto
                    {
                        OrderDetailId = item.OrderDetail.OrderDetailId,
                        OrderComboItemId = item.OrderComboItem?.OrderComboItemId,
                        OrderId = item.Order.OrderId,
                        OrderNumber = $"A{item.Order.OrderId:D2}",
                        TableNumber = GetTableNumber(item.Order),
                        Quantity = item.OrderComboItem != null 
                            ? item.OrderDetail.Quantity * item.OrderComboItem.Quantity 
                            : item.OrderDetail.Quantity,
                        Status = item.OrderComboItem != null 
                            ? (item.OrderComboItem.Status ?? "Pending") 
                            : (item.OrderDetail.Status ?? "Pending"), // Default to Pending if null
                        Notes = item.OrderComboItem?.Notes ?? item.OrderDetail.Notes,
                        CreatedAt = item.Order.CreatedAt ?? DateTime.Now,
                        WaitingMinutes = (int)((now - (item.Order.CreatedAt ?? now)).TotalMinutes)
                    }).OrderByDescending(d => d.WaitingMinutes).ToList() // Sắp xếp theo thời gian chờ giảm dần
                })
                .Where(g => g.TotalQuantity > 0) // Chỉ lấy những món có ít nhất 1 món đang chờ
                .ToList();

            return SortGroupedMenuItems(grouped);
        }

        // Helper methods
        private string GetTableNumber(Order order)
        {
            // PRIORITY 1: Get from reservation table
            if (order.Reservation != null && order.Reservation.ReservationTables != null)
            {
                var reservationTable = order.Reservation.ReservationTables
                    .FirstOrDefault(rt => rt.Table != null);

                if (reservationTable?.Table != null)
                {
                    return reservationTable.Table.TableNumber ?? "N/A";
                }

                // Fallback to customer name from reservation
                var reservationCustomer = order.Reservation.Customer?.User?.FullName;
                if (!string.IsNullOrEmpty(reservationCustomer))
                {
                    return reservationCustomer;
                }
            }

            // PRIORITY 2: Get customer name
            if (order.Customer != null && order.Customer.User != null)
            {
                return order.Customer.User.FullName ?? "Khách";
            }

            // FALLBACK: Order type or generic
            return order.OrderType ?? "N/A";
        }

        private int GetNumberOfGuests(Order order)
        {
            // Get number of guests from reservation
            if (order.Reservation != null)
            {
                return order.Reservation.NumberOfGuests;
            }

            // Fallback if no reservation
            return 0;
        }

        private string GetPriorityLevel(int waitingMinutes)
        {
            if (waitingMinutes > 15) return "Critical";  // Red - >15 phút
            if (waitingMinutes >= 10) return "Warning";  // Yellow - 10-15 phút
            return "Normal";                             // White/Light - 1-10 phút
        }

        /// <summary>
        /// Normalize status to English (handle both English and Vietnamese)
        /// </summary>
        private string NormalizeStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return "Pending";

            var statusLower = status.Trim().ToLower();

            // Handle Vietnamese statuses
            if (statusLower.Contains("chờ") || statusLower.Contains("pending"))
                return "Pending";
            if (statusLower.Contains("đang nấu") || statusLower.Contains("chế biến") || statusLower.Contains("cooking"))
                return "Cooking";
            if (statusLower.Contains("trễ") || statusLower.Contains("late"))
                return "Late";
            if (statusLower.Contains("sẵn sàng") || statusLower.Contains("ready"))
                return "Ready";
            if (statusLower.Contains("hoàn thành") || statusLower.Contains("xong") || statusLower.Contains("done"))
                return "Done";
            if (statusLower.Contains("hủy") || statusLower.Contains("cancelled"))
                return "Cancelled";

            // Handle exact English matches (case-insensitive)
            if (statusLower == "pending") return "Pending";
            if (statusLower == "cooking") return "Cooking";
            if (statusLower == "late") return "Late";
            if (statusLower == "ready") return "Ready";
            if (statusLower == "done") return "Done";
            if (statusLower == "cancelled") return "Cancelled";

            // Default: return as-is (capitalize first letter)
            return char.ToUpper(statusLower[0]) + statusLower.Substring(1);
        }

        /// <summary>
        /// Tính toán trạng thái thực tế của món ăn dựa trên status, thời gian nấu và thời gian bắt đầu
        /// </summary>
        private (string CalculatedStatus, int? LateMinutes) CalculateItemStatus(
            string currentStatus, 
            DateTime? startedAt, 
            int? timeCook, 
            DateTime now)
        {
            // Nếu đã Ready hoặc Done, giữ nguyên
            if (currentStatus == "Ready" || currentStatus == "Done")
            {
                return (currentStatus, null);
            }

            // Nếu đang Cooking, kiểm tra xem có trễ không
            if (currentStatus == "Cooking" && startedAt.HasValue && timeCook.HasValue && timeCook.Value > 0)
            {
                var elapsedMinutes = (int)((now - startedAt.Value).TotalMinutes);
                if (elapsedMinutes > timeCook.Value)
                {
                    var lateMinutes = elapsedMinutes - timeCook.Value;
                    return ("Late", lateMinutes);
                }
            }

            // Trả về trạng thái hiện tại
            return (currentStatus ?? "Pending", null);
        }

        /// <summary>
        /// Sort items by course type: Khai vị (0) -> Món chính (1) -> Tráng miệng (2) -> Other (999)
        /// </summary>
        private List<KitchenOrderItemDto> SortItemsByCourseType(List<KitchenOrderItemDto> items)
        {
            var courseTypeOrder = new Dictionary<string, int>
            {
                { "Khai vị", 0 },
                { "Món chính", 1 },
                { "Tráng miệng", 2 }
            };

            return items.OrderBy(item =>
            {
                var courseType = item.CourseType ?? "Other";
                return courseTypeOrder.ContainsKey(courseType) ? courseTypeOrder[courseType] : 999;
            }).ToList();
        }

        private List<GroupedMenuItemDto> SortGroupedMenuItems(List<GroupedMenuItemDto> items)
        {
            if (items == null || items.Count == 0)
            {
                return new List<GroupedMenuItemDto>();
            }

            var sortedItems = new List<GroupedMenuItemDto>(items);
            sortedItems.Sort(CompareGroupedMenuItems);
            return sortedItems;
        }

        private int CompareGroupedMenuItems(GroupedMenuItemDto a, GroupedMenuItemDto b)
        {
            const int LONG_COOK_THRESHOLD = 15;

            var timeCookA = a.TimeCook ?? 0;
            var timeCookB = b.TimeCook ?? 0;
            var isLongCookA = timeCookA > LONG_COOK_THRESHOLD;
            var isLongCookB = timeCookB > LONG_COOK_THRESHOLD;

            if (isLongCookA && isLongCookB)
            {
                if (timeCookB != timeCookA)
                {
                    return timeCookB.CompareTo(timeCookA);
                }

                return CompareByWaitingMinutes(a, b);
            }

            if (isLongCookA && !isLongCookB) return -1;
            if (!isLongCookA && isLongCookB) return 1;

            return CompareByWaitingMinutes(a, b);
        }

        private int CompareByWaitingMinutes(GroupedMenuItemDto a, GroupedMenuItemDto b)
        {
            var waitingA = GetMaxWaitingMinutes(a);
            var waitingB = GetMaxWaitingMinutes(b);

            if (waitingB != waitingA)
            {
                // Higher waiting minutes = older order => xuất hiện trước
                return waitingB.CompareTo(waitingA);
            }

            var nameA = (a.MenuItemName ?? string.Empty).ToLowerInvariant();
            var nameB = (b.MenuItemName ?? string.Empty).ToLowerInvariant();
            return string.Compare(nameA, nameB, StringComparison.Ordinal);
        }

        private int GetMaxWaitingMinutes(GroupedMenuItemDto item)
        {
            if (item == null || item.ItemDetails == null || item.ItemDetails.Count == 0)
            {
                return 0;
            }

            return item.ItemDetails.Max(detail => detail.WaitingMinutes);
        }

        public async Task<StationItemsResponse> GetStationItemsByCategoryAsync(string categoryName)
        {
            var now = DateTime.Now;

            // Decode HTML entities (bao gồm cả hex entities như &#x1ECB;)
            // System.Net.WebUtility.HtmlDecode không decode hex entities, cần dùng System.Web.HttpUtility
            // Hoặc decode thủ công
            if (categoryName.Contains("&#"))
            {
                // Decode hex entities như &#x1ECB; -> ị
                categoryName = System.Text.RegularExpressions.Regex.Replace(
                    categoryName,
                    @"&#x([0-9A-Fa-f]+);",
                    m => {
                        var hex = m.Groups[1].Value;
                        var code = Convert.ToInt32(hex, 16);
                        return char.ConvertFromUtf32(code);
                    }
                );
                // Decode decimal entities như &#1234;
                categoryName = System.Text.RegularExpressions.Regex.Replace(
                    categoryName,
                    @"&#(\d+);",
                    m => {
                        var dec = int.Parse(m.Groups[1].Value);
                        return char.ConvertFromUtf32(dec).ToString();
                    }
                );
            }
            // Decode named entities như &amp; &lt; etc.
            categoryName = System.Net.WebUtility.HtmlDecode(categoryName);
            
            // Trim và normalize
            categoryName = categoryName?.Trim() ?? string.Empty;
            
            // Lấy tất cả active orders với order details thuộc category này
            var activeOrders = await _unitOfWork.Orders.GetActiveOrdersForStationAsync();

            var allItems = new List<StationItemDto>();
            var urgentItems = new List<StationItemDto>();

            foreach (var order in activeOrders)
            {
                foreach (var orderDetail in order.OrderDetails)
                {
                    //  XỬ LÝ COMBO: Nếu có OrderComboItems, lấy từng món con
                    if (orderDetail.ComboId.HasValue && 
                        orderDetail.OrderComboItems != null && 
                        orderDetail.OrderComboItems.Any())
                    {
                        foreach (var orderComboItem in orderDetail.OrderComboItems)
                        {
                            var mi = orderComboItem.MenuItem;
                            if (mi == null || mi.Category == null) continue;

                            // Lọc theo category
                            var catName = mi.Category.CategoryName?.Trim() ?? string.Empty;
                            if (!catName.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            // Chỉ lấy món có BillingType = 0 hoặc 2
                            if (mi.BillingType != ItemBillingType.Unspecified &&
                                mi.BillingType != ItemBillingType.KitchenPrepared)
                            {
                                continue;
                            }

                            var comboItemStatus = orderComboItem.Status ?? "Pending";
                            var normalizedStatus = NormalizeStatus(comboItemStatus);
                            if (normalizedStatus == "Done")
                            {
                                continue; // Bỏ qua Done items
                            }

                            var waitingMinutes = (int)((now - (order.CreatedAt ?? now)).TotalMinutes);
                            var createdAtTime = (order.CreatedAt ?? DateTime.Now).ToString("HH:mm");
                            
                            var fireTime = string.Empty;
                            DateTime? startedAt = null;
                            
                            if (normalizedStatus == "Cooking")
                            {
                                startedAt = orderComboItem.StartedAt ?? orderDetail.CreatedAt;
                                fireTime = startedAt?.ToString("HH:mm") ?? orderDetail.CreatedAt.ToString("HH:mm");
                            }

                            var item = new StationItemDto
                            {
                                MenuItemId = mi.MenuItemId,
                                OrderDetailId = orderDetail.OrderDetailId,
                                OrderComboItemId = orderComboItem.OrderComboItemId,
                                OrderId = order.OrderId,
                                OrderNumber = $"A{order.OrderId:D2}",
                                TableNumber = GetTableNumber(order),
                                MenuItemName = mi.Name,
                                Quantity = orderDetail.Quantity * orderComboItem.Quantity,
                                Status = comboItemStatus,
                                Notes = orderComboItem.Notes ?? orderDetail.Notes,
                                CreatedAt = order.CreatedAt ?? DateTime.Now,
                                CreatedAtTime = createdAtTime,
                                WaitingMinutes = waitingMinutes,
                                IsUrgent = orderComboItem.IsUrgent || orderDetail.IsUrgent,
                                StartedAt = startedAt,
                                FireTime = fireTime,
                                TimeCook = mi.TimeCook ?? 0,
                                BatchSize = mi.BatchSize
                            };

                            allItems.Add(item);

                            if (orderComboItem.IsUrgent || orderDetail.IsUrgent)
                            {
                                urgentItems.Add(item);
                            }
                        }
                    }
                    //  MÓN LẺ: Xử lý OrderDetail trực tiếp
                    else if (orderDetail.MenuItem != null && orderDetail.MenuItem.Category != null)
                    {
                        // Lọc theo category
                        var catName = orderDetail.MenuItem.Category.CategoryName?.Trim() ?? string.Empty;
                        if (!catName.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var status = (orderDetail.Status ?? "Pending").Trim();
                        var normalizedStatus = NormalizeStatus(status);
                        if (normalizedStatus == "Done")
                        {
                            continue; // Bỏ qua Done items
                        }
                        
                        //  Chỉ lấy món có BillingType = 0 hoặc 2
                        if (orderDetail.MenuItem.BillingType != ItemBillingType.Unspecified && 
                            orderDetail.MenuItem.BillingType != ItemBillingType.KitchenPrepared)
                        {
                            continue;
                        }
                        
                        var waitingMinutes = (int)((now - (order.CreatedAt ?? now)).TotalMinutes);
                        var createdAtTime = (order.CreatedAt ?? DateTime.Now).ToString("HH:mm");
                        
                        var fireTime = string.Empty;
                        DateTime? startedAt = null;
                        
                        if (normalizedStatus == "Cooking")
                        {
                            startedAt = orderDetail.StartedAt ?? orderDetail.CreatedAt;
                            fireTime = startedAt?.ToString("HH:mm") ?? orderDetail.CreatedAt.ToString("HH:mm");
                        }

                        var item = new StationItemDto
                        {
                            MenuItemId = orderDetail.MenuItem.MenuItemId,
                            OrderDetailId = orderDetail.OrderDetailId,
                            OrderComboItemId = null,
                            OrderId = order.OrderId,
                            OrderNumber = $"A{order.OrderId:D2}",
                            TableNumber = GetTableNumber(order),
                            MenuItemName = orderDetail.MenuItem.Name,
                            Quantity = orderDetail.Quantity,
                            Status = status,
                            Notes = orderDetail.Notes,
                            CreatedAt = order.CreatedAt ?? DateTime.Now,
                            CreatedAtTime = createdAtTime,
                            WaitingMinutes = waitingMinutes,
                            IsUrgent = orderDetail.IsUrgent,
                            StartedAt = startedAt,
                            FireTime = fireTime,
                            TimeCook = orderDetail.MenuItem?.TimeCook ?? 0,
                            BatchSize = orderDetail.MenuItem.BatchSize
                        };

                        allItems.Add(item);

                        if (orderDetail.IsUrgent)
                        {
                            urgentItems.Add(item);
                        }
                    }
                }
            }

            // Sắp xếp: urgent trước, sau đó theo thời gian chờ giảm dần
            allItems = allItems
                .OrderByDescending(i => i.IsUrgent)
                .ThenByDescending(i => i.WaitingMinutes)
                .ToList();

            urgentItems = urgentItems
                .OrderByDescending(i => i.WaitingMinutes)
                .ToList();

            return new StationItemsResponse
            {
                CategoryName = categoryName,
                AllItems = allItems,
                UrgentItems = urgentItems
            };
        }

        public async Task<StatusUpdateResponse> MarkAsUrgentAsync(MarkAsUrgentRequest request)
        {
            try
            {
                var orderDetail = await _unitOfWork.OrderDetails.GetByIdAsync(request.OrderDetailId);

                if (orderDetail == null)
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "Order detail not found"
                    };
                }

                orderDetail.IsUrgent = request.IsUrgent;
                await _unitOfWork.OrderDetails.UpdateAsync(orderDetail);
                await _unitOfWork.SaveChangesAsync();

                return new StatusUpdateResponse
                {
                    Success = true,
                    Message = request.IsUrgent ? "Đã đánh dấu cần làm ngay" : "Đã bỏ đánh dấu cần làm ngay"
                };
            }
            catch (Exception ex)
            {
                return new StatusUpdateResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        public async Task<List<string>> GetStationCategoriesAsync()
        {
            var categories = await _unitOfWork.MenuCategory.GetCategoryNamesAsync();
            return categories.Distinct().OrderBy(c => c).ToList();
        }

        /// <summary>
        /// Lấy danh sách các order đã hoàn thành gần đây (trong X phút)
        /// Lưu ý: Vì không có CompletedAt field, sẽ lấy các order có items Done
        /// Sẽ lấy tất cả orders có items Done, không filter theo thời gian vì không biết chính xác khi nào Done
        /// </summary>
        public async Task<List<KitchenOrderCardDto>> GetRecentlyFulfilledOrdersAsync(int minutesAgo = 10)
        {
            // Lấy các orders có ít nhất một item đã hoàn tất phục vụ (Ready hoặc Done)
            // Không filter theo thời gian vì không có CompletedAt field
            // Chỉ lấy các order đang active hoặc completed (không lấy orders quá cũ đã thanh toán)
            var orders = await _unitOfWork.Orders.GetRecentlyFulfilledOrdersAsync(minutesAgo);

            var result = new List<KitchenOrderCardDto>();
            var now = DateTime.Now;

            foreach (var order in orders)
            {
                // Lấy tất cả items đã hoàn tất trong order này (Ready hoặc Done)
                var doneItems = order.OrderDetails
                    .Where(od =>
                        string.Equals(NormalizeStatus(od.Status ?? string.Empty), "Done", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(NormalizeStatus(od.Status ?? string.Empty), "Ready", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // Nếu không có món Ready/Done (chỉ toàn Cancelled) thì bỏ qua
                if (!doneItems.Any()) continue;

                // Bắt buộc trạng thái đơn phải là Completed / Hoàn thành
                var orderStatus = (order.Status ?? string.Empty).Trim();
                var orderStatusLower = orderStatus.ToLowerInvariant();
                var isCompletedOrder =
                    orderStatusLower == "completed" ||
                    orderStatusLower == "hoàn thành" ||
                    orderStatusLower.Contains("completed") ||
                    orderStatusLower.Contains("hoàn thành");

                if (!isCompletedOrder)
                {
                    continue;
                }

                var orderCard = new KitchenOrderCardDto
                {
                    OrderId = order.OrderId,
                    OrderNumber = $"A{order.OrderId:D2}",
                    TableNumber = GetTableNumber(order),
                    NumberOfGuests = GetNumberOfGuests(order),
                    CreatedAt = order.CreatedAt ?? DateTime.Now,
                    WaitingMinutes = (int)((now - (order.CreatedAt ?? now)).TotalMinutes),
                    PriorityLevel = GetPriorityLevel((int)((now - (order.CreatedAt ?? now)).TotalMinutes)),
                    TotalItems = order.OrderDetails.Count,
                    CompletedItems = doneItems.Count,
                    Items = doneItems
                        .Where(od => od.MenuItem != null) // Chỉ lấy items có MenuItem
                        .Where(od => od.MenuItem.BillingType == ItemBillingType.Unspecified || od.MenuItem.BillingType == ItemBillingType.KitchenPrepared) // Chỉ lấy món có BillingType = 0 hoặc 2
                        .Select(od => new KitchenOrderItemDto
                        {
                            OrderDetailId = od.OrderDetailId,
                            MenuItemName = od.MenuItem?.Name ?? "Unknown",
                            Quantity = od.Quantity,
                            Status = od.Status ?? "Done",
                            Notes = od.Notes,
                            CourseType = od.MenuItem?.CourseType ?? "Other",
                            IsUrgent = od.IsUrgent,
                            CompletedAt = od.CreatedAt, // Dùng CreatedAt làm proxy (không chính xác 100%)
                            TimeCook = od.MenuItem?.TimeCook ?? 0, // Thời gian nấu (phút)
                            BatchSize = od.MenuItem?.BatchSize ?? 0
                        }).ToList()
                };

                result.Add(orderCard);
            }

            return result;
        }

        /// <summary>
        /// Khôi phục (Recall) một order detail đã Done, đưa nó quay lại trạng thái Pending
        /// </summary>
        public async Task<StatusUpdateResponse> RecallOrderDetailAsync(RecallOrderDetailRequest request)
        {
            try
            {
                var orderDetail = await _unitOfWork.OrderDetails.GetByIdWithMenuItemAsync(request.OrderDetailId);
                
                // Load Order separately if needed
                if (orderDetail?.OrderId != null)
                {
                    var order = await _unitOfWork.Orders.GetByIdWithOrderDetailsAsync(orderDetail.OrderId);
                    if (order != null)
                    {
                        // Note: OrderDetail doesn't have navigation property to Order in this context
                        // We'll check order status separately
                    }
                }

                if (orderDetail == null)
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy món ăn"
                    };
                }

                if (orderDetail.Status != "Done")
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "Món ăn này chưa được đánh dấu hoàn thành, không thể khôi phục"
                    };
                }

                // Khôi phục về trạng thái "Pending"
                orderDetail.Status = "Pending";
                
                // ✅ QUAN TRỌNG: Giữ nguyên ReadyAt để đánh dấu đã consume inventory
                // Khi chuyển lại từ Cooking → Ready, sẽ check ReadyAt.HasValue để không consume lại
                // (Không reset ReadyAt = null như khi Done → Cooking)

                // Đảm bảo Order quay lại trạng thái có thể quản lý
                if (orderDetail.OrderId != null)
                {
                    var order = await _unitOfWork.Orders.GetByIdWithOrderDetailsAsync(orderDetail.OrderId);
                    if (order != null && order.Status == "Completed")
                    {
                        order.Status = "Pending";
                        await _unitOfWork.Orders.UpdateAsync(order);
                    }
                }

                await _unitOfWork.OrderDetails.UpdateAsync(orderDetail);
                await _unitOfWork.SaveChangesAsync();

                await UpdateKitchenOrderStatusAsync(orderDetail.OrderId);

                return new StatusUpdateResponse
                {
                    Success = true,
                    Message = "Đã khôi phục món ăn thành công",
                    UpdatedItem = new KitchenOrderItemDto
                    {
                        OrderDetailId = orderDetail.OrderDetailId,
                        MenuItemName = orderDetail.MenuItem.Name,
                        Quantity = orderDetail.Quantity,
                        Status = orderDetail.Status ?? "Pending",
                        Notes = orderDetail.Notes,
                        CourseType = orderDetail.MenuItem.CourseType ?? "Other",
                        IsUrgent = orderDetail.IsUrgent,
                        TimeCook = orderDetail.MenuItem.TimeCook, // Thời gian nấu (phút)
                        BatchSize = orderDetail.MenuItem.BatchSize
                    }
                };
            }
            catch (Exception ex)
            {
                return new StatusUpdateResponse
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Cập nhật status của món con trong combo (OrderComboItem)
        /// ĐÃ ĐƠN GIẢN HÓA: không đụng tới inventory, chỉ đổi trạng thái trên OrderComboItems
        /// và nếu tất cả món con đã Ready/Done thì cập nhật OrderDetail cha.
        /// </summary>
        private static readonly HashSet<string> KitchenManagedOrderStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Pending",
            "Preparing",
            "Cooking",
            "Ready",
            "Late",
            "Done"
        };

        private async Task<StatusUpdateResponse> UpdateOrderComboItemStatusAsync(UpdateItemStatusRequest request)
        {
            try
            {
                if (!request.OrderComboItemId.HasValue || request.OrderComboItemId.Value <= 0)
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "OrderComboItemId không hợp lệ"
                    };
                }

                var orderComboItem = await _unitOfWork.OrderComboItems.GetByIdWithMenuItemAsync(request.OrderComboItemId.Value);

                if (orderComboItem == null)
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "Món trong combo không tìm thấy"
                    };
                }

                var currentStatus = (orderComboItem.Status ?? "Pending").Trim();
                var newStatus = (request.NewStatus ?? "").Trim();

                var normalizedCurrentStatus = NormalizeStatus(currentStatus);
                var normalizedNewStatus = NormalizeStatus(newStatus);

                // Idempotent: nếu trạng thái không đổi thì coi như thành công
                if (normalizedCurrentStatus == normalizedNewStatus)
                {
                    return new StatusUpdateResponse
                    {
                        Success = true,
                        Message = "Trạng thái món trong combo đã ở đúng trạng thái hiện tại"
                    };
                }

                // Chỉ cho phép các transition hợp lý. Với combo cũng cần kiểm tra thiếu nguyên liệu trước khi cho nấu.
                if (normalizedCurrentStatus == "Pending")
                {
                    if (normalizedNewStatus != "Cooking")
                    {
                        return new StatusUpdateResponse
                        {
                            Success = false,
                            Message = $"Không thể chuyển từ trạng thái 'Chờ' sang '{newStatus}'. Phải chuyển sang 'Đang nấu' trước."
                        };
                    }
                    //  Kiểm tra thiếu nguyên liệu cho món combo trước khi cho phép Cooking
                    if (orderComboItem.MenuItem != null)
                    {
                        var recipes = await _unitOfWork.MenuItem.GetRecipeByMenuItem(orderComboItem.MenuItemId);
                        if (recipes.Any())
                        {
                            var shortageMessages = new List<string>();

                            foreach (var recipe in recipes)
                            {
                                if (recipe.Ingredient == null) continue;

                                // Lấy tất cả batches (kể cả available <= 0) để kiểm tra available thực tế
                                var allBatches = await _unitOfWork.InventoryIngredient.GetAllBatchesByIngredientAsync(recipe.IngredientId);
                                var availableQuantity = allBatches.Sum(b => b.QuantityRemaining - b.QuantityReserved);

                                var totalNeeded = recipe.QuantityNeeded * orderComboItem.Quantity;

                                if (availableQuantity < 0 || availableQuantity < totalNeeded)
                                {
                                    var shortage = totalNeeded - availableQuantity;
                                    if (shortage > 0)
                                    {
                                        shortageMessages.Add($"{recipe.Ingredient.Name}: Thiếu {shortage} {recipe.Ingredient.Unit?.UnitName ?? ""}");
                                    }
                                }
                            }

                            if (shortageMessages.Any())
                            {
                                return new StatusUpdateResponse
                                {
                                    Success = false,
                                    Message = $"Không đủ nguyên liệu để nấu món trong combo. {string.Join("; ", shortageMessages)}"
                                };
                            }
                        }
                    }

                    orderComboItem.StartedAt = DateTime.Now;
                }
                else if (normalizedCurrentStatus == "Cooking" || normalizedCurrentStatus == "Late")
                {
                    if (normalizedNewStatus != "Ready" && normalizedNewStatus != "Done")
                    {
                        return new StatusUpdateResponse
                        {
                            Success = false,
                            Message = $"Không thể chuyển từ trạng thái '{currentStatus}' sang '{newStatus}'. Chỉ có thể chuyển sang 'Sẵn sàng' hoặc 'Hoàn thành'."
                        };
                    }
                    if (normalizedNewStatus == "Ready")
                    {
                        orderComboItem.ReadyAt = DateTime.Now;
                    }
                }
                else if (normalizedCurrentStatus == "Ready")
                {
                    if (normalizedNewStatus == "Done")
                    {
                        // chỉ đánh dấu hoàn thành hiển thị
                    }
                    else if (normalizedNewStatus == "Cooking")
                    {
                        // Hủy sẵn sàng → quay lại Cooking
                        orderComboItem.ReadyAt = null;
                        orderComboItem.StartedAt ??= DateTime.Now;
                    }
                    else
                    {
                        return new StatusUpdateResponse
                        {
                            Success = false,
                            Message = $"Không thể chuyển từ trạng thái 'Sẵn sàng' sang '{newStatus}'. Chỉ có thể chuyển sang 'Hoàn thành' hoặc quay lại 'Đang nấu'."
                        };
                    }
                }
                else if (normalizedCurrentStatus == "Done")
                {
                    if (normalizedNewStatus != "Cooking")
                    {
                        return new StatusUpdateResponse
                        {
                            Success = false,
                            Message = $"Không thể chuyển từ trạng thái 'Hoàn thành' sang '{newStatus}'. Chỉ có thể quay lại 'Đang nấu'."
                        };
                    }
                    orderComboItem.StartedAt = DateTime.Now;
                    orderComboItem.ReadyAt = null;
                }

                // Gán lại status cuối cùng
                orderComboItem.Status = normalizedNewStatus;

                await _unitOfWork.OrderComboItems.UpdateAsync(orderComboItem);
                await _unitOfWork.SaveChangesAsync();

                // Sau khi cập nhật một món con, đồng bộ trạng thái OrderDetail cha và toàn bộ đơn
                if (request.OrderDetailId > 0)
                {
                    var allComboItems = await _unitOfWork.OrderComboItems.GetByOrderDetailIdAsync(request.OrderDetailId);
                    if (allComboItems != null && allComboItems.Count > 0)
                    {
                        await UpdateParentOrderDetailStatusAsync(request.OrderDetailId, allComboItems);
                    }
                }

                return new StatusUpdateResponse
                {
                    Success = true,
                    Message = "Đã cập nhật trạng thái món trong combo",
                    UpdatedItem = new KitchenOrderItemDto
                    {
                        OrderDetailId = request.OrderDetailId,
                        OrderComboItemId = orderComboItem.OrderComboItemId,
                        MenuItemName = orderComboItem.MenuItem?.Name ?? "Unknown",
                        Quantity = orderComboItem.Quantity,
                        Status = orderComboItem.Status ?? "Pending",
                        Notes = orderComboItem.Notes,
                        CourseType = orderComboItem.MenuItem?.CourseType ?? "Other",
                        StartedAt = orderComboItem.StartedAt,
                        CompletedAt = orderComboItem.Status == "Done" ? DateTime.Now : null,
                        ReadyAt = orderComboItem.ReadyAt,
                        IsUrgent = orderComboItem.IsUrgent,
                        TimeCook = orderComboItem.MenuItem?.TimeCook ?? 0,
                        BatchSize = orderComboItem.MenuItem?.BatchSize ?? 0,
                        LateMinutes = null
                    }
                };
            }
            catch (Exception ex)
            {
                return new StatusUpdateResponse
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        private async Task UpdateParentOrderDetailStatusAsync(int orderDetailId, List<OrderComboItem> comboItems)
        {
            if (comboItems == null || comboItems.Count == 0)
            {
                return;
            }

            var parentDetail = await _unitOfWork.OrderDetails.GetByIdAsync(orderDetailId);
            if (parentDetail == null)
            {
                return;
            }

            var currentParentStatus = NormalizeStatus(parentDetail.Status ?? "Pending");
            // Không override khi waiter đã đánh dấu món đã phục vụ
            if (currentParentStatus == "Done")
            {
                await UpdateKitchenOrderStatusAsync(parentDetail.OrderId);
                return;
            }

            var childStatuses = comboItems
                .Select(ci => NormalizeStatus(ci.Status ?? "Pending"))
                .ToList();

            string newStatus;
            if (childStatuses.All(s => s == "Pending"))
            {
                newStatus = "Pending";
            }
            else if (childStatuses.All(s => s == "Ready" || s == "Done"))
            {
                newStatus = "Ready";
            }
            else if (childStatuses.Any(s => s == "Late"))
            {
                newStatus = "Late";
            }
            else
            {
                newStatus = "Cooking";
            }

            var hasChanges = false;
            if (currentParentStatus != newStatus)
            {
                parentDetail.Status = newStatus;
                hasChanges = true;
            }

            switch (newStatus)
            {
                case "Pending":
                    if (parentDetail.StartedAt != null || parentDetail.ReadyAt != null)
                    {
                        parentDetail.StartedAt = null;
                        parentDetail.ReadyAt = null;
                        hasChanges = true;
                    }
                    break;
                case "Cooking":
                case "Late":
                    if (!parentDetail.StartedAt.HasValue)
                    {
                        parentDetail.StartedAt = DateTime.Now;
                        hasChanges = true;
                    }
                    if (parentDetail.ReadyAt != null)
                    {
                        parentDetail.ReadyAt = null;
                        hasChanges = true;
                    }
                    break;
                case "Ready":
                    if (!parentDetail.StartedAt.HasValue)
                    {
                        parentDetail.StartedAt = DateTime.Now;
                        hasChanges = true;
                    }
                    if (!parentDetail.ReadyAt.HasValue)
                    {
                        parentDetail.ReadyAt = DateTime.Now;
                        hasChanges = true;
                    }
                    break;
            }

            if (hasChanges)
            {
                await _unitOfWork.OrderDetails.UpdateAsync(parentDetail);
                await _unitOfWork.SaveChangesAsync();
            }

            await UpdateKitchenOrderStatusAsync(parentDetail.OrderId);
        }

        private async Task UpdateKitchenOrderStatusAsync(int orderId)
        {
            if (orderId <= 0)
            {
                return;
            }

            var order = await _unitOfWork.Orders.GetByIdWithOrderDetailsAsync(orderId);
            if (order == null || order.OrderDetails == null || !order.OrderDetails.Any())
            {
                return;
            }

            var currentStatus = string.IsNullOrWhiteSpace(order.Status) ? "Pending" : order.Status;
            if (!KitchenManagedOrderStatuses.Contains(currentStatus))
            {
                return;
            }

            var detailStatuses = order.OrderDetails
                .Select(od => NormalizeStatus(od.Status ?? "Pending"))
                .ToList();

            var allPending = detailStatuses.All(s => s == "Pending");
            var anyLate = detailStatuses.Any(s => s == "Late");
            var allDone = detailStatuses.All(s => s == "Done");
            var allReadyOrDone = detailStatuses.All(s => s == "Ready" || s == "Done");
            var anyCooking = detailStatuses.Any(s => s == "Cooking");

            string newOrderStatus;
            if (allPending)
            {
                newOrderStatus = "Pending";
            }
            else if (anyLate)
            {
                newOrderStatus = "Late";
            }
            else if (allDone)
            {
                newOrderStatus = "Done";
            }
            else if (allReadyOrDone)
            {
                newOrderStatus = "Ready";
            }
            else if (anyCooking)
            {
                newOrderStatus = "Cooking";
            }
            else
            {
                newOrderStatus = "Cooking";
            }

            if (!string.Equals(currentStatus, newOrderStatus, StringComparison.OrdinalIgnoreCase))
            {
                order.Status = newOrderStatus;
                await _unitOfWork.Orders.UpdateAsync(order);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task<KitchenOrderCardDto?> GetOrderDetailsWithAllItemsAsync(int orderId)
        {
            var now = DateTime.Now;

            // Use GetByIdWithDetailsAsync to include OrderDetails and MenuItem
            var order = await _unitOfWork.Orders.GetByIdWithDetailsAsync(orderId);
            if (order == null) return null;

            // Get all order details including Done items
            var orderDetails = order.OrderDetails?.ToList() ?? new List<DomainAccessLayer.Models.OrderDetail>();
            if (!orderDetails.Any()) return null;

            // Map ALL OrderDetail to KitchenOrderItemDto (including Done items)
            // Bao gồm cả món lẻ và các món trong combo, chỉ lấy BillingType = 0 hoặc 2
            var items = new List<KitchenOrderItemDto>();

            foreach (var od in orderDetails)
            {
                var currentStatus = od.Status ?? "Pending";
                var (calculatedStatus, lateMinutes) = CalculateItemStatus(
                    currentStatus,
                    od.StartedAt,
                    od.MenuItem?.TimeCook ?? 0,
                    now);

                // 1) Món lẻ trực tiếp trên OrderDetail
                if (od.MenuItem != null &&
                    (od.MenuItem.BillingType == ItemBillingType.Unspecified ||
                     od.MenuItem.BillingType == ItemBillingType.KitchenPrepared))
                {
                    items.Add(new KitchenOrderItemDto
                    {
                        OrderDetailId = od.OrderDetailId,
                        MenuItemName = od.MenuItem?.Name ?? "Unknown",
                        Quantity = od.Quantity,
                        Status = calculatedStatus,
                        Notes = od.Notes,
                        CourseType = od.MenuItem?.CourseType ?? "Other",
                        StartedAt = od.StartedAt,
                        CompletedAt = od.Status == "Done" ? od.CreatedAt : null,
                        ReadyAt = od.ReadyAt,
                        IsUrgent = od.IsUrgent,
                        TimeCook = od.MenuItem?.TimeCook ?? 0,
                        BatchSize = od.MenuItem?.BatchSize ?? 0,
                        LateMinutes = lateMinutes
                    });
                }

                // 2) Các món trong combo:
                //    Ưu tiên OrderComboItems, fallback Combo.ComboItems nếu chưa có record chi tiết
                if (od.ComboId.HasValue)
                {
                    if (od.OrderComboItems != null && od.OrderComboItems.Any())
                    {
                        foreach (var orderComboItem in od.OrderComboItems)
                        {
                            var mi = orderComboItem.MenuItem;
                            if (mi == null) continue;

                            if (mi.BillingType != ItemBillingType.Unspecified &&
                                mi.BillingType != ItemBillingType.KitchenPrepared)
                            {
                                continue;
                            }

                            var comboItemStatus = orderComboItem.Status ?? "Pending";
                            var (comboCalculatedStatus, comboLateMinutes) = CalculateItemStatus(
                                comboItemStatus,
                                orderComboItem.StartedAt,
                                mi.TimeCook ?? 0,
                                now);

                            var itemQuantity = od.Quantity * orderComboItem.Quantity;

                            items.Add(new KitchenOrderItemDto
                            {
                                OrderDetailId = od.OrderDetailId,
                                OrderComboItemId = orderComboItem.OrderComboItemId,
                                MenuItemName = mi.Name,
                                Quantity = itemQuantity,
                                Status = comboCalculatedStatus,
                                Notes = orderComboItem.Notes ?? od.Notes,
                                CourseType = mi.CourseType ?? "Other",
                                StartedAt = orderComboItem.StartedAt,
                                CompletedAt = comboItemStatus == "Done" ? orderComboItem.CreatedAt : null,
                                ReadyAt = orderComboItem.ReadyAt,
                                IsUrgent = orderComboItem.IsUrgent || od.IsUrgent,
                                TimeCook = mi.TimeCook ?? 0,
                                BatchSize = mi.BatchSize ?? 0,
                                LateMinutes = comboLateMinutes
                            });
                        }
                    }
                    else if (od.Combo != null && od.Combo.ComboItems != null)
                    {
                        foreach (var comboItem in od.Combo.ComboItems)
                        {
                            var mi = comboItem.MenuItem;
                            if (mi == null) continue;

                            if (mi.BillingType != ItemBillingType.Unspecified &&
                                mi.BillingType != ItemBillingType.KitchenPrepared)
                            {
                                continue;
                            }

                            var itemQuantity = od.Quantity * comboItem.Quantity;

                            items.Add(new KitchenOrderItemDto
                            {
                                OrderDetailId = od.OrderDetailId,
                                MenuItemName = mi.Name,
                                Quantity = itemQuantity,
                                Status = calculatedStatus,
                                Notes = od.Notes,
                                CourseType = mi.CourseType ?? "Other",
                                StartedAt = od.StartedAt,
                                CompletedAt = od.Status == "Done" ? od.CreatedAt : null,
                                ReadyAt = od.ReadyAt,
                                IsUrgent = od.IsUrgent,
                                TimeCook = mi.TimeCook ?? 0,
                                BatchSize = mi.BatchSize ?? 0,
                                LateMinutes = lateMinutes
                            });
                        }
                    }
                }
            }

            // Sort items by course type
            items = SortItemsByCourseType(items);

            var waitingMinutes = (int)((now - (order.CreatedAt ?? now)).TotalMinutes);
            var lateCount = items.Count(i => i.Status == "Late");
            var readyCount = items.Count(i => i.Status == "Ready");
            var doneCount = items.Count(i => 
                (i.Status ?? "").ToLower().Contains("done") || 
                (i.Status ?? "").ToLower().Contains("hoàn thành"));

            var card = new KitchenOrderCardDto
            {
                OrderId = order.OrderId,
                OrderNumber = $"A{order.OrderId:D2}",
                TableNumber = GetTableNumber(order),
                NumberOfGuests = GetNumberOfGuests(order),
                CreatedAt = order.CreatedAt ?? DateTime.Now,
                WaitingMinutes = waitingMinutes,
                PriorityLevel = GetPriorityLevel(waitingMinutes),
                TotalItems = items.Count,
                CompletedItems = readyCount,
                LateItems = lateCount,
                ReadyItems = readyCount,
                Items = items
            };

            return card;
        }

        /// <summary>
        /// Lấy thông tin order detail để in ticket khi hoàn thành món
        /// </summary>
        public async Task<PrintItemTicketDto?> GetOrderDetailForPrintAsync(int orderDetailId, int? orderComboItemId)
        {
            try
            {
                // Nếu có orderComboItemId, lấy từ OrderComboItem
                if (orderComboItemId.HasValue)
                {
                    var orderComboItem = await _unitOfWork.OrderComboItems.GetByIdWithMenuItemAsync(orderComboItemId.Value);
                    if (orderComboItem == null) return null;

                    // Load OrderDetail với Order và Reservation
                    var orderDetail = await _unitOfWork.OrderDetails.GetByIdWithMenuItemAsync(orderComboItem.OrderDetailId);
                    if (orderDetail == null) return null;

                    var order = await _unitOfWork.Orders.GetByIdWithDetailsAsync(orderDetail.OrderId);
                    if (order == null) return null;

                    return new PrintItemTicketDto
                    {
                        OrderId = order.OrderId,
                        OrderNumber = $"A{order.OrderId:D2}",
                        TableNumber = GetTableNumber(order),
                        MenuItemName = orderComboItem.MenuItem?.Name ?? "Unknown",
                        Quantity = orderDetail.Quantity * orderComboItem.Quantity,
                        Notes = orderComboItem.Notes ?? orderDetail.Notes,
                        CompletedAt = DateTime.Now,
                        StationName = orderComboItem.MenuItem?.Category?.CategoryName 
                            ?? orderComboItem.MenuItem?.CourseType 
                            ?? "N/A"
                    };
                }
                else
                {
                    // Món lẻ
                    var orderDetail = await _unitOfWork.OrderDetails.GetByIdWithMenuItemAsync(orderDetailId);
                    if (orderDetail == null) return null;

                    var order = await _unitOfWork.Orders.GetByIdWithDetailsAsync(orderDetail.OrderId);
                    if (order == null) return null;

                    return new PrintItemTicketDto
                    {
                        OrderId = order.OrderId,
                        OrderNumber = $"A{order.OrderId:D2}",
                        TableNumber = GetTableNumber(order),
                        MenuItemName = orderDetail.MenuItem?.Name ?? "Unknown",
                        Quantity = orderDetail.Quantity,
                        Notes = orderDetail.Notes,
                        CompletedAt = DateTime.Now,
                        StationName = orderDetail.MenuItem?.Category?.CategoryName 
                            ?? orderDetail.MenuItem?.CourseType 
                            ?? "N/A"
                    };
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Broadcast đơn mới đến tất cả màn hình bếp qua SignalR
        /// Method này sẽ được gọi từ Controller với IHubContext<KitchenHub>
        /// </summary>
        public async Task NotifyNewOrderAddedAsync(KitchenOrderCardDto order)
        {
            // Method này chỉ là placeholder
            // Thực sự broadcast sẽ được thực hiện từ Controller layer với IHubContext<KitchenHub>
            // Để tránh dependency từ BusinessAccessLayer đến API layer
            await Task.CompletedTask;
        }

        /// <summary>
        /// Batch start cooking/update status cho nhiều món trong một lần để giảm số lượng call từ KDS
        /// </summary>
        public async Task<BatchCookResponse> BatchStartCookingAsync(BatchCookRequest request)
        {
            var response = new BatchCookResponse();

            foreach (var item in request.Items)
            {
                var result = new BatchCookItemResult
                {
                    OrderDetailId = item.OrderDetailId,
                    OrderComboItemId = item.OrderComboItemId
                };

                try
                {
                    // Nếu có OrderComboItemId → update status trực tiếp sang Cooking
                    if (item.OrderComboItemId.HasValue && item.OrderComboItemId.Value > 0)
                    {
                        var updateResp = await UpdateItemStatusAsync(new UpdateItemStatusRequest
                        {
                            OrderDetailId = item.OrderDetailId,
                            OrderComboItemId = item.OrderComboItemId,
                            NewStatus = "Cooking",
                            UserId = request.UserId
                        });

                        result.Success = updateResp.Success;
                        result.Message = updateResp.Message;
                    }
                    else
                    {
                        // Món lẻ: sử dụng luồng start-cooking-with-quantity để xử lý split nếu cần
                        var cookResp = await StartCookingWithQuantityAsync(new StartCookingWithQuantityRequest
                        {
                            OrderDetailId = item.OrderDetailId,
                            Quantity = item.Quantity > 0 ? item.Quantity : 1,
                            UserId = request.UserId
                        });

                        result.Success = cookResp.Success;
                        result.Message = cookResp.Message;
                    }
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Message = ex.Message;
                }

                response.Items.Add(result);
            }

            // Tổng hợp kết quả
            response.Success = response.Items.All(i => i.Success);
            response.Message = response.Success
                ? "Batch cooking thành công"
                : "Một số món không thể bắt đầu nấu";

            return response;
        }
    }
}