using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Dbcontext;
using BusinessAccessLayer.DTOs.Kitchen;
using DomainAccessLayer.Models;

namespace BusinessAccessLayer.Services
{
    public class KitchenDisplayService : IKitchenDisplayService
    {
        private readonly SapaFoRestRmsContext _context;

        public KitchenDisplayService(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<List<KitchenOrderCardDto>> GetActiveOrdersAsync()
        {
            var now = DateTime.Now;

            var activeOrders = await _context.Orders
                .Include(o => o.KitchenTickets)
                    .ThenInclude(kt => kt.KitchenTicketDetails)
                        .ThenInclude(ktd => ktd.OrderDetail)
                            .ThenInclude(od => od.MenuItem)
                .Include(o => o.Customer)
                    .ThenInclude(c => c.User) // For customer name
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.Customer)
                        .ThenInclude(c => c.User) // For reservation customer name
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.Staff) // For staff who created reservation/order
                .Where(o => o.Status == "Processing" || o.Status == "Preparing")
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();

            var result = new List<KitchenOrderCardDto>();

            foreach (var order in activeOrders)
            {
                // Get all tickets for this order (one ticket per CourseType)
                var tickets = order.KitchenTickets.ToList();
                if (!tickets.Any()) continue;

                // Get all items from all tickets
                var items = tickets
                    .SelectMany(ticket => ticket.KitchenTicketDetails)
                    .Select(ktd => new KitchenOrderItemDto
                    {
                        TicketDetailId = ktd.TicketDetailId,
                        OrderDetailId = ktd.OrderDetailId,
                        MenuItemName = ktd.OrderDetail.MenuItem.Name,
                        Quantity = ktd.OrderDetail.Quantity,
                        Status = ktd.Status,
                        Notes = ktd.OrderDetail.Notes,
                        CourseType = ktd.OrderDetail.MenuItem.CourseType ?? "Other",
                        StartedAt = ktd.StartedAt,
                        CompletedAt = ktd.CompletedAt
                    })
                    .ToList();

                var waitingMinutes = (int)((now - (order.CreatedAt ?? now)).TotalMinutes);

                var completedCount = items.Count(i => i.Status == "Done");

                var card = new KitchenOrderCardDto
                {
                    OrderId = order.OrderId,
                    OrderNumber = $"A{order.OrderId:D2}", // Format: A01, A02...
                    TableNumber = GetTableNumber(order),
                    StaffName = GetStaffName(order), // NEW: Tên nhân viên
                    CreatedAt = order.CreatedAt ?? DateTime.Now,
                    WaitingMinutes = waitingMinutes,
                    PriorityLevel = GetPriorityLevel(waitingMinutes),
                    TotalItems = items.Count,
                    CompletedItems = completedCount,
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
                var detail = await _context.KitchenTicketDetails
                    .Include(ktd => ktd.OrderDetail)
                        .ThenInclude(od => od.MenuItem)
                    .FirstOrDefaultAsync(ktd => ktd.TicketDetailId == request.TicketDetailId);

                if (detail == null)
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "Item not found"
                    };
                }

                // Update status
                detail.Status = request.NewStatus;
                detail.AssignedUserId = request.UserId;

                if (request.NewStatus == "Cooking" && detail.StartedAt == null)
                {
                    detail.StartedAt = DateTime.Now;
                }
                else if (request.NewStatus == "Done" && detail.CompletedAt == null)
                {
                    detail.CompletedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return new StatusUpdateResponse
                {
                    Success = true,
                    Message = "Status updated successfully",
                    UpdatedItem = new KitchenOrderItemDto
                    {
                        TicketDetailId = detail.TicketDetailId,
                        OrderDetailId = detail.OrderDetailId,
                        MenuItemName = detail.OrderDetail.MenuItem.Name,
                        Quantity = detail.OrderDetail.Quantity,
                        Status = detail.Status,
                        Notes = detail.OrderDetail.Notes,
                        CourseType = detail.OrderDetail.MenuItem.CourseType ?? "Other",
                        StartedAt = detail.StartedAt,
                        CompletedAt = detail.CompletedAt
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

        public async Task<StatusUpdateResponse> CompleteOrderAsync(CompleteOrderRequest request)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.KitchenTickets)
                        .ThenInclude(kt => kt.KitchenTicketDetails)
                    .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

                if (order == null)
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "Order not found"
                    };
                }

                var ticket = order.KitchenTickets.FirstOrDefault();
                if (ticket == null)
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "Kitchen ticket not found"
                    };
                }

                // Check if all items are done
                var allDone = ticket.KitchenTicketDetails.All(ktd => ktd.Status == "Done");
                if (!allDone)
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "Not all items are completed yet"
                    };
                }

                // Update order and ticket status
                order.Status = "Completed";
                ticket.Status = "Completed";

                await _context.SaveChangesAsync();

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
            return await _context.MenuItems
                .Where(m => !string.IsNullOrEmpty(m.CourseType))
                .Select(m => m.CourseType!)
                .Distinct()
                .OrderBy(ct => ct)
                .ToListAsync();
        }

        public async Task<List<GroupedMenuItemDto>> GetGroupedItemsByMenuItemAsync()
        {
            var now = DateTime.Now;

            // Lấy tất cả active orders với items
            var activeOrders = await _context.Orders
                .Include(o => o.KitchenTickets)
                    .ThenInclude(kt => kt.KitchenTicketDetails)
                        .ThenInclude(ktd => ktd.OrderDetail)
                            .ThenInclude(od => od.MenuItem)
                .Include(o => o.Customer)
                    .ThenInclude(c => c.User)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.Customer)
                        .ThenInclude(c => c.User)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r.ReservationTables)
                        .ThenInclude(rt => rt.Table)
                .Where(o => o.Status == "Processing" || o.Status == "Preparing")
                .ToListAsync();

            // Flatten tất cả items từ tất cả orders
            var allItems = new List<(KitchenTicketDetail Detail, Order Order, OrderDetail OrderDetail, MenuItem MenuItem)>();

            foreach (var order in activeOrders)
            {
                foreach (var ticket in order.KitchenTickets)
                {
                    foreach (var detail in ticket.KitchenTicketDetails)
                    {
                        if (detail.OrderDetail?.MenuItem != null)
                        {
                            allItems.Add((detail, order, detail.OrderDetail, detail.OrderDetail.MenuItem));
                        }
                    }
                }
            }

            // Nhóm theo MenuItemId
            var grouped = allItems
                .GroupBy(item => new
                {
                    item.MenuItem.MenuItemId,
                    item.MenuItem.Name,
                    item.MenuItem.ImageUrl,
                    item.MenuItem.CourseType
                })
                .Select(g => new GroupedMenuItemDto
                {
                    MenuItemId = g.Key.MenuItemId,
                    MenuItemName = g.Key.Name,
                    ImageUrl = g.Key.ImageUrl,
                    CourseType = g.Key.CourseType ?? "Other",
                    TotalQuantity = g.Sum(item => item.OrderDetail.Quantity),
                    ItemDetails = g.Select(item => new GroupedItemDetailDto
                    {
                        TicketDetailId = item.Detail.TicketDetailId,
                        OrderId = item.Order.OrderId,
                        OrderNumber = $"A{item.Order.OrderId:D2}",
                        TableNumber = GetTableNumber(item.Order),
                        Quantity = item.OrderDetail.Quantity,
                        Status = item.Detail.Status,
                        Notes = item.OrderDetail.Notes,
                        CreatedAt = item.Order.CreatedAt ?? DateTime.Now,
                        WaitingMinutes = (int)((now - (item.Order.CreatedAt ?? now)).TotalMinutes)
                    }).OrderByDescending(d => d.WaitingMinutes).ToList() // Sắp xếp theo thời gian chờ giảm dần
                })
                .OrderByDescending(g => g.TotalQuantity) // Sắp xếp theo tổng số lượng giảm dần
                .ToList();

            return grouped;
        }

        // Helper methods
        private string GetTableNumber(Order order)
        {
            // PRIORITY 1: Get from reservation table
            if (order.Reservation != null)
            {
                var table = _context.ReservationTables
                    .Include(rt => rt.Table)
                    .FirstOrDefault(rt => rt.ReservationId == order.ReservationId);

                if (table != null)
                {
                    return table.Table.TableNumber ?? "N/A";
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

        private string GetStaffName(Order order)
        {
            // Get staff name from reservation (staff who created the reservation/order)
            if (order.Reservation != null && order.Reservation.Staff != null)
            {
                return order.Reservation.Staff.FullName ?? "N/A";
            }

            // Fallback if no staff assigned
            return "N/A";
        }

        private string GetPriorityLevel(int waitingMinutes)
        {
            if (waitingMinutes > 15) return "Critical";  // Red - >15 phút
            if (waitingMinutes >= 10) return "Warning";  // Yellow - 10-15 phút
            return "Normal";                             // White/Light - 1-10 phút
        }
    }
}