using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.UnitOfWork.Interfaces;
using BusinessAccessLayer.DTOs.Kitchen;
using DomainAccessLayer.Models;

namespace BusinessAccessLayer.Services
{
    public class KitchenDisplayService : IKitchenDisplayService
    {
        private readonly IUnitOfWork _unitOfWork;

        public KitchenDisplayService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<KitchenOrderCardDto>> GetActiveOrdersAsync()
        {
            var now = DateTime.Now;

            var activeOrders = await _unitOfWork.Orders.GetActiveOrdersAsync();

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
                        IsUrgent = od.IsUrgent,
                        TimeCook = od.MenuItem.TimeCook, // Thời gian nấu (phút)
                        BatchSize = od.MenuItem.BatchSize
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
                var currentStatus = orderDetail.Status ?? "Pending";
                var newStatus = request.NewStatus;

                // Validate status transitions
                if (currentStatus == "Pending")
                {
                    // From Pending, only allow transition to Cooking
                    if (newStatus != "Cooking")
                    {
                        return new StatusUpdateResponse
                        {
                            Success = false,
                            Message = $"Không thể chuyển từ trạng thái 'Chờ' sang '{newStatus}'. Phải chuyển sang 'Đang nấu' trước."
                        };
                    }
                }
                else if (currentStatus == "Cooking")
                {
                    // From Cooking, only allow transition to Done
                    if (newStatus != "Done")
                    {
                        return new StatusUpdateResponse
                        {
                            Success = false,
                            Message = $"Không thể chuyển từ trạng thái 'Đang nấu' sang '{newStatus}'. Chỉ có thể chuyển sang 'Hoàn thành'."
                        };
                    }
                }
                else if (currentStatus == "Done")
                {
                    // From Done, only allow transition back to Cooking (unfulfill)
                    if (newStatus != "Cooking")
                    {
                        return new StatusUpdateResponse
                        {
                            Success = false,
                            Message = $"Không thể chuyển từ trạng thái 'Hoàn thành' sang '{newStatus}'. Chỉ có thể quay lại 'Đang nấu'."
                        };
                    }
                }

                // Update status trên OrderDetail (nguồn chính)
                orderDetail.Status = request.NewStatus;

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
                        StartedAt = null, // Không dùng KitchenTicketDetail nữa
                        CompletedAt = null, // Không dùng KitchenTicketDetail nữa
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
                    Message = $"Error: {ex.Message}"
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

        public async Task<List<GroupedMenuItemDto>> GetGroupedItemsByMenuItemAsync()
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
                    // Chỉ lấy những món chưa nấu (status = "Pending" hoặc null)
                    // Món đã nấu (Cooking) hoặc đã hoàn thành (Done) sẽ không được thêm vào
                    var status = orderDetail.Status ?? "Pending";
                    if (orderDetail.MenuItem != null && status == "Pending")
                    {
                        allItems.Add((order, orderDetail, orderDetail.MenuItem));
                    }
                }
            }

            // Nhóm theo MenuItemId
            // Lưu ý: allItems chỉ chứa những orderDetail có status = "Pending"
            // Do đó TotalQuantity sẽ chỉ tính tổng số lượng của những món chưa nấu
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
                    // TotalQuantity chỉ tính những món còn Pending (chưa nấu)
                    // Ví dụ: có 7 món mực xào, đã nấu 2 món → chỉ hiển thị x5
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
                .Where(g => g.TotalQuantity > 0) // Chỉ lấy những món có ít nhất 1 item Pending
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
                        FireTime = fireTime,
                        TimeCook = orderDetail.MenuItem.TimeCook, // Thời gian nấu (phút)
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
                    StaffName = order.Reservation?.Customer?.User?.FullName ?? "N/A",
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
    }
}