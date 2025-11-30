using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public WaiterOrderTrackingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<WaiterOrderTrackingDto> GetOrderTrackingAsync(int? waiterUserId = null)
        {
            var now = DateTime.Now;
            var result = new WaiterOrderTrackingDto();

            // Lấy tất cả active orders với order details
            var activeOrders = await _unitOfWork.Orders.GetActiveOrdersAsync();

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
                    var canCancel = status == "Pending" || status == "Chờ bếp xác nhận" || status == "Chờ";
                    var canRequestUrgent = status == "Pending" || status == "Chờ bếp xác nhận" || status == "Chờ" || 
                                          status == "Cooking" || status == "Đang nấu";
                    var isDone = statusLower.Contains("done") || statusLower.Contains("hoàn thành") || statusLower.Contains("xong");
                    
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
                        IsSplit = isSplit
                    };

                    allItems.Add(item);
                    group.Items.Add(item);

                    // Đếm theo status
                    if (statusLower.Contains("pending") || statusLower.Contains("chờ"))
                    {
                        result.WaitingKitchenCount++;
                        result.ProcessingCount++;
                    }
                    else if (statusLower.Contains("cooking") || statusLower.Contains("đang nấu") || 
                             statusLower.Contains("processing") || statusLower.Contains("đang xử lý") ||
                             statusLower.Contains("late") || statusLower.Contains("trễ"))
                    {
                        result.CookingCount++;
                        result.ProcessingCount++;
                    }
                    else if (statusLower.Contains("ready") || statusLower.Contains("sẵn sàng"))
                    {
                        result.ReadyCount++;
                        result.ProcessingCount++;
                    }
                    // Done items không đếm vào ProcessingCount, nhưng vẫn được thêm vào danh sách
                }
            }

            result.OrderGroups = orderGroups.Values.ToList();
            result.TotalCount = allItems.Count;

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

                // Kiểm tra status - chỉ có thể yêu cầu làm gấp khi chưa Done
                var status = (orderDetail.Status ?? "Pending").Trim();
                if (status == "Done" || status == "Hoàn thành" || status == "Xong")
                {
                    return new RequestUrgentResponse
                    {
                        Success = false,
                        Message = "Món đã hoàn thành, không thể yêu cầu làm gấp"
                    };
                }

                // Đánh dấu urgent
                orderDetail.IsUrgent = true;
                // Lưu lý do vào Notes (có thể tạo field riêng UrgentReason nếu cần)
                if (!string.IsNullOrEmpty(request.Reason))
                {
                    var currentNotes = orderDetail.Notes ?? "";
                    orderDetail.Notes = $"{currentNotes} [LÀM GẤP: {request.Reason}]".Trim();
                }

                await _unitOfWork.SaveChangesAsync();

                // TODO: Gửi SignalR notification cho bếp

                return new RequestUrgentResponse
                {
                    Success = true,
                    Message = "Đã yêu cầu làm gấp thành công"
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

                // Kiểm tra status - chỉ có thể hủy khi chưa nấu
                var status = (orderDetail.Status ?? "Pending").Trim();
                if (status == "Cooking" || status == "Đang nấu" || status == "Ready" || status == "Sẵn sàng" || 
                    status == "Done" || status == "Hoàn thành")
                {
                    return new CancelOrderDetailResponse
                    {
                        Success = false,
                        Message = "Món đã được nấu, không thể hủy. Vui lòng sử dụng chức năng 'Trả món'"
                    };
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
                var orderDetail = await _unitOfWork.OrderDetails.GetByIdWithMenuItemAsync(request.OrderDetailId);
                if (orderDetail == null)
                {
                    return new MarkAsServedResponse
                    {
                        Success = false,
                        Message = "Không tìm thấy món ăn"
                    };
                }

                // Kiểm tra status - chỉ có thể đánh dấu đã phục vụ khi món đã Ready
                var status = (orderDetail.Status ?? "Pending").Trim();
                var statusLower = status.ToLower();
                
                if (!statusLower.Contains("ready") && !statusLower.Contains("sẵn sàng"))
                {
                    return new MarkAsServedResponse
                    {
                        Success = false,
                        Message = "Chỉ có thể lấy món khi món đã sẵn sàng"
                    };
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


        // Helper methods
        private string GetTableNumber(Order order)
        {
            if (order.Reservation != null && order.Reservation.ReservationTables != null)
            {
                var reservationTable = order.Reservation.ReservationTables
                    .FirstOrDefault(rt => rt.Table != null);
                if (reservationTable?.Table != null)
                {
                    return reservationTable.Table.TableNumber ?? "N/A";
                }
            }
            return "N/A";
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
    }
}

