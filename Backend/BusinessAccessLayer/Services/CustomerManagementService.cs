using AutoMapper;
using BusinessAccessLayer.Common.Pagination;
using BusinessAccessLayer.DTOs.CustomerManagement;
using BusinessAccessLayer.DTOs.CustomerProfile;
using BusinessAccessLayer.DTOs.Customers;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    /// <summary>
    /// Service implementation - Xử lý business logic và mapping DTOs
    /// </summary>
    public class CustomerManagementService : ICustomerManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAuditLogService _auditLogService;
        private readonly IConfiguration _configuration;
        private readonly ICloudinaryService _cloudinaryService;

        // VIP Criteria Configuration
        private const decimal DEFAULT_VIP_THRESHOLD = 500000m; // 500k VND
        private const int DEFAULT_AVG_PEOPLE_COUNT = 2;

        public CustomerManagementService(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            IAuditLogService auditLogService,
            IConfiguration configuration,
            ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _auditLogService = auditLogService;
            _configuration = configuration;
            _cloudinaryService = cloudinaryService;
        }

        /// <summary>
        /// UC145 - View List Customer
        /// </summary>
        public async Task<PagedResult<CustomerListItemDto>> GetCustomersAsync(
            CustomerFilterDto filter, 
            CancellationToken ct = default)
        {
            // Validate filter
            if (filter.Page <= 0) filter.Page = 1;
            if (filter.PageSize <= 0 || filter.PageSize > 100) filter.PageSize = 20;

            // Get customers query from repository
            var (query, totalCount) = await _unitOfWork.CustomerManagement.GetCustomersQueryAsync(
                filter.SearchKeyword,
                filter.IsVipOnly,
                filter.MinSpending,
                filter.MaxSpending,
                filter.MinVisits,
                filter.MaxVisits,
                filter.SortBy,
                filter.SortDirection,
                ct);

            // Calculate statistics for each customer
            var customersWithStats = query.Select(c => new
            {
                Customer = c,
                // ✅ FIX: Tính TotalSpending từ Transactions + ReservationDeposits
                // 1. Tính từ Transactions đã thanh toán thành công (Status = "Paid" và CompletedAt != null)
                TransactionSpending = c.Orders
                    .Where(o => o.Status == "Completed" || o.Status == "Paid")
                    .SelectMany(o => o.Transactions)
                    .Where(t => t.Status == "Paid" && t.CompletedAt.HasValue)
                    .Sum(t => (decimal?)t.Amount) ?? 0,
                // 2. Tính từ ReservationDeposits (tiền đặt cọc) - CHỈ tính từ Reservation có Status = "Completed"
                DepositSpending = c.Reservations
                    .Where(r => r.Status == "Completed")
                    .SelectMany(r => r.ReservationDeposits)
                    .Sum(d => (decimal?)d.Amount) ?? 0,
                // 3. Tổng chi tiêu = TransactionSpending + DepositSpending
                TotalSpending = (c.Orders
                    .Where(o => o.Status == "Completed" || o.Status == "Paid")
                    .SelectMany(o => o.Transactions)
                    .Where(t => t.Status == "Paid" && t.CompletedAt.HasValue)
                    .Sum(t => (decimal?)t.Amount) ?? 0) +
                    (c.Reservations
                    .Where(r => r.Status == "Completed")
                    .SelectMany(r => r.ReservationDeposits)
                    .Sum(d => (decimal?)d.Amount) ?? 0),
                TotalVisits = c.Orders
                    .Count(o => o.Status == "Completed" || o.Status == "Paid"),
                LastVisit = c.Orders
                    .Where(o => o.Status == "Completed" || o.Status == "Paid")
                    .Max(o => (DateTime?)o.CreatedAt)
            }).AsEnumerable(); // Execute query first

            // Apply spending filters (in memory after getting stats)
            if (filter.MinSpending.HasValue)
            {
                customersWithStats = customersWithStats.Where(x => x.TotalSpending >= filter.MinSpending.Value);
            }
            if (filter.MaxSpending.HasValue)
            {
                customersWithStats = customersWithStats.Where(x => x.TotalSpending <= filter.MaxSpending.Value);
            }
            if (filter.MinVisits.HasValue)
            {
                customersWithStats = customersWithStats.Where(x => x.TotalVisits >= filter.MinVisits.Value);
            }
            if (filter.MaxVisits.HasValue)
            {
                customersWithStats = customersWithStats.Where(x => x.TotalVisits <= filter.MaxVisits.Value);
            }

            // Apply sorting
            customersWithStats = filter.SortBy?.ToLower() switch
            {
                "fullname" => filter.SortDirection?.ToLower() == "asc" 
                    ? customersWithStats.OrderBy(x => x.Customer.User.FullName)
                    : customersWithStats.OrderByDescending(x => x.Customer.User.FullName),
                "totalspending" => filter.SortDirection?.ToLower() == "asc"
                    ? customersWithStats.OrderBy(x => x.TotalSpending)
                    : customersWithStats.OrderByDescending(x => x.TotalSpending),
                "totalvisits" => filter.SortDirection?.ToLower() == "asc"
                    ? customersWithStats.OrderBy(x => x.TotalVisits)
                    : customersWithStats.OrderByDescending(x => x.TotalVisits),
                "lastvisit" => filter.SortDirection?.ToLower() == "asc"
                    ? customersWithStats.OrderBy(x => x.LastVisit)
                    : customersWithStats.OrderByDescending(x => x.LastVisit),
                _ => customersWithStats.OrderByDescending(x => x.TotalSpending)
            };

            // Get filtered total count
            var filteredTotalCount = customersWithStats.Count();

            // Apply pagination
            var pageSize = filter.PageSize;
            var page = filter.Page;
            var totalPages = (int)Math.Ceiling(filteredTotalCount / (double)pageSize);

            var pagedData = customersWithStats
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Map to DTOs
            var items = pagedData.Select(x => new CustomerListItemDto
            {
                CustomerId = x.Customer.CustomerId,
                FullName = x.Customer.User?.FullName ?? "N/A",
                Phone = x.Customer.User?.Phone,
                Email = x.Customer.User?.Email,
                TotalSpending = x.TotalSpending,
                TotalVisits = x.TotalVisits,
                IsVip = x.Customer.IsVip,
                LastVisit = x.LastVisit,
                LoyaltyPoints = x.Customer.LoyaltyPoints,
                AverageSpendPerVisit = x.TotalVisits > 0 ? x.TotalSpending / x.TotalVisits : 0
            }).ToList();

            return new PagedResult<CustomerListItemDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = filteredTotalCount,
                TotalPages = totalPages,
                Data = items
            };
        }

        /// <summary>
        /// UC146 - View Customer Detail
        /// </summary>
        public async Task<CustomerDetailDto?> GetCustomerDetailAsync(
            int customerId, 
            CancellationToken ct = default)
        {
            // Get customer with all related data from repository
            var customer = await _unitOfWork.CustomerManagement.GetCustomerWithOrdersAsync(customerId, ct);
            if (customer == null)
                return null;

            // Map basic info
            var customerDetail = _mapper.Map<CustomerDetailDto>(customer);

            // Calculate statistics from domain entities
            var completedOrders = customer.Orders
                .Where(o => o.Status == "Completed" || o.Status == "Paid")
                .ToList();

            // ✅ FIX: Tính TotalSpending từ Transactions + ReservationDeposits
            // 1. Tính từ Transactions đã thanh toán thành công (Status = "Paid" và CompletedAt != null)
            var transactionSpending = completedOrders
                .SelectMany(o => o.Transactions)
                .Where(t => t.Status == "Paid" && t.CompletedAt.HasValue)
                .Sum(t => t.Amount);
            
            // 2. Tính từ ReservationDeposits (tiền đặt cọc) - CHỈ tính từ Reservation có Status = "Completed"
            var depositSpending = customer.Reservations
                .Where(r => r.Status == "Completed")
                .SelectMany(r => r.ReservationDeposits)
                .Sum(d => d.Amount);
            
            // 3. Tổng chi tiêu = TransactionSpending + DepositSpending
            customerDetail.TotalSpending = transactionSpending + depositSpending;
            
            customerDetail.TotalVisits = completedOrders.Count;
            
            customerDetail.LastVisit = completedOrders
                .Max(o => o.CreatedAt);

            customerDetail.AverageSpendPerVisit = customerDetail.TotalVisits > 0 
                ? customerDetail.TotalSpending / customerDetail.TotalVisits 
                : 0;

            // Calculate favorite dishes
            var dishStats = completedOrders
                .SelectMany(o => o.OrderDetails)
                .Where(od => od.MenuItemId.HasValue && od.MenuItem != null) // Filter null items
                .GroupBy(od => new { MenuItemId = od.MenuItemId.Value, od.MenuItem.Name })
                .Select(g => new FavoriteDishDto
                {
                    MenuItemId = g.Key.MenuItemId,
                    DishName = g.Key.Name,
                    OrderCount = g.Sum(x => x.Quantity),
                    TotalSpent = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderByDescending(x => x.OrderCount)
                .Take(3)
                .ToList();

            customerDetail.FavoriteDishes = dishStats;

            // Build order history
            var orderHistory = completedOrders
                .OrderByDescending(o => o.CreatedAt)
                .Take(20)
                .Select(o => new CustomerOrderSummaryDto
                {
                    OrderId = o.OrderId,
                    OrderDate = o.CreatedAt,
                    TotalAmount = o.TotalAmount ?? 0,
                    Status = o.Status ?? "Unknown",
                    NumberOfItems = o.OrderDetails.Sum(od => od.Quantity),
                    OrderType = o.OrderType,
                    PaymentId = o.Payments.FirstOrDefault()?.PaymentId
                })
                .ToList();

            customerDetail.OrderHistory = orderHistory;

            // Calculate monthly spending trend
            var monthlyTrend = completedOrders
                .Where(o => o.CreatedAt.HasValue && o.CreatedAt >= DateTime.UtcNow.AddMonths(-12))
                .GroupBy(o => new { 
                    Year = o.CreatedAt.Value.Year, 
                    Month = o.CreatedAt.Value.Month 
                })
                .Select(g => new MonthlySpendingDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalSpent = g.SelectMany(o => o.Payments).Sum(p => p.FinalAmount),
                    VisitCount = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();

            customerDetail.SpendingTrend = monthlyTrend;

            return customerDetail;
        }

        /// <summary>
        /// UC147 - Update VIP Status
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateVipStatusAsync(
            CustomerVipUpdateDto dto, 
            int managerId, 
            string? ipAddress = null,
            CancellationToken ct = default)
        {
            // Validate customer exists
            var customerExists = await _unitOfWork.CustomerManagement
                .CustomerExistsAsync(dto.CustomerId, ct);
            
            if (!customerExists)
                return (false, "Customer not found or has been deleted.");

            // Get current customer
            var customer = await _unitOfWork.CustomerManagement
                .GetCustomerByIdAsync(dto.CustomerId, ct);
            
            if (customer == null)
                return (false, "Customer not found.");

            var oldVipStatus = customer.IsVip;

            // Check VIP criteria if upgrading to VIP
            if (dto.IsVip && !dto.IsManualOverride)
            {
                var (meetsCriteria, avgAmount, reason) = await CheckVipCriteriaAsync(dto.CustomerId, ct);
                
                if (!meetsCriteria)
                {
                    return (false, $"Customer does not meet VIP criteria. {reason}. " +
                                  $"Average amount per person: {avgAmount:N0} VND. " +
                                  $"Required: {GetVipThreshold():N0} VND. " +
                                  $"Manager can override this by setting IsManualOverride = true.");
                }
            }

            // Update VIP status via repository
            var updateSuccess = await _unitOfWork.CustomerManagement
                .UpdateVipStatusAsync(dto.CustomerId, dto.IsVip, ct);

            if (!updateSuccess)
                return (false, "Failed to update VIP status.");

            // Log to AuditLog
            var metadata = JsonSerializer.Serialize(new
            {
                CustomerId = dto.CustomerId,
                CustomerName = customer.User?.FullName ?? "Unknown",
                OldVipStatus = oldVipStatus,
                NewVipStatus = dto.IsVip,
                IsManualOverride = dto.IsManualOverride,
                Reason = dto.Reason ?? "No reason provided",
                ManagerId = managerId
            });

            await _auditLogService.LogEventAsync(
                eventType: "vip_status_update",
                entityType: "Customer",
                entityId: dto.CustomerId,
                description: $"Manager {managerId} updated VIP status from {oldVipStatus} to {dto.IsVip} for Customer {dto.CustomerId}",
                metadata: metadata,
                userId: managerId,
                ipAddress: ipAddress,
                ct: ct
            );

            var action = dto.IsVip ? "upgraded to VIP" : "downgraded from VIP";
            return (true, $"Customer successfully {action}.");
        }

        /// <summary>
        /// Check VIP criteria based on spending statistics
        /// </summary>
        public virtual async Task<(bool MeetsCriteria, decimal AverageAmountPerPerson, string Reason)> CheckVipCriteriaAsync(
            int customerId, 
            CancellationToken ct = default)
        {
            // Get customer with orders
            var customer = await _unitOfWork.CustomerManagement
                .GetCustomerWithOrdersAsync(customerId, ct);

            if (customer == null)
                return (false, 0, "Customer not found.");

            var completedOrders = customer.Orders
                .Where(o => o.Status == "Completed" || o.Status == "Paid")
                .ToList();

            if (!completedOrders.Any())
            {
                return (false, 0, "Customer has no completed visits.");
            }

            var totalSpending = completedOrders.SelectMany(o => o.Payments).Sum(p => p.FinalAmount);
            var totalVisits = completedOrders.Count;

            // Calculate average amount per person
            var avgPeopleCount = GetAveragePeopleCount();
            var averageAmountPerPerson = totalSpending / totalVisits / avgPeopleCount;

            var vipThreshold = GetVipThreshold();
            var meetsCriteria = averageAmountPerPerson >= vipThreshold;

            var reason = meetsCriteria 
                ? $"Customer meets VIP criteria with average {averageAmountPerPerson:N0} VND per person."
                : $"Customer does not meet VIP criteria. Average: {averageAmountPerPerson:N0} VND, Required: {vipThreshold:N0} VND.";

            return (meetsCriteria, averageAmountPerPerson, reason);
        }

        private decimal GetVipThreshold()
        {
            var configValue = _configuration["CustomerManagement:VipThreshold"];
            if (decimal.TryParse(configValue, out var threshold))
                return threshold;
            
            return DEFAULT_VIP_THRESHOLD;
        }

        private int GetAveragePeopleCount()
        {
            var configValue = _configuration["CustomerManagement:AveragePeopleCount"];
            if (int.TryParse(configValue, out var count) && count > 0)
                return count;

            return DEFAULT_AVG_PEOPLE_COUNT;
        }

        /// <summary>
        /// Get customer profile information
        /// </summary>
        public async Task<CustomerProfileDto?> GetCustomerProfileAsync(int customerId, CancellationToken ct = default)
        {
            var customer = await _unitOfWork.CustomerManagement.GetCustomerByIdAsync(customerId, ct);
            if (customer == null || customer.User == null)
                return null;

            // Map from User entity since profile data is stored there
            var profileDto = new CustomerProfileDto
            {
                CustomerId = customer.CustomerId,
                FullName = customer.User.FullName,
                Email = customer.User.Email,
                Phone = customer.User.Phone,
                AvatarUrl = customer.User.AvatarUrl,
                LoyaltyPoints = customer.LoyaltyPoints.HasValue ? (decimal?)customer.LoyaltyPoints.Value : null,
                VipLevel = customer.IsVip ? "VIP" : "Regular",
                CreatedAt = customer.User.CreatedAt,
                UpdatedAt = customer.User.ModifiedAt
            };

            return profileDto;
        }

        /// <summary>
        /// Update customer profile information
        /// </summary>
        public async Task<CustomerProfileDto?> UpdateCustomerProfileAsync(int customerId, CustomerProfileUpdateRequest request, CancellationToken ct = default)
        {
            var customer = await _unitOfWork.CustomerManagement.GetCustomerByIdAsync(customerId, ct);
            if (customer == null || customer.User == null)
                return null;

            // Update User entity (where profile data is stored)
            var user = customer.User;
            user.FullName = request.FullName;

            // IMPORTANT: Email/Phone changes require OTP verification and are handled by dedicated endpoints.
            // Do NOT update Email/Phone here even if they are present in the multipart form.
            user.ModifiedAt = DateTime.UtcNow;

            // Handle avatar upload if provided
            if (request.AvatarFile != null && request.AvatarFile.Length > 0)
            {
                try
                {
                    // Upload to Cloudinary (configured in DI)
                    var uploadedUrl = await _cloudinaryService.UploadImageAsync(request.AvatarFile, folder: "avatars");
                    if (!string.IsNullOrWhiteSpace(uploadedUrl))
                    {
                        user.AvatarUrl = uploadedUrl;
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the update
                    await _auditLogService.LogEventAsync(
                        eventType: "AvatarUploadError",
                        entityType: "Customer",
                        entityId: customerId,
                        description: $"Failed to upload avatar: {ex.Message}",
                        userId: null,
                        ct: ct);
                }
            }
            else if (!string.IsNullOrEmpty(request.AvatarUrl))
            {
                user.AvatarUrl = request.AvatarUrl;
            }

            // Update customer entity (including User data)
            await _unitOfWork.CustomerManagement.UpdateCustomerAsync(customer, ct);

            // Log the profile update
            await _auditLogService.LogEventAsync(
                eventType: "CustomerProfileUpdated",
                entityType: "Customer",
                entityId: customerId,
                description: $"Customer profile updated: {request.FullName}",
                userId: null, // TODO: Get current user ID
                ct: ct);

            // Return updated profile
            return await GetCustomerProfileAsync(customerId, ct);
        }
    }
}
