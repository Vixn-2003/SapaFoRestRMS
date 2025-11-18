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
    }
}


