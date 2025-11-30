using AutoMapper;
using BusinessAccessLayer.DTOs.OrderConfirmation;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Enums;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace BusinessAccessLayer.Services
{
    /// <summary>
    /// Service xử lý logic xác nhận hóa đơn
    /// </summary>
    public class OrderConfirmationService : IOrderConfirmationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        
        public OrderConfirmationService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        
        public async Task<OrderConfirmationDto?> GetOrderForConfirmationAsync(int orderId, CancellationToken ct = default)
        {
            var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(orderId);
            
            if (order == null)
            {
                return null;
            }
            
            var dto = new OrderConfirmationDto
            {
                OrderId = order.OrderId,
                OrderCode = $"ORD-{order.OrderId:D6}",
                CustomerName = order.Customer?.User?.FullName ?? "Khách vãng lai",
                CustomerPhone = order.Customer?.User?.Phone,
                CreatedAt = order.CreatedAt,
                Status = order.Status ?? "Pending",
                DiscountAmount = 0m // Will be calculated from Payments if any
            };
            
            // Get table numbers
            if (order.Reservation?.ReservationTables != null && order.Reservation.ReservationTables.Any())
            {
                var tableNumbers = order.Reservation.ReservationTables
                    .Where(rt => rt.Table != null)
                    .Select(rt => rt.Table.TableNumber ?? "")
                    .Where(tn => !string.IsNullOrEmpty(tn))
                    .ToList();
                    
                dto.TableNumbers = tableNumbers;
                dto.TableNumber = string.Join(", ", tableNumbers);
            }
            
            // Map order items
            if (order.OrderDetails != null && order.OrderDetails.Any())
            {
                foreach (var od in order.OrderDetails.Where(x => x.Status != "Removed"))
                {
                    var item = new OrderItemConfirmDto
                    {
                        OrderDetailId = od.OrderDetailId,
                        MenuItemId = od.MenuItemId,
                        ComboId = od.ComboId,
                        ItemName = od.ComboId.HasValue ? (od.Combo?.Name ?? "Combo") : (od.MenuItem?.Name ?? "Món ăn"),
                        ItemType = od.ComboId.HasValue ? "Combo" : "MenuItem",
                        QuantityOrdered = od.Quantity,
                        QuantityUsed = od.QuantityUsed ?? od.Quantity, // Default to ordered quantity
                        UnitPrice = od.UnitPrice,
                        Status = od.Status ?? "Pending",
                        Notes = od.Notes
                    };
                    
                    // Determine billing type
                    if (od.MenuItemId.HasValue && od.MenuItem != null)
                    {
                        item.BillingType = od.MenuItem.BillingType;
                    }
                    else
                    {
                        // Combo items default to KitchenPrepared
                        item.BillingType = ItemBillingType.KitchenPrepared;
                    }
                    
                    // Calculate total price
                    if (item.IsConsumptionItem)
                    {
                        item.TotalPrice = (item.QuantityUsed ?? item.QuantityOrdered) * item.UnitPrice;
                    }
                    else // Kitchen item
                    {
                        item.TotalPrice = item.QuantityOrdered * item.UnitPrice;
                    }
                    
                    dto.AllItems.Add(item);
                }
            }
            
            return dto;
        }
        
        public async Task<OrderConfirmationDto> ConfirmOrderAsync(ConfirmOrderRequestDto request, CancellationToken ct = default)
        {
            var order = await _unitOfWork.Payments.GetOrderWithItemsAsync(request.OrderId);
            
            if (order == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy đơn hàng với ID: {request.OrderId}");
            }
            
            if (order.OrderDetails == null || !order.OrderDetails.Any())
            {
                throw new InvalidOperationException("Đơn hàng không có món để xác nhận.");
            }
            
            // Update consumption items với QuantityUsed
            foreach (var consumptionUpdate in request.ConsumptionItems)
            {
                var orderDetail = order.OrderDetails.FirstOrDefault(x => x.OrderDetailId == consumptionUpdate.OrderDetailId);
                
                if (orderDetail == null) continue;
                
                // Validate: QuantityUsed <= QuantityOrdered
                if (consumptionUpdate.QuantityUsed < 0 || consumptionUpdate.QuantityUsed > orderDetail.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Số lượng sử dụng ({consumptionUpdate.QuantityUsed}) phải từ 0 đến {orderDetail.Quantity} cho món '{orderDetail.MenuItem?.Name ?? "Món"}'");
                }
                
                // Check billing type
                if (orderDetail.MenuItem?.BillingType == ItemBillingType.ConsumptionBased)
                {
                    orderDetail.QuantityUsed = consumptionUpdate.QuantityUsed;
                }
            }
            
            // Update order status
            order.Status = "Confirmed";
            order.ConfirmedAt = DateTime.UtcNow;
            
            if (request.ConfirmedByStaffId.HasValue)
            {
                order.ConfirmedByStaffId = request.ConfirmedByStaffId.Value;
            }
            
            await _unitOfWork.SaveChangesAsync();
            
            // Return updated confirmation
            return await GetOrderForConfirmationAsync(request.OrderId, ct) 
                ?? throw new InvalidOperationException("Không thể load lại đơn hàng sau khi xác nhận");
        }
        
        public async Task<bool> CancelItemAsync(CancelItemRequestDto request, CancellationToken ct = default)
        {
            var orderDetail = await _unitOfWork.Payments.GetOrderDetailByIdAsync(request.OrderDetailId);
            
            if (orderDetail == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy món với ID: {request.OrderDetailId}");
            }
            
            // Validate can cancel
            var (canCancel, reason) = await ValidateCanCancelItemAsync(request.OrderDetailId, ct);
            
            if (!canCancel)
            {
                throw new InvalidOperationException(reason);
            }
            
            // Mark as removed
            orderDetail.Status = "Removed";
            orderDetail.Quantity = 0;
            orderDetail.QuantityUsed = 0;
            orderDetail.Notes = $"Đã hủy: {request.Reason}. " + orderDetail.Notes;
            
            await _unitOfWork.SaveChangesAsync();
            
            return true;
        }
        
        public async Task<(bool CanCancel, string Reason)> ValidateCanCancelItemAsync(int orderDetailId, CancellationToken ct = default)
        {
            var orderDetail = await _unitOfWork.Payments.GetOrderDetailByIdAsync(orderDetailId);
            
            if (orderDetail == null)
            {
                return (false, "Không tìm thấy món");
            }
            
            // Check if it's a kitchen item
            var isKitchenItem = orderDetail.MenuItem?.BillingType == ItemBillingType.KitchenPrepared;
            
            if (!isKitchenItem)
            {
                return (false, "Chỉ món chế biến trong bếp mới có thể hủy. Món tiêu hao vui lòng điều chỉnh số lượng sử dụng.");
            }
            
            // Check kitchen status
            var status = orderDetail.Status ?? "Pending";
            
            if (status == "Cooking")
            {
                return (false, "Món đang được chế biến, không thể hủy");
            }
            
            if (status == "Done" || status == "Served")
            {
                return (false, "Món đã hoàn thành, không thể hủy");
            }
            
            if (status == "Removed")
            {
                return (false, "Món đã được hủy trước đó");
            }
            
            // Can cancel if status is Pending or Confirmed (NotStarted)
            if (status == "Pending" || status == "Confirmed")
            {
                return (true, "Có thể hủy món");
            }
            
            return (false, $"Trạng thái '{status}' không cho phép hủy món");
        }
    }
}

