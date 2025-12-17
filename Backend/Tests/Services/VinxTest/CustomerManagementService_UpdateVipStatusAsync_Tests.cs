using AutoMapper;
using BusinessAccessLayer.DTOs.CustomerManagement;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services.VinxTest;

/// <summary>
/// Unit Tests for CustomerManagementService.UpdateVipStatusAsync
/// </summary>
public class CustomerManagementService_UpdateVipStatusAsync_Tests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICustomerManagementRepository> _mockCustomerManagementRepository;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ICloudinaryService> _mockCloudinaryService;
    private readonly Mock<IConfigurationSection> _mockVipThresholdSection;
    private readonly Mock<IConfigurationSection> _mockAvgPeopleCountSection;

    public CustomerManagementService_UpdateVipStatusAsync_Tests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCustomerManagementRepository = new Mock<ICustomerManagementRepository>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockMapper = new Mock<IMapper>();
        _mockCloudinaryService = new Mock<ICloudinaryService>();
        _mockVipThresholdSection = new Mock<IConfigurationSection>();
        _mockAvgPeopleCountSection = new Mock<IConfigurationSection>();

        // Setup IUnitOfWork.CustomerManagement to return mock repository
        _mockUnitOfWork.Setup(uow => uow.CustomerManagement)
            .Returns(_mockCustomerManagementRepository.Object);

        // Setup configuration values
        _mockVipThresholdSection.Setup(s => s.Value).Returns("500000");
        _mockAvgPeopleCountSection.Setup(s => s.Value).Returns("2");

        _mockConfiguration.Setup(c => c["CustomerManagement:VipThreshold"])
            .Returns("500000");
        _mockConfiguration.Setup(c => c["CustomerManagement:AveragePeopleCount"])
            .Returns("2");
    }

    #region Test 1: Customer does NOT exist

    [Fact]
    public async Task UpdateVipStatusAsync_CustomerDoesNotExist_ReturnsFailure()
    {
        // Arrange
        var dto = new CustomerVipUpdateDto
        {
            CustomerId = 1,
            IsVip = true,
            IsManualOverride = false,
            Reason = "Test reason"
        };
        var managerId = 10;
        var ipAddress = "192.168.1.1";

        _mockCustomerManagementRepository
            .Setup(repo => repo.CustomerExistsAsync(dto.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new CustomerManagementService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockAuditLogService.Object,
            _mockConfiguration.Object,
            _mockCloudinaryService.Object
        );

        // Act
        var result = await service.UpdateVipStatusAsync(dto, managerId, ipAddress);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Customer not found or has been deleted.", result.Message);

        // Verify CustomerExistsAsync was called
        _mockCustomerManagementRepository.Verify(
            repo => repo.CustomerExistsAsync(dto.CustomerId, It.IsAny<CancellationToken>()),
            Times.Once
        );

        // Verify GetCustomerByIdAsync was NOT called
        _mockCustomerManagementRepository.Verify(
            repo => repo.GetCustomerByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        // Verify audit log was NOT called
        _mockAuditLogService.Verify(
            service => service.LogEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );
    }

    #endregion

    #region Test 2: Customer exists BUT GetCustomerByIdAsync returns null

    [Fact]
    public async Task UpdateVipStatusAsync_CustomerExistsButGetByIdReturnsNull_ReturnsFailure()
    {
        // Arrange
        var dto = new CustomerVipUpdateDto
        {
            CustomerId = 1,
            IsVip = true,
            IsManualOverride = false,
            Reason = "Test reason"
        };
        var managerId = 10;
        var ipAddress = "192.168.1.1";

        _mockCustomerManagementRepository
            .Setup(repo => repo.CustomerExistsAsync(dto.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockCustomerManagementRepository
            .Setup(repo => repo.GetCustomerByIdAsync(dto.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var service = new CustomerManagementService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockAuditLogService.Object,
            _mockConfiguration.Object,
            _mockCloudinaryService.Object
        );

        // Act
        var result = await service.UpdateVipStatusAsync(dto, managerId, ipAddress);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Customer not found.", result.Message);

        // Verify both methods were called
        _mockCustomerManagementRepository.Verify(
            repo => repo.CustomerExistsAsync(dto.CustomerId, It.IsAny<CancellationToken>()),
            Times.Once
        );

        _mockCustomerManagementRepository.Verify(
            repo => repo.GetCustomerByIdAsync(dto.CustomerId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    #endregion

    #region Test 3: Upgrade to VIP - criteria not met

    [Fact]
    public async Task UpdateVipStatusAsync_UpgradeToVip_CriteriaNotMet_ReturnsFailure()
    {
        // Arrange
        var dto = new CustomerVipUpdateDto
        {
            CustomerId = 1,
            IsVip = true,
            IsManualOverride = false,
            Reason = "Test reason"
        };
        var managerId = 10;
        var ipAddress = "192.168.1.1";

        var customer = new Customer
        {
            CustomerId = 1,
            IsVip = false,
            User = new User
            {
                UserId = 1,
                FullName = "Test Customer"
            }
        };

        // Create customer with orders that result in average < 500000 per person
        // 1 visit with 600,000 VND = 600,000 / 1 / 2 = 300,000 per person (below threshold)
        var customerWithOrders = new Customer
        {
            CustomerId = 1,
            IsVip = false,
            User = new User
            {
                UserId = 1,
                FullName = "Test Customer"
            },
            Orders = new List<Order>
            {
                new Order
                {
                    OrderId = 1,
                    CustomerId = 1,
                    Status = "Completed",
                    CreatedAt = DateTime.UtcNow,
                    Payments = new List<Payment>
                    {
                        new Payment
                        {
                            PaymentId = 1,
                            OrderId = 1,
                            FinalAmount = 600000m
                        }
                    }
                }
            }
        };

        _mockCustomerManagementRepository
            .Setup(repo => repo.CustomerExistsAsync(dto.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockCustomerManagementRepository
            .Setup(repo => repo.GetCustomerByIdAsync(dto.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _mockCustomerManagementRepository
            .Setup(repo => repo.GetCustomerWithOrdersAsync(dto.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customerWithOrders);

        var service = new CustomerManagementService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockAuditLogService.Object,
            _mockConfiguration.Object,
            _mockCloudinaryService.Object
        );

        // Act
        var result = await service.UpdateVipStatusAsync(dto, managerId, ipAddress);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("does not meet VIP criteria", result.Message);

        // Verify GetCustomerWithOrdersAsync was called (by CheckVipCriteriaAsync)
        _mockCustomerManagementRepository.Verify(
            repo => repo.GetCustomerWithOrdersAsync(dto.CustomerId, It.IsAny<CancellationToken>()),
            Times.Once
        );

        // Verify UpdateVipStatusAsync was NOT called on repository
        _mockCustomerManagementRepository.Verify(
            repo => repo.UpdateVipStatusAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    #endregion

    #region Test 4: Upgrade to VIP WITH manual override

    [Fact]
    public async Task UpdateVipStatusAsync_UpgradeToVip_WithManualOverride_ReturnsSuccess()
    {
        // Arrange
        var dto = new CustomerVipUpdateDto
        {
            CustomerId = 1,
            IsVip = true,
            IsManualOverride = true,
            Reason = "Manager override"
        };
        var managerId = 10;
        var ipAddress = "192.168.1.1";

        var customer = new Customer
        {
            CustomerId = 1,
            IsVip = false,
            User = new User
            {
                UserId = 1,
                FullName = "Test Customer"
            }
        };

        _mockCustomerManagementRepository
            .Setup(repo => repo.CustomerExistsAsync(dto.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockCustomerManagementRepository
            .Setup(repo => repo.GetCustomerByIdAsync(dto.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _mockCustomerManagementRepository
            .Setup(repo => repo.UpdateVipStatusAsync(dto.CustomerId, dto.IsVip, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockAuditLogService
            .Setup(service => service.LogEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            ))
            .Returns(Task.CompletedTask);

        var service = new CustomerManagementService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockAuditLogService.Object,
            _mockConfiguration.Object,
            _mockCloudinaryService.Object
        );

        // Act
        var result = await service.UpdateVipStatusAsync(dto, managerId, ipAddress);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("upgraded to VIP", result.Message);

        // Verify UpdateVipStatusAsync was called
        _mockCustomerManagementRepository.Verify(
            repo => repo.UpdateVipStatusAsync(dto.CustomerId, dto.IsVip, It.IsAny<CancellationToken>()),
            Times.Once
        );

        // Verify audit log was called
        _mockAuditLogService.Verify(
            service => service.LogEventAsync(
                "vip_status_update",
                "Customer",
                dto.CustomerId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                managerId,
                ipAddress,
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    #endregion

    #region Test 5: Upgrade to VIP, criteria met

    [Fact]
    public async Task UpdateVipStatusAsync_UpgradeToVip_CriteriaMet_ReturnsSuccess()
    {
        // Arrange
        var dto = new CustomerVipUpdateDto
        {
            CustomerId = 1,
            IsVip = true,
            IsManualOverride = false,
            Reason = "Meets criteria"
        };
        var managerId = 10;
        var ipAddress = "192.168.1.1";

        var customer = new Customer
        {
            CustomerId = 1,
            IsVip = false,
            User = new User
            {
                UserId = 1,
                FullName = "Test Customer"
            }
        };

        // Create customer with orders that result in average >= 500000 per person
        // 1 visit with 1,200,000 VND = 1,200,000 / 1 / 2 = 600,000 per person (above threshold)
        var customerWithOrders = new Customer
        {
            CustomerId = 1,
            IsVip = false,
            User = new User
            {
                UserId = 1,
                FullName = "Test Customer"
            },
            Orders = new List<Order>
            {
                new Order
                {
                    OrderId = 1,
                    CustomerId = 1,
                    Status = "Completed",
                    CreatedAt = DateTime.UtcNow,
                    Payments = new List<Payment>
                    {
                        new Payment
                        {
                            PaymentId = 1,
                            OrderId = 1,
                            FinalAmount = 1200000m
                        }
                    }
                }
            }
        };

        _mockCustomerManagementRepository
            .Setup(repo => repo.CustomerExistsAsync(dto.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockCustomerManagementRepository
            .Setup(repo => repo.GetCustomerByIdAsync(dto.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _mockCustomerManagementRepository
            .Setup(repo => repo.GetCustomerWithOrdersAsync(dto.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customerWithOrders);

        _mockCustomerManagementRepository
            .Setup(repo => repo.UpdateVipStatusAsync(dto.CustomerId, dto.IsVip, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockAuditLogService
            .Setup(service => service.LogEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            ))
            .Returns(Task.CompletedTask);

        var service = new CustomerManagementService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockAuditLogService.Object,
            _mockConfiguration.Object,
            _mockCloudinaryService.Object
        );

        // Act
        var result = await service.UpdateVipStatusAsync(dto, managerId, ipAddress);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("upgraded to VIP", result.Message);

        // Verify GetCustomerWithOrdersAsync was called (by CheckVipCriteriaAsync)
        _mockCustomerManagementRepository.Verify(
            repo => repo.GetCustomerWithOrdersAsync(dto.CustomerId, It.IsAny<CancellationToken>()),
            Times.Once
        );

        // Verify UpdateVipStatusAsync was called
        _mockCustomerManagementRepository.Verify(
            repo => repo.UpdateVipStatusAsync(dto.CustomerId, dto.IsVip, It.IsAny<CancellationToken>()),
            Times.Once
        );

        // Verify audit log was called
        _mockAuditLogService.Verify(
            service => service.LogEventAsync(
                "vip_status_update",
                "Customer",
                dto.CustomerId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                managerId,
                ipAddress,
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    #endregion

    #region Test 6: Downgrade VIP

    [Fact]
    public async Task UpdateVipStatusAsync_DowngradeVip_ReturnsSuccess()
    {
        // Arrange
        var dto = new CustomerVipUpdateDto
        {
            CustomerId = 1,
            IsVip = false,
            IsManualOverride = false,
            Reason = "Downgrade request"
        };
        var managerId = 10;
        var ipAddress = "192.168.1.1";

        var customer = new Customer
        {
            CustomerId = 1,
            IsVip = true, // Old status is VIP
            User = new User
            {
                UserId = 1,
                FullName = "Test Customer"
            }
        };

        _mockCustomerManagementRepository
            .Setup(repo => repo.CustomerExistsAsync(dto.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockCustomerManagementRepository
            .Setup(repo => repo.GetCustomerByIdAsync(dto.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _mockCustomerManagementRepository
            .Setup(repo => repo.UpdateVipStatusAsync(dto.CustomerId, dto.IsVip, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockAuditLogService
            .Setup(service => service.LogEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            ))
            .Returns(Task.CompletedTask);

        var service = new CustomerManagementService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockAuditLogService.Object,
            _mockConfiguration.Object,
            _mockCloudinaryService.Object
        );

        // Act
        var result = await service.UpdateVipStatusAsync(dto, managerId, ipAddress);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("downgraded", result.Message);

        // Verify CheckVipCriteriaAsync was NOT called (only for upgrades)
        // Verify UpdateVipStatusAsync was called
        _mockCustomerManagementRepository.Verify(
            repo => repo.UpdateVipStatusAsync(dto.CustomerId, dto.IsVip, It.IsAny<CancellationToken>()),
            Times.Once
        );

        // Verify audit log was called
        _mockAuditLogService.Verify(
            service => service.LogEventAsync(
                "vip_status_update",
                "Customer",
                dto.CustomerId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                managerId,
                ipAddress,
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    #endregion

    #region Test 7: Repository update fails

    [Fact]
    public async Task UpdateVipStatusAsync_RepositoryUpdateFails_ReturnsFailure()
    {
        // Arrange
        var dto = new CustomerVipUpdateDto
        {
            CustomerId = 1,
            IsVip = false,
            IsManualOverride = false,
            Reason = "Test reason"
        };
        var managerId = 10;
        var ipAddress = "192.168.1.1";

        var customer = new Customer
        {
            CustomerId = 1,
            IsVip = true,
            User = new User
            {
                UserId = 1,
                FullName = "Test Customer"
            }
        };

        _mockCustomerManagementRepository
            .Setup(repo => repo.CustomerExistsAsync(dto.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockCustomerManagementRepository
            .Setup(repo => repo.GetCustomerByIdAsync(dto.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _mockCustomerManagementRepository
            .Setup(repo => repo.UpdateVipStatusAsync(dto.CustomerId, dto.IsVip, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Update fails

        var service = new CustomerManagementService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockAuditLogService.Object,
            _mockConfiguration.Object,
            _mockCloudinaryService.Object
        );

        // Act
        var result = await service.UpdateVipStatusAsync(dto, managerId, ipAddress);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Failed to update VIP status.", result.Message);

        // Verify UpdateVipStatusAsync was called
        _mockCustomerManagementRepository.Verify(
            repo => repo.UpdateVipStatusAsync(dto.CustomerId, dto.IsVip, It.IsAny<CancellationToken>()),
            Times.Once
        );

        // Verify audit log was NOT called (update failed)
        _mockAuditLogService.Verify(
            service => service.LogEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );
    }

    #endregion

    #region Test 8: Audit log is written on success

    [Fact]
    public async Task UpdateVipStatusAsync_Success_AuditLogIsWritten()
    {
        // Arrange
        var dto = new CustomerVipUpdateDto
        {
            CustomerId = 1,
            IsVip = true,
            IsManualOverride = true,
            Reason = "Manager decision"
        };
        var managerId = 10;
        var ipAddress = "192.168.1.1";

        var customer = new Customer
        {
            CustomerId = 1,
            IsVip = false,
            User = new User
            {
                UserId = 1,
                FullName = "Test Customer"
            }
        };

        _mockCustomerManagementRepository
            .Setup(repo => repo.CustomerExistsAsync(dto.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockCustomerManagementRepository
            .Setup(repo => repo.GetCustomerByIdAsync(dto.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _mockCustomerManagementRepository
            .Setup(repo => repo.UpdateVipStatusAsync(dto.CustomerId, dto.IsVip, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockAuditLogService
            .Setup(service => service.LogEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            ))
            .Returns(Task.CompletedTask);

        var service = new CustomerManagementService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockAuditLogService.Object,
            _mockConfiguration.Object,
            _mockCloudinaryService.Object
        );

        // Act
        var result = await service.UpdateVipStatusAsync(dto, managerId, ipAddress);

        // Assert
        Assert.True(result.Success);

        // Verify audit log was called ONCE with correct parameters
        _mockAuditLogService.Verify(
            service => service.LogEventAsync(
                "vip_status_update",
                "Customer",
                dto.CustomerId,
                It.Is<string>(desc => desc.Contains($"Manager {managerId}") && desc.Contains("updated VIP status")),
                It.Is<string>(meta => meta.Contains("\"CustomerId\":1") && meta.Contains("\"ManagerId\":10")),
                managerId,
                ipAddress,
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    #endregion
}

