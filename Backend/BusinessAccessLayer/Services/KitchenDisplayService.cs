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
                .Include(o => o.OrderDetails)
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
                // Get all order details directly
                var orderDetails = order.OrderDetails.ToList();
                if (!orderDetails.Any()) continue;

                // Map OrderDetail to KitchenOrderItemDto
                var items = orderDetails
                    .Select(od => new KitchenOrderItemDto
                    {
                        OrderDetailId = od.OrderDetailId,
                        MenuItemName = od.MenuItem.Name,
                        Quantity = od.Quantity,
                        Status = od.Status ?? "Pending", // Default to Pending if null
                        Notes = od.Notes,
                        CourseType = od.MenuItem.CourseType ?? "Other",
                        StartedAt = null, // OrderDetail doesn't have StartedAt
                        CompletedAt = null, // OrderDetail doesn't have CompletedAt
                        IsUrgent = od.IsUrgent
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
                var orderDetail = await _context.OrderDetails
                    .Include(od => od.MenuItem)
                    .FirstOrDefaultAsync(od => od.OrderDetailId == request.OrderDetailId);

                if (orderDetail == null)
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "Item not found"
                    };
                }

                // Update status trên OrderDetail (nguồn chính)
                orderDetail.Status = request.NewStatus;

                await _context.SaveChangesAsync();

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
                        StartedAt = null, // Không dùng KitchenTicketDetail nữa
                        CompletedAt = null, // Không dùng KitchenTicketDetail nữa
                        IsUrgent = orderDetail.IsUrgent
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
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

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

            // Lấy tất cả active orders với order details
            var activeOrders = await _context.Orders
                .Include(o => o.OrderDetails)
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

            // Flatten tất cả order details từ tất cả orders
            var allItems = new List<(Order Order, OrderDetail OrderDetail, MenuItem MenuItem)>();

            foreach (var order in activeOrders)
            {
                foreach (var orderDetail in order.OrderDetails)
                {
                    if (orderDetail.MenuItem != null)
                    {
                        allItems.Add((order, orderDetail, orderDetail.MenuItem));
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

        public async Task<StationItemsResponse> GetStationItemsByCategoryAsync(string categoryName)
        {
            var now = DateTime.Now;

            // Lấy tất cả active orders với order details thuộc category này
            var activeOrders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.MenuItem)
                        .ThenInclude(mi => mi.Category)
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

            var allItems = new List<StationItemDto>();
            var urgentItems = new List<StationItemDto>();

            // Debug: Log số lượng orders
            Console.WriteLine($"[GetStationItemsByCategoryAsync] Found {activeOrders.Count} active orders");
            Console.WriteLine($"[GetStationItemsByCategoryAsync] Filtering by categoryName: {categoryName}");
            
            // Log tổng số order details trước khi filter
            var totalOrderDetails = activeOrders.Sum(o => o.OrderDetails.Count);
            Console.WriteLine($"[GetStationItemsByCategoryAsync] Total order details before filter: {totalOrderDetails}");

            foreach (var order in activeOrders)
            {
                // Lọc order details theo category name
                var orderDetails = order.OrderDetails
                    .Where(od => od.MenuItem != null && 
                                 od.MenuItem.Category != null && 
                                 od.MenuItem.Category.CategoryName.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
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
                        if (od.MenuItem?.Category != null)
                        {
                            Console.WriteLine($"[GetStationItemsByCategoryAsync]   - Item: {od.MenuItem.Name}, Category: {od.MenuItem.Category.CategoryName}");
                        }
                        else
                        {
                            Console.WriteLine($"[GetStationItemsByCategoryAsync]   - Item: {od.MenuItem?.Name}, Category: NULL");
                        }
                    }
                }

                foreach (var orderDetail in orderDetails)
                {
                    var waitingMinutes = (int)((now - (order.CreatedAt ?? now)).TotalMinutes);
                    var createdAtTime = (order.CreatedAt ?? DateTime.Now).ToString("HH:mm");

                    // Lấy status từ OrderDetail (nguồn chính)
                    var status = orderDetail.Status ?? "Pending";
                    
                    // Hiển thị thời gian hiện tại khi status = "Cooking" (thời gian bắt đầu nấu)
                    // Không lưu StartedAt, chỉ hiển thị real-time
                    var fireTime = string.Empty;
                    DateTime? startedAt = null;
                    
                    // Nếu status = "Cooking", hiển thị thời gian hiện tại
                    if (status == "Cooking" || status == "Đang chế biến")
                    {
                        // Có thể dùng CreatedAt của OrderDetail làm thời gian fire
                        // Hoặc hiển thị thời gian hiện tại (real-time)
                        // Ở đây dùng CreatedAt của OrderDetail (thời gian tạo order detail)
                        startedAt = orderDetail.CreatedAt;
                        fireTime = orderDetail.CreatedAt.ToString("HH:mm");
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
                        StartedAt = startedAt,
                        FireTime = fireTime
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
                var orderDetail = await _context.OrderDetails
                    .FirstOrDefaultAsync(od => od.OrderDetailId == request.OrderDetailId);

                if (orderDetail == null)
                {
                    return new StatusUpdateResponse
                    {
                        Success = false,
                        Message = "Order detail not found"
                    };
                }

                orderDetail.IsUrgent = request.IsUrgent;
                await _context.SaveChangesAsync();

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
            return await _context.MenuCategories
                .Select(c => c.CategoryName)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }
    }
}