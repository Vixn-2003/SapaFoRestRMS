using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.UnitOfWork.Interfaces;
using BusinessAccessLayer.DTOs.Kitchen;
using DomainAccessLayer.Models;
using BusinessAccessLayer.Services.Interfaces;

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

                // ✅ HIỂN THỊ TẤT CẢ: Bao gồm cả Ready và Done
                // Map OrderDetail to KitchenOrderItemDto với tính toán trạng thái
                var items = orderDetails
                    .Select(od =>
                    {
                        var currentStatus = od.Status ?? "Pending";
                        var (calculatedStatus, lateMinutes) = CalculateItemStatus(
                            currentStatus,
                            od.StartedAt,
                            od.MenuItem.TimeCook,
                            now);

                        return new KitchenOrderItemDto
                        {
                            OrderDetailId = od.OrderDetailId,
                            MenuItemName = od.MenuItem.Name,
                            Quantity = od.Quantity,
                            Status = calculatedStatus, // Sử dụng trạng thái đã tính toán
                            Notes = od.Notes,
                            CourseType = od.MenuItem.CourseType ?? "Other",
                            StartedAt = od.StartedAt,
                            CompletedAt = od.Status == "Done" ? od.CreatedAt : null,
                            ReadyAt = od.ReadyAt,
                            IsUrgent = od.IsUrgent,
                            TimeCook = od.MenuItem.TimeCook, // Thời gian nấu (phút)
                            BatchSize = od.MenuItem.BatchSize,
                            LateMinutes = lateMinutes
                        };
                    })
                    .ToList();

                // ✅ THÊM: Filter by status nếu có
                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    items = items.Where(i => i.Status == statusFilter).ToList();
                }

                // ✅ THÊM: Sort items by course type (Khai vị -> Món chính -> Tráng miệng)
                items = SortItemsByCourseType(items);

                // ✅ SỬA: Chỉ bỏ qua order nếu không có items nào (kể cả Done)
                if (!items.Any())
                {
                    continue;
                }

                var waitingMinutes = (int)((now - (order.CreatedAt ?? now)).TotalMinutes);

                // ✅ Đếm các trạng thái (bao gồm cả Done)
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
                    CompletedItems = readyCount + doneCount, // ✅ SỬA: Ready + Done = Completed
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

                var orderDetail = await _unitOfWork.OrderDetails.GetByIdWithMenuItemAsync(request.OrderDetailId);

                if (orderDetail == null)
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "Item not found"
                    };
                }

                // Validate status transition: Pending → Cooking → Done
                var currentStatus = (orderDetail.Status ?? "Pending").Trim();
                var newStatus = request.NewStatus.Trim();

                // Normalize status for comparison (handle both English and Vietnamese)
                var normalizedCurrentStatus = NormalizeStatus(currentStatus);
                var normalizedNewStatus = NormalizeStatus(newStatus);
                
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
                    // Lưu thời gian bắt đầu nấu
                    orderDetail.StartedAt = DateTime.Now;
                    
                    // Bếp phó duyệt → Tăng QuantityReserved của các batch cần dùng
                    var reserveResult = await _inventoryService.ReserveBatchesForOrderDetailAsync(request.OrderDetailId);
                    if (!reserveResult.success)
                    {
                        return new StatusUpdateResponse
                        {
                            Success = false,
                            Message = reserveResult.message
                        };
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
                    // Nếu chuyển sang Ready, lưu thời gian
                    if (normalizedNewStatus == "Ready")
                    {
                        orderDetail.ReadyAt = DateTime.Now;
                    }
                    // Nếu chuyển sang Done: Nấu xong → Trừ QuantityRemaining + Giảm QuantityReserved + Tạo StockTransaction
                    else if (normalizedNewStatus == "Done")
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
                }
                else if (normalizedCurrentStatus == "Ready")
                {
                    // From Ready, allow transition to Done or back to Cooking (hủy sẵn sàng)
                    if (normalizedNewStatus == "Done")
                    {
                        // Nấu xong → Trừ QuantityRemaining + Giảm QuantityReserved + Tạo StockTransaction
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
                    else if (normalizedNewStatus == "Cooking")
                    {
                        // Hủy sẵn sàng → Quay lại Cooking, reset ReadyAt
                        orderDetail.ReadyAt = null;
                        // Giữ nguyên StartedAt để tiếp tục đếm thời gian nấu
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
                    orderDetail.StartedAt = DateTime.Now;
                    orderDetail.ReadyAt = null;
                }

                // Update status trên OrderDetail (nguồn chính) - luôn lưu bằng tiếng Anh
                orderDetail.Status = normalizedNewStatus;

                await _unitOfWork.OrderDetails.UpdateAsync(orderDetail);
                await _unitOfWork.SaveChangesAsync();

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

                if (cookingQuantity <= 0 || cookingQuantity > totalQuantity)
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = $"Số lượng nấu ({cookingQuantity}) phải lớn hơn 0 và không vượt quá số lượng đơn ({totalQuantity})"
                    };
                }

                // Nếu số lượng nấu = tổng số lượng, chỉ cần update status
                if (cookingQuantity == totalQuantity)
                {
                    orderDetail.Status = "Cooking";
                    orderDetail.StartedAt = DateTime.Now;
                    
                    // Reserve inventory
                    var reserveResult = await _inventoryService.ReserveBatchesForOrderDetailAsync(request.OrderDetailId);
                    if (!reserveResult.success)
                    {
                        return new StatusUpdateResponse
                        {
                            Success = false,
                            Message = reserveResult.message
                        };
                    }

                    await _unitOfWork.OrderDetails.UpdateAsync(orderDetail);
                    await _unitOfWork.SaveChangesAsync();

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
                // Tạo order detail mới với số lượng đã chọn, status = Cooking
                var newOrderDetail = new OrderDetail
                {
                    OrderId = orderDetail.OrderId,
                    MenuItemId = orderDetail.MenuItemId,
                    ComboId = orderDetail.ComboId,
                    Quantity = cookingQuantity,
                    UnitPrice = orderDetail.UnitPrice,
                    Status = "Cooking",
                    Notes = orderDetail.Notes,
                    IsUrgent = orderDetail.IsUrgent,
                    StartedAt = DateTime.Now,
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.OrderDetails.AddAsync(newOrderDetail);
                await _unitOfWork.SaveChangesAsync();

                // Reserve inventory cho order detail mới
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

                // Giảm số lượng của order detail gốc (vẫn giữ status Pending)
                orderDetail.Quantity = totalQuantity - cookingQuantity;
                await _unitOfWork.OrderDetails.UpdateAsync(orderDetail);
                await _unitOfWork.SaveChangesAsync();

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
                var order = await _unitOfWork.Orders.GetByIdWithOrderDetailsAsync(request.OrderId);

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

                // Check if all items are done
                var allDone = order.OrderDetails.All(od => od.Status == "Done");
                if (!allDone)
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "Not all items are completed yet"
                    };
                }

                // Update order status
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
            var allItems = new List<(Order Order, OrderDetail OrderDetail, MenuItem MenuItem)>();

            foreach (var order in activeOrders)
            {
                foreach (var orderDetail in order.OrderDetails)
                {
                    // ✅ HIỂN THỊ TẤT CẢ: Bao gồm cả Ready và Done
                    var status = (orderDetail.Status ?? "Pending").Trim();
                    
                    // ✅ THÊM: Filter by status nếu có
                    if (!string.IsNullOrWhiteSpace(statusFilter) && status != statusFilter)
                    {
                        continue;
                    }
                    
                    // ✅ Lấy tất cả các status (Pending, Cooking, Late, Ready, Done)
                    if (orderDetail.MenuItem != null)
                    {
                        allItems.Add((order, orderDetail, orderDetail.MenuItem));
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
                        var itemStatus = (item.OrderDetail.Status ?? "Pending").Trim();
                        var normalizedStatus = NormalizeStatus(itemStatus);
                        return normalizedStatus == "Pending";
                    }).Sum(item => item.OrderDetail.Quantity),
                    ItemDetails = g.Select(item => new GroupedItemDetailDto
                    {
                        OrderDetailId = item.OrderDetail.OrderDetailId,
                        OrderId = item.Order.OrderId,
                        OrderNumber = $"A{item.Order.OrderId:D2}",
                        TableNumber = GetTableNumber(item.Order),
                        Quantity = item.OrderDetail.Quantity,
                        Status = item.OrderDetail.Status ?? "Pending", // Default to Pending if null
                        Notes = item.OrderDetail.Notes,
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
            
            Console.WriteLine($"[GetStationItemsByCategoryAsync] Received categoryName (after decode): '{categoryName}'");
            Console.WriteLine($"[GetStationItemsByCategoryAsync] CategoryName length: {categoryName.Length}");
            
            // Lấy tất cả active orders với order details thuộc category này
            var activeOrders = await _unitOfWork.Orders.GetActiveOrdersForStationAsync();

            var allItems = new List<StationItemDto>();
            var urgentItems = new List<StationItemDto>();

            // Debug: Log số lượng orders
            Console.WriteLine($"[GetStationItemsByCategoryAsync] Found {activeOrders.Count} active orders");
            Console.WriteLine($"[GetStationItemsByCategoryAsync] Filtering by categoryName: '{categoryName}'");
            
            // Log tổng số order details trước khi filter
            var totalOrderDetails = activeOrders.Sum(o => o.OrderDetails.Count);
            Console.WriteLine($"[GetStationItemsByCategoryAsync] Total order details before filter: {totalOrderDetails}");
            
            // Log tất cả categories có trong database để debug
            var allCategories = await _unitOfWork.MenuCategory.GetCategoryNamesAsync();
            Console.WriteLine($"[GetStationItemsByCategoryAsync] All categories in DB: {string.Join(", ", allCategories)}");

            foreach (var order in activeOrders)
            {
                // Lọc order details theo category name - sử dụng Trim và case-insensitive
                var orderDetails = order.OrderDetails
                    .Where(od => od.MenuItem != null && 
                                 od.MenuItem.Category != null)
                    .Where(od => {
                        var catName = od.MenuItem.Category.CategoryName?.Trim() ?? string.Empty;
                        return catName.Equals(categoryName, StringComparison.OrdinalIgnoreCase);
                    })
                    .ToList();
                
                // Debug: Log số lượng order details
                if (orderDetails.Any())
                {
                    Console.WriteLine($"[GetStationItemsByCategoryAsync] Order {order.OrderId} has {orderDetails.Count} items in category {categoryName}");
                }
                else
                {
                    // Debug: Log để kiểm tra tại sao không có items
                    var allOrderDetails = order.OrderDetails.ToList();
                    Console.WriteLine($"[GetStationItemsByCategoryAsync] Order {order.OrderId} has {allOrderDetails.Count} total items");
                    foreach (var od in allOrderDetails)
                    {
                        if (od.MenuItem != null)
                        {
                            if (od.MenuItem.Category != null)
                            {
                                var itemCatName = od.MenuItem.Category.CategoryName?.Trim() ?? "NULL";
                                var matches = itemCatName.Equals(categoryName, StringComparison.OrdinalIgnoreCase);
                                Console.WriteLine($"[GetStationItemsByCategoryAsync]   - Item: {od.MenuItem.Name}, Category: '{itemCatName}' (Match: {matches})");
                            }
                            else
                            {
                                Console.WriteLine($"[GetStationItemsByCategoryAsync]   - Item: {od.MenuItem.Name}, CategoryId: {od.MenuItem.CategoryId}, Category: NULL (not loaded)");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[GetStationItemsByCategoryAsync]   - Item: NULL, MenuItemId: {od.MenuItemId}");
                        }
                    }
                }

                foreach (var orderDetail in orderDetails)
                {
                    // ✅ BỎ: Filter Done items - Bếp không cần nhìn Done
                    var status = (orderDetail.Status ?? "Pending").Trim();
                    if (status == "Done" || status == "Hoàn thành" || status == "Xong")
                    {
                        continue; // Bỏ qua Done items
                    }
                    
                    var waitingMinutes = (int)((now - (order.CreatedAt ?? now)).TotalMinutes);
                    var createdAtTime = (order.CreatedAt ?? DateTime.Now).ToString("HH:mm");
                    
                    // Hiển thị thời gian hiện tại khi status = "Cooking" (thời gian bắt đầu nấu)
                    // Không lưu StartedAt, chỉ hiển thị real-time
                    var fireTime = string.Empty;
                    DateTime? startedAt = null;
                    
                    // Nếu status = "Cooking", lấy StartedAt (thời gian bắt đầu nấu)
                    if (status == "Cooking" || status == "Đang chế biến")
                    {
                        // Dùng StartedAt nếu có, nếu không thì dùng CreatedAt
                        startedAt = orderDetail.StartedAt ?? orderDetail.CreatedAt;
                        fireTime = startedAt?.ToString("HH:mm") ?? orderDetail.CreatedAt.ToString("HH:mm");
                    }

                    var item = new StationItemDto
                    {
                        OrderDetailId = orderDetail.OrderDetailId,
                        OrderId = order.OrderId,
                        OrderNumber = $"A{order.OrderId:D2}",
                        TableNumber = GetTableNumber(order),
                        MenuItemName = orderDetail.MenuItem.Name,
                        Quantity = orderDetail.Quantity,
                        Status = status, // Lấy từ OrderDetail
                        Notes = orderDetail.Notes,
                        CreatedAt = order.CreatedAt ?? DateTime.Now,
                        CreatedAtTime = createdAtTime,
                        WaitingMinutes = waitingMinutes,
                        IsUrgent = orderDetail.IsUrgent,
                        StartedAt = startedAt, // Thời gian bắt đầu nấu (dùng để đếm ngược)
                        FireTime = fireTime,
                        TimeCook = orderDetail.MenuItem?.TimeCook ?? 0, // Thời gian nấu (phút)
                        BatchSize = orderDetail.MenuItem.BatchSize
                    };

                    allItems.Add(item);

                    // Thêm vào urgent items nếu được đánh dấu urgent
                    if (orderDetail.IsUrgent)
                    {
                        urgentItems.Add(item);
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

            // Debug: Log kết quả
            Console.WriteLine($"[GetStationItemsByCategoryAsync] Total items found: {allItems.Count}");
            Console.WriteLine($"[GetStationItemsByCategoryAsync] Items by status:");
            var statusGroups = allItems.GroupBy(i => i.Status ?? "NULL");
            foreach (var group in statusGroups)
            {
                Console.WriteLine($"[GetStationItemsByCategoryAsync]   - {group.Key}: {group.Count()} items");
            }

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
            // Lấy các orders có ít nhất một item Done
            // Không filter theo thời gian vì không có CompletedAt field
            // Chỉ lấy các order đang active hoặc completed (không lấy orders quá cũ đã thanh toán)
            var orders = await _unitOfWork.Orders.GetRecentlyFulfilledOrdersAsync(minutesAgo);

            var result = new List<KitchenOrderCardDto>();
            var now = DateTime.Now;

            foreach (var order in orders)
            {
                // Lấy tất cả items Done trong order này
                var doneItems = order.OrderDetails
                    .Where(od => od.Status == "Done" || od.Status == "Hoàn thành")
                    .ToList();

                if (!doneItems.Any()) continue;

                // Chỉ hiển thị order nếu TẤT CẢ các món đều hoàn thành
                var allItemsDone = order.OrderDetails.All(od =>
                {
                    var status = (od.Status ?? string.Empty).Trim();
                    return status.Equals("Done", StringComparison.OrdinalIgnoreCase) ||
                           status.Equals("Hoàn thành", StringComparison.OrdinalIgnoreCase) ||
                           status.Equals("Xong", StringComparison.OrdinalIgnoreCase);
                });

                if (!allItemsDone)
                {
                    // Nếu còn món chưa hoàn thành, bỏ qua order này
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
                    Items = doneItems.Select(od => new KitchenOrderItemDto
                    {
                        OrderDetailId = od.OrderDetailId,
                        MenuItemName = od.MenuItem.Name,
                        Quantity = od.Quantity,
                        Status = od.Status ?? "Done",
                        Notes = od.Notes,
                        CourseType = od.MenuItem.CourseType ?? "Other",
                        IsUrgent = od.IsUrgent,
                        CompletedAt = od.CreatedAt, // Dùng CreatedAt làm proxy (không chính xác 100%)
                        TimeCook = od.MenuItem.TimeCook, // Thời gian nấu (phút)
                        BatchSize = od.MenuItem.BatchSize
                    }).ToList()
                };

                result.Add(orderCard);
            }

            return result;
        }

        /// <summary>
        /// Khôi phục (Recall) một order detail đã Done, đưa nó quay lại trạng thái Processing
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

                if (orderDetail.Status != "Done" && orderDetail.Status != "Hoàn thành")
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "Món ăn này chưa được đánh dấu hoàn thành, không thể khôi phục"
                    };
                }

                // Khôi phục về trạng thái "Pending"
                orderDetail.Status = "Pending";

                // Đảm bảo Order status là Processing hoặc Preparing
                if (orderDetail.OrderId != null)
                {
                    var order = await _unitOfWork.Orders.GetByIdWithOrderDetailsAsync(orderDetail.OrderId);
                    if (order != null && order.Status == "Completed")
                    {
                        order.Status = "Processing";
                        await _unitOfWork.Orders.UpdateAsync(order);
                    }
                }

                await _unitOfWork.OrderDetails.UpdateAsync(orderDetail);
                await _unitOfWork.SaveChangesAsync();

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
            var items = orderDetails
                .Select(od =>
                {
                    var currentStatus = od.Status ?? "Pending";
                    var (calculatedStatus, lateMinutes) = CalculateItemStatus(
                        currentStatus,
                        od.StartedAt,
                        od.MenuItem?.TimeCook ?? 0,
                        now);

                    return new KitchenOrderItemDto
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
                    };
                })
                .ToList();

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
    }
}