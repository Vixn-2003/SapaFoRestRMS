using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Dbcontext;
using DomainAccessLayer.Models;
using DomainAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using BusinessAccessLayer.Constants;

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
                    PasswordHash = HashPassword("C=Nt1,qu@F16oX86"),
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
                existing.PasswordHash = HashPassword("C=Nt1,qu@F16oX86");
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

        public static async Task SeedTestStaffAndManagerAsync(SapaFoRestRmsContext context)
        {
            // Ensure roles exist
            async Task<int> EnsureRoleAsync(string roleName)
            {
                var roleId = await context.Roles.Where(r => r.RoleName == roleName)
                    .Select(r => r.RoleId).FirstOrDefaultAsync();
                if (roleId == 0)
                {
                    var role = new Role { RoleName = roleName };
                    await context.Roles.AddAsync(role);
                    await context.SaveChangesAsync();
                    roleId = role.RoleId;
                }
                return roleId;
            }

            var staffRoleId = await EnsureRoleAsync("Staff");
            var managerRoleId = await EnsureRoleAsync("Manager");

            string HashPassword(string password)
            {
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }

            // Seed Manager user
            var managerEmail = "manager.seed@example.com";
            var manager = await context.Users.FirstOrDefaultAsync(u => u.Email == managerEmail && u.IsDeleted == false);
            if (manager == null)
            {
                manager = new User
                {
                    FullName = "Seed Manager",
                    Email = managerEmail,
                    Phone = "0900001001",
                    PasswordHash = HashPassword("Password123!"),
                    RoleId = managerRoleId,
                    Status = 0,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                await context.Users.AddAsync(manager);
            }
            else
            {
                manager.RoleId = managerRoleId;
            }

            // Seed Staff user + Staff profile
            var staffEmail = "staff.seed@example.com";
            var staffUser = await context.Users.FirstOrDefaultAsync(u => u.Email == staffEmail && u.IsDeleted == false);
            if (staffUser == null)
            {
                staffUser = new User
                {
                    FullName = "Seed Staff",
                    Email = staffEmail,
                    Phone = "0900001002",
                    PasswordHash = HashPassword("Password123!"),
                    RoleId = staffRoleId,
                    Status = 0,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                await context.Users.AddAsync(staffUser);
                await context.SaveChangesAsync();

                // Create Staff record
                var staffProfile = new Staff
                {
                    UserId = staffUser.UserId,
                    HireDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                    SalaryBase = 7000000m,
                    Status = 0
                };
                await context.Staffs.AddAsync(staffProfile);
            }
            else
            {
                staffUser.RoleId = staffRoleId;
                // Ensure staff profile exists
                var hasProfile = await context.Staffs.AnyAsync(s => s.UserId == staffUser.UserId);
                if (!hasProfile)
                {
                    await context.Staffs.AddAsync(new Staff
                    {
                        UserId = staffUser.UserId,
                        HireDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                        SalaryBase = 7000000m,
                        Status = 0
                    });
                }
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

        /// <summary>
        /// Seed sample data for testing the cashier payment workflow
        /// Includes: Combos with ComboItems, Orders with OrderDetails (both menu items and combos)
        /// </summary>
        public static async Task SeedCashierWorkflowTestAsync(SapaFoRestRmsContext context)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] === Starting cashier workflow seeding ===");

            // Cleanup previous pending/test orders
            var pendingOrderIds = await context.Orders
                .Where(o => o.Status == OrderStatusConstants.WaitingConfirmation ||
                           o.Status == OrderStatusConstants.Confirmed ||
                           o.Status == OrderStatusConstants.PendingPayment)
                .Select(o => o.OrderId)
                .ToListAsync();

            if (pendingOrderIds.Any())
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Cleaning {pendingOrderIds.Count} existing pending orders...");

                var oldPayments = context.Payments.Where(p => pendingOrderIds.Contains(p.OrderId));
                context.Payments.RemoveRange(oldPayments);

                var oldOrderDetails = context.OrderDetails.Where(od => pendingOrderIds.Contains(od.OrderId));
                context.OrderDetails.RemoveRange(oldOrderDetails);

                var oldOrders = context.Orders.Where(o => pendingOrderIds.Contains(o.OrderId));
                context.Orders.RemoveRange(oldOrders);

                await context.SaveChangesAsync();
                Console.WriteLine("✅ Previous pending orders removed.");
            }
            else
            {
                Console.WriteLine("ℹ️ No existing pending orders to clean.");
            }

            var ordersCreated = 0;

            // 1. Get or find required entities
            var cashierUser = await context.Users
                .Include(u => u.Staff)
                .FirstOrDefaultAsync(u => u.Email == "cashier@test.com" && u.IsDeleted == false);

            if (cashierUser == null || cashierUser.Staff == null || !cashierUser.Staff.Any())
            {
                // Cashier doesn't exist, seed it first
                await SeedStaffWithAllPositionsAsync(context);
                cashierUser = await context.Users
                    .Include(u => u.Staff)
                    .FirstOrDefaultAsync(u => u.Email == "cashier@test.com" && u.IsDeleted == false);
            }

            var cashierStaff = cashierUser?.Staff?.FirstOrDefault();
            if (cashierStaff == null)
            {
                throw new InvalidOperationException("Test Cashier staff not found. Please run SeedStaffWithAllPositionsAsync first.");
            }

            // Get a customer
            var customer = await context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                await SeedTestCustomerAsync(context);
                customer = await context.Customers
                    .Include(c => c.User)
                    .FirstOrDefaultAsync();
            }

            // Ensure at least one Area/Table exists
            var table = await context.Tables
                .Include(t => t.Area)
                .FirstOrDefaultAsync();

            if (table == null)
            {
                Console.WriteLine("⚠️ No tables found. Creating sample area & table for cashier workflow test...");

                var area = new Area
                {
                    AreaName = "Khu chính",
                    Floor = 1,
                    Description = "Khu vực mặc định cho seeding"
                };
                await context.Areas.AddAsync(area);
                await context.SaveChangesAsync();

                var sampleTable = new Table
                {
                    TableNumber = "B01",
                    Capacity = 4,
                    Status = "Available",
                    AreaId = area.AreaId
                };
                await context.Tables.AddAsync(sampleTable);
                await context.SaveChangesAsync();

                table = sampleTable;
                Console.WriteLine("✅ Created sample area and table (B01).");
            }

            // Create or get a reservation for the table
            var reservation = await context.Reservations
                .Include(r => r.ReservationTables)
                .FirstOrDefaultAsync(r => r.ReservationTables.Any(rt => rt.TableId == table.TableId) &&
                                          r.Status == "Guest Seated");

            if (reservation == null)
            {
                reservation = new Reservation
                {
                    CustomerId = customer.CustomerId,
                    CustomerNameReservation = customer.User?.FullName ?? "Test Customer",
                    StaffId = cashierUser?.UserId, // StaffId actually stores UserId
                    ReservationDate = DateTime.UtcNow.Date,
                    TimeSlot = "Ca tối",
                    ReservationTime = DateTime.UtcNow.AddHours(-2),
                    NumberOfGuests = 4,
                    Status = "Guest Seated",
                    ArrivalAt = DateTime.UtcNow.AddHours(-2)
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
            }

            // 2. Get or create MenuItems for combos
            // Ensure there are enough available MenuItems
            var menuItems = await context.MenuItems
                .Where(m => m.IsAvailable == true)
                .OrderBy(m => m.MenuItemId)
                .Take(10)
                .ToListAsync();

            if (menuItems.Count < 5)
            {
                Console.WriteLine("⚠️ Not enough menu items. Creating sample menu items for cashier workflow...");

                var sampleMenuItems = new List<MenuItem>
                {
                    new MenuItem { Name = "Steak Sapa Signature", Price = 245000m, Description = "Steak nhập khẩu", CourseType = "MainCourse", IsAvailable = true },
                    new MenuItem { Name = "Trà sen Tuyết", Price = 45000m, Description = "Trà hoa sen đặc trưng", CourseType = "Beverage", IsAvailable = true },
                    new MenuItem { Name = "Lẩu cá hồi Fansipan", Price = 320000m, Description = "Lẩu cá hồi đặc biệt", CourseType = "MainCourse", IsAvailable = true },
                    new MenuItem { Name = "Rau tổng hợp", Price = 85000m, Description = "Set rau tươi sạch", CourseType = "SideDish", IsAvailable = true },
                    new MenuItem { Name = "Trà đào cam sả", Price = 55000m, Description = "Đồ uống giải khát", CourseType = "Beverage", IsAvailable = true }
                };

                await context.MenuItems.AddRangeAsync(sampleMenuItems);
                await context.SaveChangesAsync();

                menuItems = await context.MenuItems
                    .Where(m => m.IsAvailable == true)
                    .OrderBy(m => m.MenuItemId)
                    .Take(10)
                    .ToListAsync();

                Console.WriteLine("✅ Created sample menu items for cashier workflow.");
            }

            // 3. Seed Combos with ComboItems
            var combo1 = await context.Combos
                .FirstOrDefaultAsync(c => c.Name.Contains("Steak Dinner"));

            if (combo1 == null)
            {
                combo1 = new Combo
                {
                    Name = "Steak Dinner",
                    Description = "Combo bữa tối với steak và trà",
                    Price = 290000m, // Discounted price
                    IsAvailable = true,
                    ImageUrl = null
                };
                await context.Combos.AddAsync(combo1);
                await context.SaveChangesAsync();

                // Add ComboItems
                var steakItem = menuItems.FirstOrDefault(m => m.Name.Contains("Steak") || m.Name.Contains("Bò"));
                var teaItem = menuItems.FirstOrDefault(m => m.Name.Contains("Trà") || m.Name.Contains("Tea"));

                if (steakItem != null)
                {
                    await context.ComboItems.AddAsync(new ComboItem
                    {
                        ComboId = combo1.ComboId,
                        MenuItemId = steakItem.MenuItemId,
                        Quantity = 1
                    });
                }

                if (teaItem != null)
                {
                    await context.ComboItems.AddAsync(new ComboItem
                    {
                        ComboId = combo1.ComboId,
                        MenuItemId = teaItem.MenuItemId,
                        Quantity = 1
                    });
                }

                await context.SaveChangesAsync();
            }

            var combo2 = await context.Combos
                .FirstOrDefaultAsync(c => c.Name.Contains("Family Hotpot"));

            if (combo2 == null)
            {
                combo2 = new Combo
                {
                    Name = "Family Hotpot",
                    Description = "Combo lẩu gia đình",
                    Price = 450000m,
                    IsAvailable = true,
                    ImageUrl = null
                };
                await context.Combos.AddAsync(combo2);
                await context.SaveChangesAsync();

                // Add ComboItems
                var hotpotItem = menuItems.FirstOrDefault(m => m.Name.Contains("Lẩu") || m.Name.Contains("Hotpot"));
                var vegetableItem = menuItems.FirstOrDefault(m => m.Name.Contains("Rau") || m.Name.Contains("Vegetable"));
                var drinkItem = menuItems.FirstOrDefault(m => m.Name.Contains("Trà") || m.Name.Contains("Nước"));

                if (hotpotItem != null)
                {
                    await context.ComboItems.AddAsync(new ComboItem
                    {
                        ComboId = combo2.ComboId,
                        MenuItemId = hotpotItem.MenuItemId,
                        Quantity = 1
                    });
                }

                if (vegetableItem != null)
                {
                    await context.ComboItems.AddAsync(new ComboItem
                    {
                        ComboId = combo2.ComboId,
                        MenuItemId = vegetableItem.MenuItemId,
                        Quantity = 1
                    });
                }

                if (drinkItem != null)
                {
                    await context.ComboItems.AddAsync(new ComboItem
                    {
                        ComboId = combo2.ComboId,
                        MenuItemId = drinkItem.MenuItemId,
                        Quantity = 1
                    });
                }

                await context.SaveChangesAsync();
            }

            // 4. Create Order 1: Only menu items (no combo)
            // Status should be "waiting-confirmation" for cashier workflow testing
            var order1 = new Order
            {
                ReservationId = reservation.ReservationId,
                CustomerId = customer.CustomerId,
                OrderType = "DineIn",
                Status = OrderStatusConstants.WaitingConfirmation,
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                TotalAmount = 0m // Will be calculated later
            };
            await context.Orders.AddAsync(order1);
            await context.SaveChangesAsync();
            ordersCreated++;
            Console.WriteLine("✅ Created Order 1 (menu items only).");

            // Add OrderDetails for Order 1
            // Status should be "Pending" (not "Served") because order is waiting confirmation
            var order1Details = new List<OrderDetail>();
            var item1 = menuItems[0];
            var item2 = menuItems[1];
            var item3 = menuItems.Count > 2 ? menuItems[2] : menuItems[1];

            order1Details.Add(new OrderDetail
            {
                OrderId = order1.OrderId,
                MenuItemId = item1.MenuItemId,
                ComboId = null,
                Quantity = 2,
                UnitPrice = item1.Price,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow.AddMinutes(-29)
            });

            order1Details.Add(new OrderDetail
            {
                OrderId = order1.OrderId,
                MenuItemId = item2.MenuItemId,
                ComboId = null,
                Quantity = 1,
                UnitPrice = item2.Price,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow.AddMinutes(-29)
            });

            order1Details.Add(new OrderDetail
            {
                OrderId = order1.OrderId,
                MenuItemId = item3.MenuItemId,
                ComboId = null,
                Quantity = 1,
                UnitPrice = item3.Price,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow.AddMinutes(-29)
            });

            await context.OrderDetails.AddRangeAsync(order1Details);
            await context.SaveChangesAsync();

            // Calculate totals for Order 1
            var subtotal1 = order1Details.Sum(od => od.UnitPrice * od.Quantity);
            var vat1 = subtotal1 * 0.1m;
            var serviceFee1 = subtotal1 * 0.05m;
            var total1 = subtotal1 + vat1 + serviceFee1;

            order1.TotalAmount = total1;
            context.Orders.Update(order1);
            await context.SaveChangesAsync();

            // 5. Create Order 2: Has combo + individual items
            // This order is already confirmed by customer, ready for payment
            var order2 = new Order
            {
                ReservationId = reservation.ReservationId,
                CustomerId = customer.CustomerId,
                OrderType = "DineIn",
                Status = OrderStatusConstants.Confirmed,
                ConfirmedAt = DateTime.UtcNow.AddMinutes(-5),
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                TotalAmount = 0m // Will be calculated later
            };
            await context.Orders.AddAsync(order2);
            await context.SaveChangesAsync();
            ordersCreated++;
            Console.WriteLine("✅ Created Order 2 (combos + items).");

            // Add OrderDetails for Order 2 (with combo)
            // Order is confirmed, so items should be "Confirmed" status
            var order2Details = new List<OrderDetail>();

            // Add combo order details
            order2Details.Add(new OrderDetail
            {
                OrderId = order2.OrderId,
                MenuItemId = null,
                ComboId = combo1.ComboId,
                Quantity = 1,
                UnitPrice = combo1.Price,
                Status = "Confirmed",
                CreatedAt = DateTime.UtcNow.AddMinutes(-9)
            });

            order2Details.Add(new OrderDetail
            {
                OrderId = order2.OrderId,
                MenuItemId = null,
                ComboId = combo2.ComboId,
                Quantity = 1,
                UnitPrice = combo2.Price,
                Status = "Confirmed",
                CreatedAt = DateTime.UtcNow.AddMinutes(-9)
            });

            // Add individual items (no combo)
            var item4 = menuItems.Count > 3 ? menuItems[3] : menuItems[0];
            var item5 = menuItems.Count > 4 ? menuItems[4] : menuItems[1];

            order2Details.Add(new OrderDetail
            {
                OrderId = order2.OrderId,
                MenuItemId = item4.MenuItemId,
                ComboId = null,
                Quantity = 1,
                UnitPrice = item4.Price,
                Status = "Confirmed",
                CreatedAt = DateTime.UtcNow.AddMinutes(-9)
            });

            order2Details.Add(new OrderDetail
            {
                OrderId = order2.OrderId,
                MenuItemId = item5.MenuItemId,
                ComboId = null,
                Quantity = 2,
                UnitPrice = item5.Price,
                Status = "Confirmed",
                CreatedAt = DateTime.UtcNow.AddMinutes(-9)
            });

            await context.OrderDetails.AddRangeAsync(order2Details);
            await context.SaveChangesAsync();

            // Calculate totals for Order 2
            var subtotal2 = order2Details.Sum(od => od.UnitPrice * od.Quantity);
            var vat2 = subtotal2 * 0.1m;
            var serviceFee2 = subtotal2 * 0.05m;
            var total2 = subtotal2 + vat2 + serviceFee2;

            order2.TotalAmount = total2;
            context.Orders.Update(order2);
            await context.SaveChangesAsync();

            // 7. Create Order 3: Another waiting-confirmation order (different table)
            var table2 = await context.Tables
                .Where(t => t.TableId != table.TableId && t.Status == "Available")
                .FirstOrDefaultAsync();

            if (table2 == null)
            {
                table2 = new Table
                {
                    TableNumber = "B02",
                    Capacity = 6,
                    Status = "Available",
                    AreaId = table.AreaId
                };
                await context.Tables.AddAsync(table2);
                await context.SaveChangesAsync();
                Console.WriteLine("✅ Created table B02 for Order 3.");
            }

            var reservation2 = new Reservation
            {
                CustomerId = customer.CustomerId,
                CustomerNameReservation = customer.User?.FullName ?? "Test Customer",
                StaffId = cashierUser?.UserId,
                ReservationDate = DateTime.UtcNow.Date,
                TimeSlot = "Ca trưa",
                ReservationTime = DateTime.UtcNow.AddHours(-1),
                NumberOfGuests = 2,
                Status = "Guest Seated",
                ArrivalAt = DateTime.UtcNow.AddHours(-1)
            };
            await context.Reservations.AddAsync(reservation2);
            await context.SaveChangesAsync();

            await context.ReservationTables.AddAsync(new ReservationTable
            {
                ReservationId = reservation2.ReservationId,
                TableId = table2.TableId
            });
            await context.SaveChangesAsync();

            var order3 = new Order
            {
                ReservationId = reservation2.ReservationId,
                CustomerId = customer.CustomerId,
                OrderType = "DineIn",
                Status = OrderStatusConstants.WaitingConfirmation,
                CreatedAt = DateTime.UtcNow.AddMinutes(-20),
                TotalAmount = 0m
            };
            await context.Orders.AddAsync(order3);
            await context.SaveChangesAsync();
            ordersCreated++;
            Console.WriteLine("✅ Created Order 3 (waiting-confirmation).");

            // Add simple items for Order 3
            var order3Details = new List<OrderDetail>
            {
                new OrderDetail
                {
                    OrderId = order3.OrderId,
                    MenuItemId = menuItems[0].MenuItemId,
                    Quantity = 1,
                    UnitPrice = menuItems[0].Price,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-19)
                },
                new OrderDetail
                {
                    OrderId = order3.OrderId,
                    MenuItemId = menuItems[1].MenuItemId,
                    Quantity = 2,
                    UnitPrice = menuItems[1].Price,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-19)
                }
            };
            await context.OrderDetails.AddRangeAsync(order3Details);
            await context.SaveChangesAsync();

            var subtotal3 = order3Details.Sum(od => od.UnitPrice * od.Quantity);
            order3.TotalAmount = subtotal3 + (subtotal3 * 0.15m); // VAT + Service
            context.Orders.Update(order3);
            await context.SaveChangesAsync();

            // 8. Create Order 4: Confirmed order with combo (ready for payment)
            var order4 = new Order
            {
                ReservationId = reservation.ReservationId,
                CustomerId = customer.CustomerId,
                OrderType = "DineIn",
                Status = OrderStatusConstants.Confirmed,
                ConfirmedAt = DateTime.UtcNow.AddMinutes(-3),
                ConfirmedByStaffId = cashierStaff.StaffId,
                CreatedAt = DateTime.UtcNow.AddMinutes(-15),
                TotalAmount = 0m
            };
            await context.Orders.AddAsync(order4);
            await context.SaveChangesAsync();
            ordersCreated++;
            Console.WriteLine("✅ Created Order 4 (confirmed with combo).");

            var order4Details = new List<OrderDetail>
            {
                new OrderDetail
                {
                    OrderId = order4.OrderId,
                    ComboId = combo1.ComboId,
                    Quantity = 2,
                    UnitPrice = combo1.Price,
                    Status = "Confirmed",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-14)
                }
            };
            await context.OrderDetails.AddRangeAsync(order4Details);
            await context.SaveChangesAsync();

            var subtotal4 = order4Details.Sum(od => od.UnitPrice * od.Quantity);
            order4.TotalAmount = subtotal4 + (subtotal4 * 0.15m);
            context.Orders.Update(order4);
            await context.SaveChangesAsync();

            // 9. Create Order 5: Paid order (completed payment)
            var order5 = new Order
            {
                ReservationId = reservation2.ReservationId,
                CustomerId = customer.CustomerId,
                OrderType = "DineIn",
                Status = OrderStatusConstants.Paid,
                ConfirmedAt = DateTime.UtcNow.AddHours(-1),
                ConfirmedByStaffId = cashierStaff.StaffId,
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                TotalAmount = 0m
            };
            await context.Orders.AddAsync(order5);
            await context.SaveChangesAsync();
            ordersCreated++;
            Console.WriteLine("✅ Created Order 5 (paid).");

            var order5Details = new List<OrderDetail>
            {
                new OrderDetail
                {
                    OrderId = order5.OrderId,
                    MenuItemId = menuItems.Count > 2 ? menuItems[2].MenuItemId : menuItems[0].MenuItemId,
                    Quantity = 1,
                    UnitPrice = menuItems.Count > 2 ? menuItems[2].Price : menuItems[0].Price,
                    Status = "Served",
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                },
                new OrderDetail
                {
                    OrderId = order5.OrderId,
                    MenuItemId = menuItems.Count > 3 ? menuItems[3].MenuItemId : menuItems[1].MenuItemId,
                    Quantity = 1,
                    UnitPrice = menuItems.Count > 3 ? menuItems[3].Price : menuItems[1].Price,
                    Status = "Served",
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                }
            };
            await context.OrderDetails.AddRangeAsync(order5Details);
            await context.SaveChangesAsync();

            var subtotal5 = order5Details.Sum(od => od.UnitPrice * od.Quantity);
            order5.TotalAmount = subtotal5 + (subtotal5 * 0.15m);
            context.Orders.Update(order5);
            await context.SaveChangesAsync();

            // Create payment record for Order 5 (already paid)
            var payment5 = new Payment
            {
                OrderId = order5.OrderId,
                PaymentMethod = "Cash",
                Subtotal = subtotal5,
                DiscountAmount = 0m,
                Vatpercent = 10m,
                Vatamount = subtotal5 * 0.1m,
                FinalAmount = subtotal5 + (subtotal5 * 0.15m),
                PaymentDate = DateTime.UtcNow.AddMinutes(-30)
            };
            await context.Payments.AddAsync(payment5);
            await context.SaveChangesAsync();
            Console.WriteLine("ℹ️ Payment record created for Order 5 (already paid).");

            // 10. Create Order 6: Waiting-confirmation with many items (edge case)
            var order6 = new Order
            {
                ReservationId = reservation.ReservationId,
                CustomerId = customer.CustomerId,
                OrderType = "DineIn",
                Status = OrderStatusConstants.WaitingConfirmation,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                TotalAmount = 0m
            };
            await context.Orders.AddAsync(order6);
            await context.SaveChangesAsync();
            ordersCreated++;
            Console.WriteLine("✅ Created Order 6 (waiting-confirmation, many items).");

            var order6Details = new List<OrderDetail>();
            for (int i = 0; i < Math.Min(5, menuItems.Count); i++)
            {
                order6Details.Add(new OrderDetail
                {
                    OrderId = order6.OrderId,
                    MenuItemId = menuItems[i].MenuItemId,
                    Quantity = i + 1,
                    UnitPrice = menuItems[i].Price,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-4)
                });
            }
            await context.OrderDetails.AddRangeAsync(order6Details);
            await context.SaveChangesAsync();

            var subtotal6 = order6Details.Sum(od => od.UnitPrice * od.Quantity);
            order6.TotalAmount = subtotal6 + (subtotal6 * 0.15m);
            context.Orders.Update(order6);
            await context.SaveChangesAsync();

            // 11. Create Order 7: Old paid order (for history/reporting)
            var order7 = new Order
            {
                ReservationId = reservation.ReservationId,
                CustomerId = customer.CustomerId,
                OrderType = "DineIn",
                Status = OrderStatusConstants.Paid,
                ConfirmedAt = DateTime.UtcNow.AddDays(-1),
                ConfirmedByStaffId = cashierStaff.StaffId,
                CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(-1),
                TotalAmount = 0m
            };
            await context.Orders.AddAsync(order7);
            await context.SaveChangesAsync();
            ordersCreated++;
            Console.WriteLine("✅ Created Order 7 (old paid order).");

            var order7Details = new List<OrderDetail>
            {
                new OrderDetail
                {
                    OrderId = order7.OrderId,
                    ComboId = combo2.ComboId,
                    Quantity = 1,
                    UnitPrice = combo2.Price,
                    Status = "Served",
                    CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(-1)
                },
                new OrderDetail
                {
                    OrderId = order7.OrderId,
                    MenuItemId = menuItems[0].MenuItemId,
                    Quantity = 3,
                    UnitPrice = menuItems[0].Price,
                    Status = "Served",
                    CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(-1)
                }
            };
            await context.OrderDetails.AddRangeAsync(order7Details);
            await context.SaveChangesAsync();

            var subtotal7 = order7Details.Sum(od => od.UnitPrice * od.Quantity);
            order7.TotalAmount = subtotal7 + (subtotal7 * 0.15m);
            context.Orders.Update(order7);
            await context.SaveChangesAsync();

            var payment7 = new Payment
            {
                OrderId = order7.OrderId,
                PaymentMethod = "QR",
                Subtotal = subtotal7,
                DiscountAmount = 0m,
                Vatpercent = 10m,
                Vatamount = subtotal7 * 0.1m,
                FinalAmount = subtotal7 + (subtotal7 * 0.15m),
                PaymentDate = DateTime.UtcNow.AddDays(-1).AddMinutes(30)
            };
            await context.Payments.AddAsync(payment7);
            await context.SaveChangesAsync();
            Console.WriteLine("ℹ️ Payment record created for Order 7 (old paid order).");

            // Note: Payment records should NOT be created for pending orders
            // They will be created when cashier initiates payment via InitiatePaymentAsync()
            // This ensures proper payment workflow: Order → Customer Confirm → Initiate Payment → Process Payment

            Console.WriteLine($"🎯 Cashier workflow seeding finished. Orders created: {ordersCreated}.");
            Console.WriteLine("📊 Order Status Summary:");
            Console.WriteLine($"   - waiting-confirmation: Orders 1, 3, 6 (3 orders)");
            Console.WriteLine($"   - confirmed: Orders 2, 4 (2 orders)");
            Console.WriteLine($"   - paid: Orders 5, 7 (2 orders)");
            Console.WriteLine($"   Total: {ordersCreated} orders created for comprehensive testing.");
        }

    }
}


