using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Dbcontext;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

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

            // Check if seed data already exists by looking for the test customer
            var testCustomer = await context.Customers
                .Include(c => c.User)
                .Where(c => c.User != null && (c.User.Email == "customer.test@example.com" || c.User.Phone == "0900000002"))
                .FirstOrDefaultAsync();

            // If test customer already exists, skip creating new seed data
            // (even if orders were deleted or completed, we don't want to create duplicate seed data)
            if (testCustomer != null)
            {
                // Check if there are any orders linked to this test customer
                var existingSeedOrders = await context.Orders
                    .Where(o => o.CustomerId == testCustomer.CustomerId)
                    .ToListAsync();

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
                var reservationsToUpdate = existingSeedOrders
                    .Where(o => o.ReservationId != null)
                    .Select(o => o.ReservationId)
                    .Distinct()
                    .ToList();

                if (reservationsToUpdate.Any())
                {
                    var reservations = await context.Reservations
                        .Where(r => reservationsToUpdate.Contains(r.ReservationId) && r.StaffId == null)
                        .ToListAsync();

                    foreach (var res in reservations)
                    {
                        res.StaffId = existingStaffUser.UserId;
                        context.Reservations.Update(res);
                    }

                    if (reservations.Any())
                    {
                        await context.SaveChangesAsync();
                    }
                }

                // Skip creating new seed data if test customer already exists
                return;
            }

            // Get or create customer SPECIFICALLY for kitchen orders
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

            // Check if customer with specific email/phone already exists
            var customer = await context.Customers
                .Include(c => c.User)
                .Where(c => c.User != null && (c.User.Email == "customer.test@example.com" || c.User.Phone == "0900000002"))
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
            
            // Check if this customer already has seed orders
            // If yes, skip creating new orders to avoid duplicates
            var existingOrders = await context.Orders
                .Where(o => o.CustomerId == customer.CustomerId)
                .ToListAsync();
            
            if (existingOrders.Any())
            {
                // Customer already has orders, skip creating new seed data
                return;
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
                        CategoryId = nuongThanCategory.CategoryId,
                        TimeCook = 25,
                        BatchSize = 4
                    },
                    new MenuItem
                    {
                        Name = "Gà nướng",
                        Description = "Gà nướng nguyên con",
                        Price = 250000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = nuongThanCategory.CategoryId,
                        TimeCook = 30,
                        BatchSize = 2
                    },
                    new MenuItem
                    {
                        Name = "Tôm nướng",
                        Description = "Tôm nướng bơ tỏi",
                        Price = 200000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = nuongThanCategory.CategoryId,
                        TimeCook = 20,
                        BatchSize = 6
                    },
                    new MenuItem
                    {
                        Name = "Cá nướng",
                        Description = "Cá nướng muối ớt",
                        Price = 180000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = nuongThanCategory.CategoryId,
                        TimeCook = 22,
                        BatchSize = 3
                    },
                    // Món Xào (Món chính) -> Trạm "Xào – Chiên"
                    new MenuItem
                    {
                        Name = "Rau xào",
                        Description = "Rau xào tươi ngon",
                        Price = 80000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = xaoChienCategory.CategoryId,
                        TimeCook = 10,
                        BatchSize = 8
                    },
                    new MenuItem
                    {
                        Name = "Mực xào",
                        Description = "Mực xào rau muống",
                        Price = 180000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = xaoChienCategory.CategoryId,
                        TimeCook = 12,
                        BatchSize = 6
                    },
                    new MenuItem
                    {
                        Name = "Thịt bò xào",
                        Description = "Thịt bò xào hành tây",
                        Price = 220000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = xaoChienCategory.CategoryId,
                        TimeCook = 15,
                        BatchSize = 4
                    },
                    new MenuItem
                    {
                        Name = "Gà xào sả ớt",
                        Description = "Gà xào sả ớt cay",
                        Price = 190000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = xaoChienCategory.CategoryId,
                        TimeCook = 15,
                        BatchSize = 4
                    },
                    // Món Chiên (Món chính) -> Trạm "Xào – Chiên"
                    new MenuItem
                    {
                        Name = "Khoai tây chiên",
                        Description = "Khoai tây chiên giòn",
                        Price = 70000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = xaoChienCategory.CategoryId,
                        TimeCook = 12,
                        BatchSize = 10
                    },
                    new MenuItem
                    {
                        Name = "Cá chiên",
                        Description = "Cá chiên giòn",
                        Price = 160000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = xaoChienCategory.CategoryId,
                        TimeCook = 15,
                        BatchSize = 4
                    },
                    // Lẩu (Món chính) -> Trạm "Lẩu"
                    new MenuItem
                    {
                        Name = "Lẩu thái",
                        Description = "Lẩu thái chua cay",
                        Price = 300000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = lauCategory.CategoryId,
                        TimeCook = 20,
                        BatchSize = 1
                    },
                    // Canh (Món chính) -> Trạm "Trạm Cơm – Canh"
                    new MenuItem
                    {
                        Name = "Canh chua cá",
                        Description = "Canh chua cá bông lau",
                        Price = 120000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = comCanhCategory.CategoryId,
                        TimeCook = 18,
                        BatchSize = 3
                    },
                    new MenuItem
                    {
                        Name = "Canh khổ qua",
                        Description = "Canh khổ qua nhồi thịt",
                        Price = 100000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = comCanhCategory.CategoryId,
                        TimeCook = 20,
                        BatchSize = 2
                    },
                    new MenuItem
                    {
                        Name = "Canh chua tôm",
                        Description = "Canh chua tôm cà",
                        Price = 130000,
                        CourseType = "Món chính",
                        IsAvailable = true,
                        CategoryId = comCanhCategory.CategoryId,
                        TimeCook = 15,
                        BatchSize = 3
                    },
                    // Salad (Khai vị) -> Trạm "Khai Vị"
                    new MenuItem
                    {
                        Name = "Salad rau củ",
                        Description = "Salad rau củ tươi",
                        Price = 90000,
                        CourseType = "Khai vị",
                        IsAvailable = true,
                        CategoryId = khaiViCategory.CategoryId,
                        TimeCook = 8,
                        BatchSize = 10
                    },
                    new MenuItem
                    {
                        Name = "Salad tôm",
                        Description = "Salad tôm tươi",
                        Price = 150000,
                        CourseType = "Khai vị",
                        IsAvailable = true,
                        CategoryId = khaiViCategory.CategoryId,
                        TimeCook = 10,
                        BatchSize = 8
                    },
                    // Tráng miệng -> Trạm "Tráng Miệng"
                    new MenuItem
                    {
                        Name = "Chè đậu xanh",
                        Description = "Chè đậu xanh ngọt mát",
                        Price = 50000,
                        CourseType = "Tráng miệng",
                        IsAvailable = true,
                        CategoryId = trangMiengCategory.CategoryId,
                        TimeCook = 5,
                        BatchSize = 12
                    },
                    new MenuItem
                    {
                        Name = "Kem dừa",
                        Description = "Kem dừa thơm mát",
                        Price = 60000,
                        CourseType = "Tráng miệng",
                        IsAvailable = true,
                        CategoryId = trangMiengCategory.CategoryId,
                        TimeCook = 3,
                        BatchSize = 15
                    }
                };

                foreach (var item in menuItems)
                {
                    item.BatchSize ??= 1;
                }

                await context.MenuItems.AddRangeAsync(menuItems);
                await context.SaveChangesAsync();
            }
            else
            {
                menuItems = existingMenuItems;
                
                // Update TimeCook for existing menu items if they don't have it
                var timeCookMap = new Dictionary<string, int>
                {
                    // Món Nướng
                    { "Thịt nướng", 25 },
                    { "Gà nướng", 30 },
                    { "Tôm nướng", 20 },
                    { "Cá nướng", 22 },
                    // Món Xào
                    { "Rau xào", 10 },
                    { "Mực xào", 12 },
                    { "Thịt bò xào", 15 },
                    { "Gà xào sả ớt", 15 },
                    // Món Chiên
                    { "Khoai tây chiên", 12 },
                    { "Cá chiên", 15 },
                    // Lẩu
                    { "Lẩu thái", 20 },
                    // Canh
                    { "Canh chua cá", 18 },
                    { "Canh khổ qua", 20 },
                    { "Canh chua tôm", 15 },
                    // Salad
                    { "Salad rau củ", 8 },
                    { "Salad tôm", 10 },
                    // Tráng miệng
                    { "Chè đậu xanh", 5 },
                    { "Kem dừa", 3 }
                };
                
                foreach (var item in existingMenuItems.Where(m => m.TimeCook == null && timeCookMap.ContainsKey(m.Name)))
                {
                    item.TimeCook = timeCookMap[item.Name];
                }

                var batchSizeMap = new Dictionary<string, int>
                {
                    // Món Nướng
                    { "Thịt nướng", 4 },
                    { "Gà nướng", 2 },
                    { "Tôm nướng", 6 },
                    { "Cá nướng", 3 },
                    // Món Xào
                    { "Rau xào", 8 },
                    { "Mực xào", 6 },
                    { "Thịt bò xào", 4 },
                    { "Gà xào sả ớt", 4 },
                    // Món Chiên
                    { "Khoai tây chiên", 10 },
                    { "Cá chiên", 4 },
                    // Lẩu
                    { "Lẩu thái", 1 },
                    // Canh
                    { "Canh chua cá", 3 },
                    { "Canh khổ qua", 2 },
                    { "Canh chua tôm", 3 },
                    // Salad
                    { "Salad rau củ", 10 },
                    { "Salad tôm", 8 },
                    // Tráng miệng
                    { "Chè đậu xanh", 12 },
                    { "Kem dừa", 15 }
                };

                foreach (var item in existingMenuItems)
                {
                    if (batchSizeMap.TryGetValue(item.Name, out var batchSize))
                    {
                        item.BatchSize = batchSize;
                    }
                    else if (item.BatchSize == null)
                    {
                        item.BatchSize = 1;
                    }
                }
                
                await context.SaveChangesAsync();
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
            // MenuItems index: 0-3 (Nướng), 4-9 (Xào-Chiên), 10 (Lẩu), 11-13 (Canh), 14-15 (Khai Vị - Salad), 16-17 (Tráng miệng)
            var orderItemsConfig = new List<List<int>>
            {
                // Order 1: 7 món (bao gồm Khai Vị)
                new List<int> { 0, 1, 4, 5, 6, 8, 14 }, // Thịt nướng, Gà nướng, Rau xào, Mực xào, Thịt bò xào, Khoai tây chiên, Salad rau củ (Khai Vị)
                // Order 2: 9 món (bao gồm Khai Vị và Tráng miệng)
                new List<int> { 0, 2, 3, 4, 5, 6, 7, 15, 16 }, // Nhiều món đa dạng + Salad tôm (Khai Vị) + Chè đậu xanh (Tráng miệng)
                // Order 3: 6 món (bao gồm Khai Vị)
                new List<int> { 1, 3, 5, 7, 9, 14 } // Gà nướng, Cá nướng, Mực xào, Gà xào sả ớt, Cá chiên, Salad rau củ (Khai Vị)
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
                    .GroupBy(od =>
                    {
                        var menuItemId = od.MenuItemId ?? 0;
                        return menuItemCourseTypeMap.TryGetValue(menuItemId, out var courseType)
                            ? courseType
                            : "Unknown";
                    })
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

        /// <summary>
        /// Seed test staff accounts with all positions for testing
        /// Creates one staff account for each position: Waiter/Waitress, Cashier, Kitchen Staff, Inventory Staff
        /// </summary>
        public static async Task SeedStaffWithAllPositionsAsync(SapaFoRestRmsContext context)
        {
            // Ensure roles and positions exist
            var staffRoleId = await context.Roles.Where(r => r.RoleName == "Staff")
                .Select(r => r.RoleId).FirstOrDefaultAsync();
            if (staffRoleId == 0)
            {
                var role = new Role { RoleName = "Staff" };
                await context.Roles.AddAsync(role);
                await context.SaveChangesAsync();
                staffRoleId = role.RoleId;
            }

            // Ensure positions exist
            await SeedPositionsAsync(context);
            var positions = await context.Positions.ToListAsync();

            string HashPassword(string password)
            {
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }

            // Create staff for each position
            var staffAccounts = new[]
            {
                new { Email = "cashier@test.com", FullName = "Test Cashier", Phone = "0900002001", PositionName = "Cashier" },
                new { Email = "waiter@test.com", FullName = "Test Waiter", Phone = "0900002002", PositionName = "Waiter/Waitress" },
                new { Email = "kitchen@test.com", FullName = "Test Kitchen Staff", Phone = "0900002003", PositionName = "Kitchen Staff" },
                new { Email = "inventory@test.com", FullName = "Test Inventory Staff", Phone = "0900002004", PositionName = "Inventory Staff" }
            };

            foreach (var account in staffAccounts)
            {
                var position = positions.FirstOrDefault(p => p.PositionName == account.PositionName);
                if (position == null) continue;

                // Check if user exists
                var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email == account.Email && u.IsDeleted == false);
                User user;
                Staff staff;

                if (existingUser == null)
                {
                    // Create new user
                    user = new User
                    {
                        FullName = account.FullName,
                        Email = account.Email,
                        Phone = account.Phone,
                        PasswordHash = HashPassword("Staff@123"),
                        RoleId = staffRoleId,
                        Status = 0,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };
                    await context.Users.AddAsync(user);
                    await context.SaveChangesAsync();

                    // Create staff profile
                    staff = new Staff
                    {
                        UserId = user.UserId,
                        HireDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                        SalaryBase = 7000000m,
                        Status = 0
                    };
                    await context.Staffs.AddAsync(staff);
                    await context.SaveChangesAsync();
                }
                else
                {
                    user = existingUser;
                    user.RoleId = staffRoleId;
                    user.PasswordHash = HashPassword("Staff@123"); // Reset password for testing
                    context.Users.Update(user);

                    // Get or create staff profile
                    staff = await context.Staffs.FirstOrDefaultAsync(s => s.UserId == user.UserId);
                    if (staff == null)
                    {
                        staff = new Staff
                        {
                            UserId = user.UserId,
                            HireDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                            SalaryBase = 7000000m,
                            Status = 0
                        };
                        await context.Staffs.AddAsync(staff);
                    }
                    await context.SaveChangesAsync();
                }

                // Assign position to staff (many-to-many relationship)
                // Check if position is already assigned
                var hasPosition = await context.Staffs
                    .Where(s => s.StaffId == staff.StaffId)
                    .SelectMany(s => s.Positions)
                    .AnyAsync(p => p.PositionId == position.PositionId);

                if (!hasPosition)
                {
                    // Load staff with positions to add new position
                    var staffWithPositions = await context.Staffs
                        .Include(s => s.Positions)
                        .FirstOrDefaultAsync(s => s.StaffId == staff.StaffId);

                    if (staffWithPositions != null)
                    {
                        staffWithPositions.Positions.Add(position);
                        context.Staffs.Update(staffWithPositions);
                    }
                }
            }

            await context.SaveChangesAsync();
        }
    }
}


