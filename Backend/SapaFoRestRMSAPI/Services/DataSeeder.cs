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
    }
}


