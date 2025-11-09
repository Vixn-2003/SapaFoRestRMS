using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Dbcontext;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace SapaFoRestRMSAPI.Services
{
    public static class DataSeeder
    {
        public static async Task SeedAdminAsync(SapaFoRestRmsContext context)
        {
            var email = "vinxnguyen0310@gmail.com";
            var existing = await context.Users.FirstOrDefaultAsync(u => u.Email == email);

            var adminRoleId = await context.Roles.Where(r => r.RoleName == "Admin").Select(r => r.RoleId).FirstOrDefaultAsync();
            if (adminRoleId == 0)
            {
                // Fallback to create role if missing (should be seeded via OnModelCreating)
                var adminRole = new Role { RoleName = "Admin" };
                await context.Roles.AddAsync(adminRole);
                await context.SaveChangesAsync();
                adminRoleId = adminRole.RoleId;
            }

            string HashPassword(string password)
            {
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }

            if (existing == null)
            {
                var admin = new User
                {
                    FullName = "System Admin",
                    Email = email,
                    PasswordHash = HashPassword("C\"=Nt1,qu@F16oX86"),
                    RoleId = adminRoleId,
                    Status = 0,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                await context.Users.AddAsync(admin);
            }
            else
            {
                // Ensure role and password are correct for development convenience
                existing.RoleId = adminRoleId;
                existing.PasswordHash = HashPassword("C\"=Nt1,qu@F16oX86");
                context.Users.Update(existing);
            }
            await context.SaveChangesAsync();
        }

        public static async Task SeedPositionsAsync(SapaFoRestRmsContext context)
        {
            // Ensure table exists
            if (!await context.Database.CanConnectAsync())
            {
                return;
            }

            // Desired seed positions
            var desiredPositions = new List<Position>
            {
                new Position { PositionName = "Waiter/Waitress", Description = "Front-of-house service staff", Status = 0 },
                new Position { PositionName = "Cashier", Description = "Handles billing and payments", Status = 0 },
                new Position { PositionName = "Kitchen Staff", Description = "Back-of-house food preparation", Status = 0 },
                new Position { PositionName = "Inventory Staff", Description = "Warehouse and stock management", Status = 0 }
            };

            foreach (var pos in desiredPositions)
            {
                var exists = await context.Positions.AnyAsync(p => p.PositionName == pos.PositionName);
                if (!exists)
                {
                    await context.Positions.AddAsync(pos);
                }
            }

            await context.SaveChangesAsync();
        }

        public static async Task SeedTestCustomerAsync(SapaFoRestRmsContext context)
        {
            // Ensure role 'Customer' exists or create it
            var customerRoleId = await context.Roles.Where(r => r.RoleName == "Customer").Select(r => r.RoleId).FirstOrDefaultAsync();
            if (customerRoleId == 0)
            {
                var role = new Role { RoleName = "Customer" };
                await context.Roles.AddAsync(role);
                await context.SaveChangesAsync();
                customerRoleId = role.RoleId;
            }

            var phone = "0900000001";
            var email = "test.customer@example.com";

            var existing = await context.Users.FirstOrDefaultAsync(u => (u.Phone == phone || u.Email == email) && u.IsDeleted == false);
            if (existing == null)
            {
                var user = new User
                {
                    FullName = "Test Customer",
                    Email = email,
                    Phone = phone,
                    PasswordHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()))),
                    RoleId = customerRoleId,
                    Status = 0,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();

                var customer = await context.Customers.FirstOrDefaultAsync(c => c.UserId == user.UserId);
                if (customer == null)
                {
                    await context.Customers.AddAsync(new Customer
                    {
                        UserId = user.UserId,
                        LoyaltyPoints = 0,
                        Notes = "Seeded test customer"
                    });
                }
                await context.SaveChangesAsync();
            }
        }

        public static async Task SeedKitchenOrdersAsync(SapaFoRestRmsContext context)
        {
            // Ensure database connection
            if (!await context.Database.CanConnectAsync())
            {
                return;
            }

            // Check if seed data already exists
            var existingOrders = await context.Orders
                .Include(o => o.Reservation)
                .Where(o => o.Status == "Processing" || o.Status == "Preparing")
                .ToListAsync();

            // If orders exist, ensure their reservations have StaffId
            if (existingOrders.Any())
            {
                // Get or create staff user first
                var existingStaffRoleId = await context.Roles
                    .Where(r => r.RoleName == "Staff")
                    .Select(r => r.RoleId)
                    .FirstOrDefaultAsync();

                if (existingStaffRoleId == 0)
                {
                    var staffRole = new Role { RoleName = "Staff" };
                    await context.Roles.AddAsync(staffRole);
                    await context.SaveChangesAsync();
                    existingStaffRoleId = staffRole.RoleId;
                }

                var existingStaffUser = await context.Users
                    .FirstOrDefaultAsync(u => u.Email == "staff.test@example.com" && u.IsDeleted == false);

                if (existingStaffUser == null)
                {
                    existingStaffUser = new User
                    {
                        FullName = "Nguyễn Văn Phục Vụ",
                        Email = "staff.test@example.com",
                        Phone = "0900000003",
                        PasswordHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes("test"))),
                        RoleId = existingStaffRoleId,
                        Status = 0,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };
                    await context.Users.AddAsync(existingStaffUser);
                    await context.SaveChangesAsync();

                    var staff = new Staff
                    {
                        UserId = existingStaffUser.UserId,
                        HireDate = DateOnly.FromDateTime(DateTime.Today),
                        SalaryBase = 5000000,
                        Status = 0
                    };
                    await context.Staffs.AddAsync(staff);
                    await context.SaveChangesAsync();
                }
                else if (string.IsNullOrEmpty(existingStaffUser.FullName) || existingStaffUser.FullName == "System Admin")
                {
                    existingStaffUser.FullName = "Nguyễn Văn Phục Vụ";
                    context.Users.Update(existingStaffUser);
                    await context.SaveChangesAsync();
                }

                // Update existing reservations to have StaffId
                var reservationsToUpdate = existingOrders
                    .Where(o => o.Reservation != null && o.Reservation.StaffId == null)
                    .Select(o => o.Reservation!)
                    .Distinct()
                    .ToList();

                foreach (var res in reservationsToUpdate)
                {
                    res.StaffId = existingStaffUser.UserId;
                    context.Reservations.Update(res);
                }

                if (reservationsToUpdate.Any())
                {
                    await context.SaveChangesAsync();
                }

                // Skip creating new seed data if orders already exist
                return;
            }

            // Get or create customer
            var customerRoleId = await context.Roles
                .Where(r => r.RoleName == "Customer")
                .Select(r => r.RoleId)
                .FirstOrDefaultAsync();

            if (customerRoleId == 0)
            {
                var customerRole = new Role { RoleName = "Customer" };
                await context.Roles.AddAsync(customerRole);
                await context.SaveChangesAsync();
                customerRoleId = customerRole.RoleId;
            }

            var customer = await context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                var user = new User
                {
                    FullName = "Khách hàng Test",
                    Email = "customer.test@example.com",
                    Phone = "0900000002",
                    PasswordHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes("test"))),
                    RoleId = customerRoleId,
                    Status = 0,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();

                customer = new Customer
                {
                    UserId = user.UserId,
                    LoyaltyPoints = 0,
                    Notes = "Test customer for kitchen orders"
                };
                await context.Customers.AddAsync(customer);
                await context.SaveChangesAsync();
            }

            // Get or create Area and Table
            var area = await context.Areas.FirstOrDefaultAsync();
            if (area == null)
            {
                area = new Area
                {
                    AreaName = "Tầng 1",
                    Floor = 1,
                    Description = "Khu vực tầng 1"
                };
                await context.Areas.AddAsync(area);
                await context.SaveChangesAsync();
            }

            var table = await context.Tables.FirstOrDefaultAsync();
            if (table == null)
            {
                table = new Table
                {
                    TableNumber = "12",
                    Capacity = 4,
                    Status = "Occupied",
                    AreaId = area.AreaId
                };
                await context.Tables.AddAsync(table);
                await context.SaveChangesAsync();
            }

            // Get or create MenuCategories for different stations
            // Tên trạm: Khai Vị, Lẩu, Nướng than, Xào – Chiên, Trạm Cơm – Canh, Tráng Miệng
            var categories = new List<MenuCategory>();
            var categoryNames = new[] { "Khai Vị", "Lẩu", "Nướng than", "Xào – Chiên", "Trạm Cơm – Canh", "Tráng Miệng" };
            
            foreach (var categoryName in categoryNames)
            {
                var existingCategory = await context.MenuCategories
                    .FirstOrDefaultAsync(c => c.CategoryName == categoryName);
                
                if (existingCategory == null)
                {
                    var newCategory = new MenuCategory
                    {
                        CategoryName = categoryName
                    };
                    await context.MenuCategories.AddAsync(newCategory);
                    await context.SaveChangesAsync();
                    categories.Add(newCategory);
                }
                else
                {
                    categories.Add(existingCategory);
                }
            }
            
            // Use first category as default if no categories were created
            var category = categories.FirstOrDefault() ?? await context.MenuCategories.FirstOrDefaultAsync();
            if (category == null)
            {
                category = new MenuCategory
                {
                    CategoryName = "Món chính"
                };
                await context.MenuCategories.AddAsync(category);
                await context.SaveChangesAsync();
                categories.Add(category);
            }
            
            // Map categories by name
            var khaiViCategory = categories.FirstOrDefault(c => c.CategoryName == "Khai Vị") ?? category;
            var lauCategory = categories.FirstOrDefault(c => c.CategoryName == "Lẩu") ?? category;
            var nuongThanCategory = categories.FirstOrDefault(c => c.CategoryName == "Nướng than") ?? category;
            var xaoChienCategory = categories.FirstOrDefault(c => c.CategoryName == "Xào – Chiên") ?? category;
            var comCanhCategory = categories.FirstOrDefault(c => c.CategoryName == "Trạm Cơm – Canh") ?? category;
            var trangMiengCategory = categories.FirstOrDefault(c => c.CategoryName == "Tráng Miệng") ?? category;

            // Create MenuItems with different CourseTypes
            // CourseType: Khai vị, Món chính, Tráng miệng
            var menuItems = new List<MenuItem>();
            
            // Check if menu items already exist
            var existingMenuItems = await context.MenuItems
                .Where(m => m.CourseType == "Khai vị" || m.CourseType == "Món chính" || m.CourseType == "Tráng miệng")
                .ToListAsync();

            if (existingMenuItems.Count == 0)
            {
                menuItems = new List<MenuItem>
                {
                    // Món Nướng (Món chính) -> Trạm "Nướng than"
                    new MenuItem
                    {
                        Name = "Thịt nướng",
                        Description = "Thịt nướng thơm ngon",
                        Price = 150000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = nuongThanCategory.CategoryId
                    },
                    new MenuItem
                    {
                        Name = "Gà nướng",
                        Description = "Gà nướng nguyên con",
                        Price = 250000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = nuongThanCategory.CategoryId
                    },
                    new MenuItem
                    {
                        Name = "Tôm nướng",
                        Description = "Tôm nướng bơ tỏi",
                        Price = 200000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = nuongThanCategory.CategoryId
                    },
                    new MenuItem
                    {
                        Name = "Cá nướng",
                        Description = "Cá nướng muối ớt",
                        Price = 180000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = nuongThanCategory.CategoryId
                    },
                    // Món Xào (Món chính) -> Trạm "Xào – Chiên"
                    new MenuItem
                    {
                        Name = "Rau xào",
                        Description = "Rau xào tươi ngon",
                        Price = 80000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = xaoChienCategory.CategoryId
                    },
                    new MenuItem
                    {
                        Name = "Mực xào",
                        Description = "Mực xào rau muống",
                        Price = 180000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = xaoChienCategory.CategoryId
                    },
                    new MenuItem
                    {
                        Name = "Thịt bò xào",
                        Description = "Thịt bò xào hành tây",
                        Price = 220000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = xaoChienCategory.CategoryId
                    },
                    new MenuItem
                    {
                        Name = "Gà xào sả ớt",
                        Description = "Gà xào sả ớt cay",
                        Price = 190000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = xaoChienCategory.CategoryId
                    },
                    // Món Chiên (Món chính) -> Trạm "Xào – Chiên"
                    new MenuItem
                    {
                        Name = "Khoai tây chiên",
                        Description = "Khoai tây chiên giòn",
                        Price = 70000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = xaoChienCategory.CategoryId
                    },
                    new MenuItem
                    {
                        Name = "Cá chiên",
                        Description = "Cá chiên giòn",
                        Price = 160000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = xaoChienCategory.CategoryId
                    },
                    // Lẩu (Món chính) -> Trạm "Lẩu"
                    new MenuItem
                    {
                        Name = "Lẩu thái",
                        Description = "Lẩu thái chua cay",
                        Price = 300000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = lauCategory.CategoryId
                    },
                    // Canh (Món chính) -> Trạm "Trạm Cơm – Canh"
                    new MenuItem
                    {
                        Name = "Canh chua cá",
                        Description = "Canh chua cá bông lau",
                        Price = 120000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = comCanhCategory.CategoryId
                    },
                    new MenuItem
                    {
                        Name = "Canh khổ qua",
                        Description = "Canh khổ qua nhồi thịt",
                        Price = 100000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = comCanhCategory.CategoryId
                    },
                    new MenuItem
                    {
                        Name = "Canh chua tôm",
                        Description = "Canh chua tôm cà",
                        Price = 130000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = comCanhCategory.CategoryId
                    },
                    // Salad (Khai vị) -> Trạm "Khai Vị"
                    new MenuItem
                    {
                        Name = "Salad rau củ",
                        Description = "Salad rau củ tươi",
                        Price = 90000,
                        CourseType = "Khai vị",
                        IsAvailable = true,
                        CategoryId = khaiViCategory.CategoryId
                    },
                    new MenuItem
                    {
                        Name = "Salad tôm",
                        Description = "Salad tôm tươi",
                        Price = 150000,
                        CourseType = "Khai vị",
                        IsAvailable = true,
                        CategoryId = khaiViCategory.CategoryId
                    },
                    // Tráng miệng -> Trạm "Tráng Miệng"
                    new MenuItem
                    {
                        Name = "Chè đậu xanh",
                        Description = "Chè đậu xanh ngọt mát",
                        Price = 50000,
                        CourseType = "Tráng miệng",
                        IsAvailable = true,
                        CategoryId = trangMiengCategory.CategoryId
                    },
                    new MenuItem
                    {
                        Name = "Kem dừa",
                        Description = "Kem dừa thơm mát",
                        Price = 60000,
                        CourseType = "Tráng miệng",
                        IsAvailable = true,
                        CategoryId = trangMiengCategory.CategoryId
                    }
                };

                await context.MenuItems.AddRangeAsync(menuItems);
                await context.SaveChangesAsync();
            }
            else
            {
                menuItems = existingMenuItems;
            }

            // Get or create Staff user for reservation
            var staffRoleId = await context.Roles
                .Where(r => r.RoleName == "Staff")
                .Select(r => r.RoleId)
                .FirstOrDefaultAsync();

            if (staffRoleId == 0)
            {
                var staffRole = new Role { RoleName = "Staff" };
                await context.Roles.AddAsync(staffRole);
                await context.SaveChangesAsync();
                staffRoleId = staffRole.RoleId;
            }

            var staffUser = await context.Users
                .FirstOrDefaultAsync(u => u.Email == "staff.test@example.com" && u.IsDeleted == false);

            if (staffUser == null)
            {
                staffUser = new User
                {
                    FullName = "Nguyễn Văn Phục Vụ",
                    Email = "staff.test@example.com",
                    Phone = "0900000003",
                    PasswordHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes("test"))),
                    RoleId = staffRoleId,
                    Status = 0,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                await context.Users.AddAsync(staffUser);
                await context.SaveChangesAsync();

                // Create Staff record
                var staff = new Staff
                {
                    UserId = staffUser.UserId,
                    HireDate = DateOnly.FromDateTime(DateTime.Today),
                    SalaryBase = 5000000,
                    Status = 0
                };
                await context.Staffs.AddAsync(staff);
                await context.SaveChangesAsync();
            }
            else
            {
                // Ensure staff name is correct
                if (string.IsNullOrEmpty(staffUser.FullName) || staffUser.FullName == "System Admin")
                {
                    staffUser.FullName = "Nguyễn Văn Phục Vụ";
                    context.Users.Update(staffUser);
                    await context.SaveChangesAsync();
                }
            }

            // Create Reservation for some orders
            var reservation = new Reservation
            {
                CustomerId = customer.CustomerId,
                CustomerNameReservation = customer.User?.FullName ?? "Khách hàng Test",
                StaffId = staffUser.UserId, // Assign staff who created the reservation/order
                ReservationDate = DateTime.Today,
                TimeSlot = "Ca tối",
                ReservationTime = DateTime.Now.AddHours(-1),
                NumberOfGuests = 4,
                Status = "Confirmed"
            };
            await context.Reservations.AddAsync(reservation);
            await context.SaveChangesAsync();

            // Link table to reservation
            var reservationTable = new ReservationTable
            {
                ReservationId = reservation.ReservationId,
                TableId = table.TableId
            };
            await context.ReservationTables.AddAsync(reservationTable);
            await context.SaveChangesAsync();

            // Create Orders with different statuses and times
            var now = DateTime.Now;
            var orders = new List<Order>
            {
                // Order 1: Processing (recent, 5 minutes ago)
                new Order
                {
                    ReservationId = reservation.ReservationId,
                    CustomerId = customer.CustomerId,
                    OrderType = "DineIn",
                    Status = "Processing",
                    CreatedAt = now.AddMinutes(-5),
                    TotalAmount = 0
                },
                // Order 2: Preparing (older, 10 minutes ago)
                new Order
                {
                    ReservationId = reservation.ReservationId,
                    CustomerId = customer.CustomerId,
                    OrderType = "DineIn",
                    Status = "Preparing",
                    CreatedAt = now.AddMinutes(-10),
                    TotalAmount = 0
                },
                // Order 3: Processing (very recent, 2 minutes ago)
                new Order
                {
                    ReservationId = reservation.ReservationId,
                    CustomerId = customer.CustomerId,
                    OrderType = "DineIn",
                    Status = "Processing",
                    CreatedAt = now.AddMinutes(-2),
                    TotalAmount = 0
                }
            };

            await context.Orders.AddRangeAsync(orders);
            await context.SaveChangesAsync();

            // Create OrderDetails for each order
            var orderDetails = new List<OrderDetail>();
            var random = new Random();
            // Create mapping for MenuItemId to CourseType
            var menuItemCourseTypeMap = menuItems.ToDictionary(m => m.MenuItemId, m => m.CourseType);

            // Define specific items for each order to ensure variety
            var orderItemsConfig = new List<List<int>>
            {
                // Order 1: 7 món (nhiều món)
                new List<int> { 0, 1, 4, 5, 6, 8, 9 }, // Thịt nướng, Gà nướng, Rau xào, Mực xào, Thịt bò xào, Canh chua cá, Lẩu thái
                // Order 2: 8 món (rất nhiều món)
                new List<int> { 0, 2, 3, 4, 5, 6, 7, 8, 10 }, // Nhiều món đa dạng
                // Order 3: 6 món (nhiều món vừa phải)
                new List<int> { 1, 3, 5, 7, 9, 11 } // Gà nướng, Cá nướng, Mực xào, Gà xào sả ớt, Lẩu thái, Canh chua tôm
            };

            for (int i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                
                // Get items for this order (use config if available, otherwise random)
                List<MenuItem> selectedItems;
                if (i < orderItemsConfig.Count && orderItemsConfig[i].All(idx => idx < menuItems.Count))
                {
                    // Use configured items
                    selectedItems = orderItemsConfig[i]
                        .Select(idx => menuItems[idx])
                        .ToList();
                }
                else
                {
                    // Fallback: random 6-8 items
                    var itemCount = random.Next(6, 9);
                    selectedItems = menuItems.OrderBy(x => random.Next()).Take(itemCount).ToList();
                }

                foreach (var menuItem in selectedItems)
                {
                    var quantity = random.Next(1, 4); // 1-3 phần mỗi món
                    var isUrgent = random.Next(0, 10) == 0; // 10% chance of being urgent
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        MenuItemId = menuItem.MenuItemId,
                        Quantity = quantity,
                        UnitPrice = menuItem.Price,
                        Status = "Pending",
                        CreatedAt = order.CreatedAt ?? DateTime.Now,
                        Notes = random.Next(0, 4) == 0 ? "Không cay" : (random.Next(0, 4) == 1 ? "Ít muối" : null), // Some items have notes
                        IsUrgent = isUrgent // Some items are marked as urgent
                    };
                    orderDetails.Add(orderDetail);
                }
            }

            await context.OrderDetails.AddRangeAsync(orderDetails);
            await context.SaveChangesAsync();

            // Create KitchenTickets and KitchenTicketDetails
            foreach (var order in orders)
            {
                var orderDetailList = orderDetails.Where(od => od.OrderId == order.OrderId).ToList();
                
                if (!orderDetailList.Any()) continue;

                // Group by CourseType using the mapping
                var groupedByCourseType = orderDetailList
                    .GroupBy(od => menuItemCourseTypeMap[od.MenuItemId])
                    .ToList();

                foreach (var group in groupedByCourseType)
                {
                    var kitchenTicket = new KitchenTicket
                    {
                        OrderId = order.OrderId,
                        CourseType = group.Key,
                        Status = "Active",
                        CreatedAt = order.CreatedAt ?? DateTime.Now
                    };
                    await context.KitchenTickets.AddAsync(kitchenTicket);
                    await context.SaveChangesAsync();

                    // Create KitchenTicketDetails for each OrderDetail in this group
                    foreach (var orderDetail in group)
                    {
                        var kitchenTicketDetail = new KitchenTicketDetail
                        {
                            TicketId = kitchenTicket.TicketId,
                            OrderDetailId = orderDetail.OrderDetailId,
                            Status = random.Next(0, 3) == 0 ? "Cooking" : "Pending", // Some items are already cooking
                            StartedAt = random.Next(0, 3) == 0 ? DateTime.Now.AddMinutes(-2) : null
                        };
                        await context.KitchenTicketDetails.AddAsync(kitchenTicketDetail);
                    }
                }
            }

            await context.SaveChangesAsync();

            // Update TotalAmount for orders
            foreach (var order in orders)
            {
                var total = orderDetails
                    .Where(od => od.OrderId == order.OrderId)
                    .Sum(od => od.UnitPrice * od.Quantity);
                order.TotalAmount = total;
            }

            await context.SaveChangesAsync();
        }
    }
}


