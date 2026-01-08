using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using BusinessAccessLayer.DTOs.Waiter;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace BusinessAccessLayer.Services
{
    public class WaiterOrderTrackingService : IWaiterOrderTrackingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IInventoryIngredientService _inventoryService;

        public WaiterOrderTrackingService(IUnitOfWork unitOfWork, IInventoryIngredientService inventoryService)
        {
            _unitOfWork = unitOfWork;
            _inventoryService = inventoryService;
        }

        public async Task<WaiterOrderTrackingDto> GetOrderTrackingAsync(int? waiterUserId = null, List<int>? tableIds = null)
        {
            var now = DateTime.Now;
            var result = new WaiterOrderTrackingDto();

            // Lấy tất cả active orders với order details
            var allActiveOrders = await _unitOfWork.Orders.GetActiveOrdersAsync();
            
            // Filter theo tableIds nếu có
            List<Order> activeOrders;
            if (tableIds != null && tableIds.Any())
            {
                activeOrders = allActiveOrders.Where(order =>
                {
                    // Lấy tableIds từ reservation
                    var reservationTableIds = order.Reservation?.ReservationTables?
                        .Select(rt => rt.TableId)
                        .ToList() ?? new List<int>();
                    
                    // Kiểm tra xem có bàn nào trong danh sách filter không
                    return reservationTableIds.Any(tableId => tableIds.Contains(tableId));
                }).ToList();
            }
            else
            {
                activeOrders = allActiveOrders;
            }

            var allItems = new List<OrderTrackingItemDto>();
            var orderGroups = new Dictionary<int, OrderTrackingGroupDto>();

            foreach (var order in activeOrders)
            {
                // Lấy thông tin bàn/khu vực
                var tableNumber = GetTableNumber(order);
                var areaName = GetAreaName(order);
                var orderNumber = $"A{order.OrderId:D2}";
                var numberOfGuests = order.Reservation?.NumberOfGuests ?? 0;

                // Tạo group nếu chưa có
                if (!orderGroups.ContainsKey(order.OrderId))
                {
                    orderGroups[order.OrderId] = new OrderTrackingGroupDto
                    {
                        OrderNumber = orderNumber,
                        AreaName = areaName,
                        TableNumber = tableNumber,
                        NumberOfGuests = numberOfGuests
                    };
                }

                var group = orderGroups[order.OrderId];

                // Xử lý từng order detail
                foreach (var orderDetail in order.OrderDetails)
                {
                    var status = (orderDetail.Status ?? "Pending").Trim();
                    var statusLower = status.ToLower();
                    
                    // Chỉ bỏ qua Cancelled và Returned, giữ lại Done để hiển thị
                    if (statusLower.Contains("cancelled") || statusLower.Contains("hủy") ||
                        statusLower.Contains("returned") || statusLower.Contains("trả"))
                    {
                        continue;
                    }

                    var waitingMinutes = (int)((now - (orderDetail.CreatedAt)).TotalMinutes);
                    var isConsumptionBased = orderDetail.MenuItem?.BillingType == DomainAccessLayer.Enums.ItemBillingType.ConsumptionBased;
                    var canCancel = status == "Pending" || status == "Chờ bếp xác nhận" || status == "Chờ";
                    var canRequestUrgent = status == "Pending" || status == "Chờ bếp xác nhận" || status == "Chờ" || 
                                          status == "Cooking" || status == "Đang nấu";
                    var isDone = statusLower.Contains("done") || statusLower.Contains("hoàn thành") || statusLower.Contains("xong");
                    
                    //  Món ConsumptionBased có thể xác nhận số lượng ngay (không cần chờ Ready)
                    // Món KitchenPrepared chỉ có thể xác nhận khi đã Ready
                    // Đảm bảo chỉ set true khi thực sự là ConsumptionBased
                    var canConfirmQuantity = isConsumptionBased && !isDone;
                    
                    // Kiểm tra xem order detail này có phải đã được tách từ order detail gốc không
                    // Nếu có StartedAt và CreatedAt gần nhau (trong vòng 5 phút), có thể đã được tách
                    // Hoặc kiểm tra xem có order detail khác cùng OrderId, MenuItemId, Notes nhưng có Quantity khác không
                    var isSplit = false;
                    if (orderDetail.StartedAt.HasValue)
                    {
                        var timeDiff = Math.Abs((orderDetail.StartedAt.Value - orderDetail.CreatedAt).TotalMinutes);
                        // Nếu StartedAt và CreatedAt gần nhau (trong vòng 5 phút), có thể đã được tách
                        if (timeDiff <= 5)
                        {
                            // Kiểm tra xem có order detail khác cùng OrderId, MenuItemId, Notes không
                            var hasOtherDetails = order.OrderDetails.Any(od => 
                                od.OrderDetailId != orderDetail.OrderDetailId &&
                                od.MenuItemId == orderDetail.MenuItemId &&
                                od.Notes == orderDetail.Notes &&
                                od.ComboId == orderDetail.ComboId);
                            if (hasOtherDetails)
                            {
                                isSplit = true;
                            }
                        }
                    }

                    //  Nếu là combo và đã có OrderComboItems → sổ ra từng món trong combo
                    if (orderDetail.ComboId.HasValue &&
                        orderDetail.OrderComboItems != null &&
                        orderDetail.OrderComboItems.Any())
                    {
                        foreach (var orderComboItem in orderDetail.OrderComboItems)
                        {
                            var mi = orderComboItem.MenuItem ?? orderDetail.MenuItem;
                            var menuItemName = mi?.Name
                                ?? orderDetail.Combo?.Name
                                ?? "Combo item";

                            var itemQuantity = orderDetail.Quantity * orderComboItem.Quantity;

                            // Trạng thái riêng theo từng món con
                            var comboStatus = (orderComboItem.Status ?? status).Trim();
                            var comboStatusLower = comboStatus.ToLower();
                            var normalizedComboStatus = NormalizeStatus(comboStatus);
                            var comboIsDone = comboStatusLower.Contains("done") ||
                                              comboStatusLower.Contains("hoàn thành") ||
                                              comboStatusLower.Contains("xong");

                            // Quyền Hủy / Làm gấp theo trạng thái món con
                            var canCancelItem = normalizedComboStatus == "Pending";
                            var canRequestUrgentItem = normalizedComboStatus != "Done";
                            
                            //  Món ConsumptionBased có thể xác nhận số lượng ngay (không cần chờ Ready)
                            var isComboItemConsumptionBased = mi?.BillingType == DomainAccessLayer.Enums.ItemBillingType.ConsumptionBased;
                            var canConfirmComboQuantity = isComboItemConsumptionBased && !comboIsDone;

                            var comboItem = new OrderTrackingItemDto
                            {
                                OrderDetailId = orderDetail.OrderDetailId,
                                OrderComboItemId = orderComboItem.OrderComboItemId,
                                OrderId = order.OrderId,
                                MenuItemName = menuItemName,
                                Quantity = itemQuantity,
                                Status = comboStatus,
                                Notes = orderComboItem.Notes ?? orderDetail.Notes,
                                IsUrgent = orderComboItem.IsUrgent || orderDetail.IsUrgent,
                                OrderTime = orderDetail.CreatedAt,
                                WaitingMinutes = waitingMinutes,
                                StartedAt = orderComboItem.StartedAt ?? orderDetail.StartedAt,
                                ReadyAt = orderComboItem.ReadyAt ?? orderDetail.ReadyAt,
                                ServedAt = comboIsDone ? (orderComboItem.ReadyAt ?? orderDetail.ReadyAt ?? orderDetail.CreatedAt) : null,
                                CanCancel = canCancelItem,
                                CanReturn = false,
                                CanRequestUrgent = canRequestUrgent && canRequestUrgentItem,
                                IsSplit = isSplit,
                                BillingType = mi != null ? (int)mi.BillingType : null,
                                QuantityUsed = orderDetail.QuantityUsed,
                                CanConfirmQuantity = canConfirmComboQuantity
                            };

                            allItems.Add(comboItem);
                            group.Items.Add(comboItem);
                        }
                    }
                    else
                    {
                        // Món lẻ (không phải combo) → giữ nguyên logic cũ
                        var item = new OrderTrackingItemDto
                        {
                            OrderDetailId = orderDetail.OrderDetailId,
                            OrderId = order.OrderId,
                            MenuItemName = orderDetail.MenuItem?.Name ?? orderDetail.Combo?.Name ?? "N/A",
                            Quantity = orderDetail.Quantity,
                            Status = status,
                            Notes = orderDetail.Notes,
                            IsUrgent = orderDetail.IsUrgent,
                            OrderTime = orderDetail.CreatedAt,
                            WaitingMinutes = waitingMinutes,
                            StartedAt = orderDetail.StartedAt,
                            ReadyAt = orderDetail.ReadyAt,
                            ServedAt = isDone ? (orderDetail.ReadyAt ?? orderDetail.CreatedAt) : null, // Nếu Done, dùng ReadyAt hoặc CreatedAt
                            CanCancel = canCancel,
                            CanReturn = false,
                            CanRequestUrgent = canRequestUrgent,
                            IsSplit = isSplit,
                            BillingType = orderDetail.MenuItem != null ? (int)orderDetail.MenuItem.BillingType : null,
                            QuantityUsed = orderDetail.QuantityUsed,
                            CanConfirmQuantity = canConfirmQuantity
                        };

                        allItems.Add(item);
                        group.Items.Add(item);
                    }
                }
            }

            // Tính lại counters dựa trên từng item (bao gồm món lẻ và từng món trong combo)
            result.TotalCount = allItems.Count;

            foreach (var item in allItems)
            {
                var normalizedStatus = NormalizeStatus(item.Status ?? "Pending");

                switch (normalizedStatus)
                {
                    case "Pending":
                        result.WaitingKitchenCount++;
                        result.ProcessingCount++;
                        break;
                    case "Cooking":
                    case "Late":
                        result.CookingCount++;
                        result.ProcessingCount++;
                        break;
                    case "Ready":
                        result.ReadyCount++;
                        result.ProcessingCount++;
                        break;
                    // Done/Cancelled/Returned không cộng vào ProcessingCount
                }
            }

            result.OrderGroups = orderGroups.Values.ToList();

            return result;
        }

        public async Task<RequestUrgentResponse> RequestUrgentAsync(RequestUrgentDto request)
        {
            try
            {
                var orderDetail = await _unitOfWork.OrderDetails.GetByIdAsync(request.OrderDetailId);
                if (orderDetail == null)
                {
                    return new RequestUrgentResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy món ăn"
                    };
                }

                // Nếu có OrderComboItemId → đánh dấu urgent cho đúng món con trong combo
                if (request.OrderComboItemId.HasValue && request.OrderComboItemId.Value > 0)
                {
                    var orderComboItem = await _unitOfWork.OrderComboItems.GetByIdAsync(request.OrderComboItemId.Value);
                    if (orderComboItem == null)
                    {
                        return new RequestUrgentResponse
                        {
                            Success = false,
                            Message = "Không tìm thấy món trong combo"
                        };
                    }

                    var statusCombo = (orderComboItem.Status ?? "Pending").Trim();
                    var normalizedComboStatus = NormalizeStatus(statusCombo);
                    if (normalizedComboStatus == "Done")
                    {
                        return new RequestUrgentResponse
                        {
                            Success = false,
                            Message = "Món trong combo đã hoàn thành, không thể yêu cầu làm gấp"
                        };
                    }

                    // Toggle urgent: nếu đang urgent thì hủy, nếu chưa thì bật
                    if (orderComboItem.IsUrgent)
                    {
                        orderComboItem.IsUrgent = false;
                        // Xóa các tag [LÀM GẤP: ...] khỏi ghi chú
                        orderComboItem.Notes = CleanUrgentNotes(orderComboItem.Notes);
                    }
                    else
                    {
                        orderComboItem.IsUrgent = true;
                        var baseNotes = CleanUrgentNotes(orderComboItem.Notes) ?? string.Empty;
                        if (!string.IsNullOrEmpty(request.Reason))
                        {
                            orderComboItem.Notes = $"{baseNotes} [LÀM GẤP: {request.Reason}]".Trim();
                        }
                        else
                        {
                            orderComboItem.Notes = baseNotes;
                        }
                    }

                    await _unitOfWork.OrderComboItems.UpdateAsync(orderComboItem);
                    await _unitOfWork.SaveChangesAsync();
                }
                else
                {
                    // Món lẻ hoặc combo nhưng chưa chọn món con cụ thể → đánh dấu urgent cấp OrderDetail
                    var status = (orderDetail.Status ?? "Pending").Trim();
                    var normalizedStatus = NormalizeStatus(status);
                    if (normalizedStatus == "Done")
                    {
                        return new RequestUrgentResponse
                        {
                            Success = false,
                            Message = "Món đã hoàn thành, không thể yêu cầu làm gấp"
                        };
                    }

                    // Toggle urgent cho món lẻ / dòng combo
                    if (orderDetail.IsUrgent)
                    {
                        orderDetail.IsUrgent = false;
                        orderDetail.Notes = CleanUrgentNotes(orderDetail.Notes);
                    }
                    else
                    {
                        orderDetail.IsUrgent = true;
                        var baseNotes = CleanUrgentNotes(orderDetail.Notes) ?? string.Empty;
                        if (!string.IsNullOrEmpty(request.Reason))
                        {
                            orderDetail.Notes = $"{baseNotes} [LÀM GẤP: {request.Reason}]".Trim();
                        }
                        else
                        {
                            orderDetail.Notes = baseNotes;
                        }
                    }

                    await _unitOfWork.OrderDetails.UpdateAsync(orderDetail);
                    await _unitOfWork.SaveChangesAsync();
                }

                // TODO: Gửi SignalR notification cho bếp

                return new RequestUrgentResponse
                {
                    Success = true,
                    Message = "Đã cập nhật trạng thái làm gấp"
                };
            }
            catch (Exception ex)
            {
                return new RequestUrgentResponse
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Loại bỏ tất cả các đoạn tag [LÀM GẤP: ...] khỏi ghi chú để tránh bị nối nhiều lần.
        /// Giữ lại các phần ghi chú khác của waiter.
        /// </summary>
        private string? CleanUrgentNotes(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
            {
                return notes;
            }

            // Xóa các đoạn " [LÀM GẤP: ...]" (tiếng Việt, có thể có khoảng trắng trước)
            var cleaned = Regex.Replace(notes, @"\s*\[LÀM GẤP:[^\]]*\]", string.Empty, RegexOptions.IgnoreCase);
            return cleaned.Trim();
        }

        public async Task<CancelOrderDetailResponse> CancelOrderDetailAsync(CancelOrderDetailDto request)
        {
            try
            {
                var orderDetail = await _unitOfWork.OrderDetails.GetByIdAsync(request.OrderDetailId);
                if (orderDetail == null)
                {
                    return new CancelOrderDetailResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy món ăn"
                    };
                }

                // ✅ XỬ LÝ COMBO: Nếu có OrderComboItemId → đang hủy món lẻ trong combo
                if (request.OrderComboItemId.HasValue && request.OrderComboItemId.Value > 0)
                {
                    // Load tất cả OrderComboItems của combo này
                    var allComboItems = await _unitOfWork.OrderComboItems.GetByOrderDetailIdAsync(request.OrderDetailId);
                    
                    if (allComboItems == null || !allComboItems.Any())
                    {
                        return new CancelOrderDetailResponse
                        {
                            Success = false,
                            Message = "Không tìm thấy các món trong combo"
                        };
                    }

                    // Kiểm tra xem có món nào trong combo đã nấu/sẵn sàng/hoàn thành không
                    bool hasCookedItem = false;
                    foreach (var comboItem in allComboItems)
                    {
                        var comboItemStatus = (comboItem.Status ?? "Pending").Trim();
                        var normalizedComboStatus = NormalizeStatus(comboItemStatus);
                        
                        // Nếu có món đã Cooking, Ready, hoặc Done → không cho hủy
                        if (normalizedComboStatus == "Cooking" || 
                            normalizedComboStatus == "Ready" || 
                            normalizedComboStatus == "Done")
                        {
                            hasCookedItem = true;
                            break;
                        }
                    }

                    if (hasCookedItem)
                    {
                        return new CancelOrderDetailResponse
                        {
                            Success = false,
                            Message = "Đã có món trong combo được nấu/sẵn sàng/hoàn thành, không thể hủy. Vui lòng sử dụng chức năng 'Trả món'"
                        };
                    }

                    // ✅ Nếu chưa có món nào nấu (tất cả đều Pending) → hủy cả combo
                    // Giải phóng reserved quantity cho OrderDetail (combo cha)
                    if (orderDetail.MenuItem != null)
                    {
                        var releaseResult = await _inventoryService.ReleaseReservedBatchesForOrderDetailAsync(request.OrderDetailId);
                        if (!releaseResult.success)
                        {
                        }
                    }

                    // Hủy OrderDetail (combo cha)
                    orderDetail.Status = "Cancelled";
                    if (!string.IsNullOrEmpty(request.Reason))
                    {
                        var currentNotes = orderDetail.Notes ?? "";
                        orderDetail.Notes = $"{currentNotes} [ĐÃ HỦY COMBO: {request.Reason}]".Trim();
                    }

                    // Hủy tất cả OrderComboItems (các món con)
                    foreach (var comboItem in allComboItems)
                    {
                        comboItem.Status = "Cancelled";
                        await _unitOfWork.OrderComboItems.UpdateAsync(comboItem);
                    }

                    await _unitOfWork.SaveChangesAsync();

                    // Tính số tiền hoàn lại
                    var comboRefundAmount = orderDetail.Quantity * orderDetail.UnitPrice;

                    return new CancelOrderDetailResponse
                    {
                        Success = true,
                        Message = "Đã hủy combo thành công",
                        RefundAmount = comboRefundAmount
                    };
                }

                // ✅ XỬ LÝ MÓN LẺ: Không có OrderComboItemId → hủy món lẻ bình thường
                // Kiểm tra status - cho phép hủy khi đang chờ hoặc đang nấu
                var status = (orderDetail.Status ?? "Pending").Trim();
                var normalizedStatus = NormalizeStatus(status);
                if (normalizedStatus == "Ready" || normalizedStatus == "Done")
                {
                    return new CancelOrderDetailResponse
                    {
                        Success = false,
                        Message = "Món đã sẵn sàng hoặc hoàn thành, không thể hủy. Vui lòng sử dụng chức năng 'Trả món'"
                    };
                }

                //  QUAN TRỌNG: Giải phóng reserved quantity TRƯỚC KHI cập nhật status
                // Nếu món đã được reserve nguyên liệu, cần giải phóng để available có thể tăng lại
                // Phải gọi TRƯỚC khi set status = Cancelled để release có thể check status Pending/Cooking
                if (orderDetail.MenuItem != null)
                {
                    var releaseResult = await _inventoryService.ReleaseReservedBatchesForOrderDetailAsync(request.OrderDetailId);
                    if (!releaseResult.success)
                    {
                        // Log warning nhưng không fail việc hủy món
                    }
                }

                // Cập nhật status thành Cancelled
                orderDetail.Status = "Cancelled";
                
                // Lưu lý do vào Notes
                if (!string.IsNullOrEmpty(request.Reason))
                {
                    var currentNotes = orderDetail.Notes ?? "";
                    orderDetail.Notes = $"{currentNotes} [ĐÃ HỦY: {request.Reason}]".Trim();
                }

                await _unitOfWork.SaveChangesAsync();

                // Tính số tiền hoàn lại (không tính tiền vì chưa nấu)
                var refundAmount = orderDetail.Quantity * orderDetail.UnitPrice;

                // TODO: Gửi SignalR notification

                return new CancelOrderDetailResponse
                {
                    Success = true,
                    Message = "Đã hủy món thành công",
                    RefundAmount = refundAmount
                };
            }
            catch (Exception ex)
            {
                return new CancelOrderDetailResponse
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        public async Task<MarkAsServedResponse> MarkAsServedAsync(MarkAsServedDto request)
        {
            try
            {
                // Nếu có OrderComboItemId → xử lý theo từng món trong combo
                if (request.OrderComboItemId.HasValue && request.OrderComboItemId.Value > 0)
                {
                    var comboItem = await _unitOfWork.OrderComboItems.GetByIdWithMenuItemAsync(request.OrderComboItemId.Value);
                    if (comboItem == null)
                    {
                        return new MarkAsServedResponse
                        {
                            Success = false,
                            Message = "Không tìm thấy món trong combo"
                        };
                    }

                    //  Món có BillingType = 1 (ConsumptionBased) không cần chờ Ready, có thể xác nhận ngay
                    var isComboItemConsumptionBased = comboItem.MenuItem?.BillingType == DomainAccessLayer.Enums.ItemBillingType.ConsumptionBased;
                    
                    if (!isComboItemConsumptionBased)
                    {
                        // Món KitchenPrepared - chỉ có thể đánh dấu đã phục vụ khi món đã Ready
                        var status = (comboItem.Status ?? "Pending").Trim();
                        var normalizedStatus = NormalizeStatus(status);
                        if (normalizedStatus != "Ready")
                        {
                            return new MarkAsServedResponse
                            {
                                Success = false,
                                Message = "Chỉ có thể lấy món khi món trong combo đã sẵn sàng"
                            };
                        }
                    }

                    // Đơn giản: đánh dấu món con trong combo là Done (đã phục vụ)
                    comboItem.Status = "Done";
                    await _unitOfWork.OrderComboItems.UpdateAsync(comboItem);
                    await _unitOfWork.SaveChangesAsync();

                    return new MarkAsServedResponse
                    {
                        Success = true,
                        Message = "Đã đánh dấu món trong combo đã phục vụ"
                    };
                }

                // Món lẻ (không phải combo) → giữ nguyên logic cũ
                var orderDetail = await _unitOfWork.OrderDetails.GetByIdWithMenuItemAsync(request.OrderDetailId);
                if (orderDetail == null)
                {
                    return new MarkAsServedResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy món ăn"
                    };
                }

                //  Món có BillingType = 1 (ConsumptionBased) không cần chờ Ready, có thể xác nhận ngay
                // Chỉ món KitchenPrepared mới cần chờ Ready
                var isConsumptionBased = orderDetail.MenuItem?.BillingType == DomainAccessLayer.Enums.ItemBillingType.ConsumptionBased;
                
                if (!isConsumptionBased)
                {
                    // Món KitchenPrepared - chỉ có thể đánh dấu đã phục vụ khi món đã Ready
                    var statusDetail = (orderDetail.Status ?? "Pending").Trim();
                    var statusLower = statusDetail.ToLower();
                    
                    if (!statusLower.Contains("ready") && !statusLower.Contains("sẵn sàng"))
                    {
                        return new MarkAsServedResponse
                        {
                            Success = false,
                            Message = "Chỉ có thể lấy món khi món đã sẵn sàng"
                        };
                    }
                }

                var totalQuantity = orderDetail.Quantity;
                var servedQuantity = request.Quantity > 0 ? request.Quantity : totalQuantity; // Mặc định lấy hết nếu không chỉ định

                if (servedQuantity <= 0 || servedQuantity > totalQuantity)
                {
                    return new MarkAsServedResponse
                    {
                        Success = false,
                        Message = $"Số lượng lấy ({servedQuantity}) phải lớn hơn 0 và không vượt quá số lượng sẵn sàng ({totalQuantity})"
                    };
                }

                // Nếu lấy hết số lượng, chỉ cần update status
                if (servedQuantity == totalQuantity)
                {
                    orderDetail.Status = "Done";
                    await _unitOfWork.OrderDetails.UpdateAsync(orderDetail);
                    await _unitOfWork.SaveChangesAsync();

                    return new MarkAsServedResponse
                    {
                        Success = true,
                        Message = "Đã đánh dấu món đã phục vụ"
                    };
                }

                // Nếu lấy một phần, cần split order detail
                // Tạo order detail mới cho phần đã lấy (Done)
                var servedOrderDetail = new DomainAccessLayer.Models.OrderDetail
                {
                    OrderId = orderDetail.OrderId,
                    MenuItemId = orderDetail.MenuItemId,
                    ComboId = orderDetail.ComboId,
                    Quantity = servedQuantity,
                    UnitPrice = orderDetail.UnitPrice,
                    Status = "Done",
                    Notes = orderDetail.Notes,
                    IsUrgent = orderDetail.IsUrgent,
                    StartedAt = orderDetail.StartedAt,
                    ReadyAt = orderDetail.ReadyAt,
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.OrderDetails.AddAsync(servedOrderDetail);
                await _unitOfWork.SaveChangesAsync();

                // Giảm số lượng của order detail gốc (vẫn giữ status Ready)
                orderDetail.Quantity = totalQuantity - servedQuantity;
                await _unitOfWork.OrderDetails.UpdateAsync(orderDetail);
                await _unitOfWork.SaveChangesAsync();

                // TODO: Gửi SignalR notification

                return new MarkAsServedResponse
                {
                    Success = true,
                    Message = $"Đã lấy {servedQuantity}/{totalQuantity} món. Còn lại {orderDetail.Quantity} món sẵn sàng."
                };
            }
            catch (Exception ex)
            {
                return new MarkAsServedResponse
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        public async Task<UpdateQuantityResponse> UpdateQuantityAsync(UpdateQuantityDto request)
        {
            try
            {
                // Validate quantity
                if (request.Quantity <= 0)
                {
                    return new UpdateQuantityResponse
                    {
                        Success = false,
                        Message = "Số lượng phải lớn hơn 0"
                    };
                }

                OrderDetail? orderDetail = null;

                // Nếu có OrderComboItemId → KHÔNG cho phép cập nhật số lượng cho món trong combo
                if (request.OrderComboItemId.HasValue && request.OrderComboItemId.Value > 0)
                {
                    // Lấy order combo item với MenuItem để kiểm tra BillingType
                    var orderComboItem = await _unitOfWork.OrderComboItems.GetByIdWithMenuItemAsync(request.OrderComboItemId.Value);
                    if (orderComboItem == null)
                    {
                        Console.WriteLine($"[UpdateQuantity] OrderComboItemId: {request.OrderComboItemId.Value} không tìm thấy");
                        return new UpdateQuantityResponse
                        {
                            Success = false,
                            Message = "Không tìm thấy món trong combo"
                        };
                    }

                    // Kiểm tra BillingType của món trong combo
                    if (orderComboItem.MenuItem == null)
                    {
                        return new UpdateQuantityResponse
                        {
                            Success = false,
                            Message = "Không tìm thấy thông tin món ăn trong combo"
                        };
                    }
                    
                    // ✅ QUAN TRỌNG: Món có BillingType = 1 trong combo KHÔNG được phép tăng/giảm số lượng
                    // Chỉ được phép xác nhận với số lượng hiện tại
                    if (orderComboItem.MenuItem.BillingType == DomainAccessLayer.Enums.ItemBillingType.ConsumptionBased)
                    {
                        return new UpdateQuantityResponse
                        {
                            Success = false,
                            Message = "Món tiêu hao trong combo không được phép thay đổi số lượng. Chỉ được phép xác nhận với số lượng hiện tại."
                        };
                    }
                    
                    // Nếu không phải ConsumptionBased, vẫn không cho phép update (theo yêu cầu)
                    return new UpdateQuantityResponse
                    {
                        Success = false,
                        Message = "Không được phép cập nhật số lượng cho món trong combo"
                    };
                }
                else
                {
                    // Món lẻ - cập nhật trực tiếp
                    // Lấy order detail với MenuItem để kiểm tra BillingType
                    orderDetail = await _unitOfWork.OrderDetails.GetByIdWithMenuItemAsync(request.OrderDetailId);
                    if (orderDetail == null)
                    {
                        return new UpdateQuantityResponse
                        {
                            Success = false,
                            Message = "Không tìm thấy món ăn"
                        };
                    }

                    // Kiểm tra BillingType - chỉ cho phép update quantity cho món có BillingType = 1 (ConsumptionBased)
                    if (orderDetail.MenuItem == null)
                    {
                        return new UpdateQuantityResponse
                        {
                            Success = false,
                            Message = "Không tìm thấy thông tin món ăn"
                        };
                    }
                    
                    if (orderDetail.MenuItem.BillingType != DomainAccessLayer.Enums.ItemBillingType.ConsumptionBased)
                    {
                        return new UpdateQuantityResponse
                        {
                            Success = false,
                            Message = "Chỉ có thể cập nhật số lượng cho món tiêu hao (nước, khăn ướt, v.v.)"
                        };
                    }

                    // Cập nhật quantity cho món lẻ
                    orderDetail.Quantity = request.Quantity;
                    orderDetail.QuantityUsed = request.Quantity;
                }

                // Lưu thay đổi (cho cả combo item và món lẻ)
                if (orderDetail != null)
                {
                    await _unitOfWork.OrderDetails.UpdateAsync(orderDetail);
                    await _unitOfWork.SaveChangesAsync();

                    return new UpdateQuantityResponse
                    {
                        Success = true,
                        Message = "Đã cập nhật số lượng thành công"
                    };
                }
                else
                {
                    return new UpdateQuantityResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy món ăn để cập nhật"
                    };
                }

                // Lưu thay đổi cho combo item
                await _unitOfWork.OrderDetails.UpdateAsync(orderDetail);
                await _unitOfWork.SaveChangesAsync();

                return new UpdateQuantityResponse
                {
                    Success = true,
                    Message = "Đã cập nhật số lượng thành công"
                };
            }
            catch (Exception ex)
            {
                return new UpdateQuantityResponse
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        public async Task<ConfirmConsumptionQuantityResponse> ConfirmConsumptionQuantityAsync(ConfirmConsumptionQuantityDto request)
        {
            try
            {
                // Validate quantity
                if (request.Quantity <= 0)
                {
                    return new ConfirmConsumptionQuantityResponse
                    {
                        Success = false,
                        Message = "Số lượng phải lớn hơn 0"
                    };
                }

                // Nếu có OrderComboItemId → xác nhận cho món trong combo
                if (request.OrderComboItemId.HasValue && request.OrderComboItemId.Value > 0)
                {
                    // Lấy order combo item với MenuItem để kiểm tra BillingType
                    var orderComboItem = await _unitOfWork.OrderComboItems.GetByIdWithMenuItemAsync(request.OrderComboItemId.Value);
                    if (orderComboItem == null)
                    {
                        Console.WriteLine($"[ConfirmConsumptionQuantity] OrderComboItemId: {request.OrderComboItemId.Value} không tìm thấy");
                        return new ConfirmConsumptionQuantityResponse
                        {
                            Success = false,
                            Message = "Không tìm thấy món trong combo"
                        };
                    }

                    // Kiểm tra BillingType của món trong combo
                    if (orderComboItem.MenuItem == null)
                    {
                        Console.WriteLine($"[ConfirmConsumptionQuantity] OrderComboItemId: {request.OrderComboItemId.Value}, MenuItem is null");
                        return new ConfirmConsumptionQuantityResponse
                        {
                            Success = false,
                            Message = "Không tìm thấy thông tin món ăn trong combo"
                        };
                    }
                    
                    if (orderComboItem.MenuItem.BillingType != DomainAccessLayer.Enums.ItemBillingType.ConsumptionBased)
                    {
                        return new ConfirmConsumptionQuantityResponse
                        {
                            Success = false,
                            Message = "Chỉ có thể xác nhận số lượng cho món tiêu hao (nước, khăn ướt, v.v.)"
                        };
                    }

                    // Lấy order detail để kiểm tra số lượng hiện tại
                    var orderDetail = await _unitOfWork.OrderDetails.GetByIdAsync(request.OrderDetailId);
                    if (orderDetail == null)
                    {
                        return new ConfirmConsumptionQuantityResponse
                        {
                            Success = false,
                            Message = "Không tìm thấy món ăn"
                        };
                    }

                    // ✅ QUAN TRỌNG: Món trong combo chỉ được xác nhận với số lượng hiện tại, không được thay đổi
                    // Tính số lượng hiện tại: comboItemQuantity * orderDetail.Quantity
                    var comboItemQuantity = orderComboItem.Quantity;
                    if (comboItemQuantity <= 0)
                    {
                        return new ConfirmConsumptionQuantityResponse
                        {
                            Success = false,
                            Message = "Số lượng món trong combo không hợp lệ"
                        };
                    }

                    var currentTotalQuantity = comboItemQuantity * orderDetail.Quantity;
                    
                    // Validate: số lượng xác nhận phải bằng số lượng hiện tại
                    if (request.Quantity != currentTotalQuantity)
                    {
                        return new ConfirmConsumptionQuantityResponse
                        {
                            Success = false,
                            Message = $"Món trong combo chỉ được xác nhận với số lượng hiện tại ({currentTotalQuantity}). Không được phép thay đổi số lượng."
                        };
                    }

                    // Xác nhận với số lượng hiện tại (không thay đổi orderDetail.Quantity)
                    orderDetail.QuantityUsed = request.Quantity;

                    // Đánh dấu món con trong combo là Done (đã xác nhận)
                    orderComboItem.Status = "Done";
                    orderComboItem.ReadyAt = DateTime.Now;
                    await _unitOfWork.OrderComboItems.UpdateAsync(orderComboItem);
                    
                    // Đánh dấu order detail là Done (đã xác nhận số lượng)
                    orderDetail.Status = "Done";
                    orderDetail.ReadyAt = DateTime.Now;
                    
                    // Lưu thay đổi
                    await _unitOfWork.OrderDetails.UpdateAsync(orderDetail);
                    await _unitOfWork.SaveChangesAsync();

                    return new ConfirmConsumptionQuantityResponse
                    {
                        Success = true,
                        Message = "Đã xác nhận số lượng thành công"
                    };
                }
                else
                {
                    // Món lẻ - xác nhận trực tiếp
                    // Lấy order detail với MenuItem để kiểm tra BillingType
                    var orderDetail = await _unitOfWork.OrderDetails.GetByIdWithMenuItemAsync(request.OrderDetailId);
                    if (orderDetail == null)
                    {
                        return new ConfirmConsumptionQuantityResponse
                        {
                            Success = false,
                            Message = "Không tìm thấy món ăn"
                        };
                    }

                    // Kiểm tra BillingType - chỉ cho phép xác nhận cho món có BillingType = 1 (ConsumptionBased)
                    if (orderDetail.MenuItem == null)
                    {
                        return new ConfirmConsumptionQuantityResponse
                        {
                            Success = false,
                            Message = "Không tìm thấy thông tin món ăn"
                        };
                    }
                    
                    if (orderDetail.MenuItem.BillingType != DomainAccessLayer.Enums.ItemBillingType.ConsumptionBased)
                    {
                        return new ConfirmConsumptionQuantityResponse
                        {
                            Success = false,
                            Message = "Chỉ có thể xác nhận số lượng cho món tiêu hao (nước, khăn ướt, v.v.)"
                        };
                    }

                    // Cập nhật quantity cho món lẻ
                    orderDetail.Quantity = request.Quantity;
                    orderDetail.QuantityUsed = request.Quantity;
                    
                    // Đánh dấu order detail là Done (đã xác nhận số lượng)
                    orderDetail.Status = "Done";
                    orderDetail.ReadyAt = DateTime.Now;
                    
                    // Lưu thay đổi
                    await _unitOfWork.OrderDetails.UpdateAsync(orderDetail);
                    await _unitOfWork.SaveChangesAsync();

                    return new ConfirmConsumptionQuantityResponse
                    {
                        Success = true,
                        Message = "Đã xác nhận số lượng thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ConfirmConsumptionQuantityResponse
                {
                    Success = false,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }


        // Helper methods
        private string GetTableNumber(Order order)
        {
            // Ưu tiên: lấy số bàn từ ReservationTables (đặt bàn)
            if (order.Reservation != null && order.Reservation.ReservationTables != null)
            {
                var reservationTable = order.Reservation.ReservationTables
                    .FirstOrDefault(rt => rt.Table != null);
                if (reservationTable?.Table != null)
                {
                    return reservationTable.Table.TableNumber ?? "N/A";
                }

                // Fallback: tên khách trong reservation
                var reservationCustomer = order.Reservation.Customer?.User?.FullName;
                if (!string.IsNullOrEmpty(reservationCustomer))
                {
                    return reservationCustomer;
                }
            }

            // Tiếp theo: tên khách của order trực tiếp (walk-in)
            if (order.Customer != null && order.Customer.User != null)
            {
                return order.Customer.User.FullName ?? "Khách";
            }

            // Cuối cùng: hiển thị theo loại order
            return order.OrderType ?? "N/A";
        }

        private string GetAreaName(Order order)
        {
            if (order.Reservation != null && order.Reservation.ReservationTables != null)
            {
                var reservationTable = order.Reservation.ReservationTables
                    .FirstOrDefault(rt => rt.Table != null && rt.Table.Area != null);
                if (reservationTable?.Table?.Area != null)
                {
                    return reservationTable.Table.Area.AreaName ?? "N/A";
                }
            }
            return "N/A";
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
    }
}

