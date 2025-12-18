using AutoMapper;
using BusinessAccessLayer.Common.Pagination;
using BusinessAccessLayer.DTOs.Positions;
using BusinessAccessLayer.DTOs.Staff;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services.VinxTest;

/// <summary>
/// Unit Tests cho StaffManagementService
/// Test độc lập các phương thức trong StaffManagementService sử dụng xUnit + Moq
/// </summary>
public class StaffManagementServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IStaffManagementRepository> _mockStaffManagementRepository;
    private readonly Mock<IPositionRepository> _mockPositionRepository;
    private readonly StaffManagementService _staffManagementService;

    public StaffManagementServiceTests()
    {
        // Khởi tạo mocks
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockStaffManagementRepository = new Mock<IStaffManagementRepository>();
        _mockPositionRepository = new Mock<IPositionRepository>();

        // Setup IUnitOfWork.StaffManagement trả về mock repository
        _mockUnitOfWork.Setup(uow => uow.StaffManagement).Returns(_mockStaffManagementRepository.Object);
        _mockUnitOfWork.Setup(uow => uow.Positions).Returns(_mockPositionRepository.Object);

        // Setup SaveChangesAsync
        _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync()).ReturnsAsync(1);

        // Khởi tạo StaffManagementService với mocked dependencies
        _staffManagementService = new StaffManagementService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockAuditLogService.Object,
            _mockConfiguration.Object
        );
    }

    #region Test Data Helpers

    /// <summary>
    /// Tạo Staff test data
    /// </summary>
    private Staff CreateTestStaff(
        int staffId = 1,
        int userId = 1,
        string fullName = "Nguyễn Văn A",
        string email = "staff1@example.com",
        string? phone = "0123456789",
        int? departmentId = 1,
        decimal baseSalary = 5000000,
        int status = 1,
        DateOnly? hireDate = null)
    {
        var user = new User
        {
            UserId = userId,
            FullName = fullName,
            Email = email,
            Phone = phone,
            PasswordHash = "hashed_password",
            RoleId = 4, // Staff role
            Status = 1,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            Role = new Role
            {
                RoleId = 4,
                RoleName = "Staff"
            }
        };

        var department = departmentId.HasValue ? new Department
        {
            DepartmentId = departmentId.Value,
            Name = $"Department {departmentId.Value}"
        } : null;

        var staff = new Staff
        {
            StaffId = staffId,
            UserId = userId,
            DepartmentId = departmentId,
            HireDate = hireDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            SalaryBase = baseSalary,
            Status = status,
            User = user,
            Department = department,
            Positions = new List<Position>
            {
                new Position
                {
                    PositionId = 1,
                    PositionName = "Waiter",
                    Status = 1
                }
            }
        };

        return staff;
    }

    /// <summary>
    /// Tạo Position test data
    /// </summary>
    private Position CreateTestPosition(int positionId = 1, string positionName = "Waiter", int status = 1)
    {
        return new Position
        {
            PositionId = positionId,
            PositionName = positionName,
            Status = status,
            BaseSalary = 5000000
        };
    }

    /// <summary>
    /// Tạo StaffFilterDto test data
    /// </summary>
    private StaffFilterDto CreateTestFilter(
        string? searchKeyword = null,
        string? position = null,
        int? status = null,
        int? departmentId = null,
        string sortBy = "HireDate",
        string sortDirection = "desc",
        int page = 1,
        int pageSize = 20)
    {
        return new StaffFilterDto
        {
            SearchKeyword = searchKeyword,
            Position = position,
            Status = status,
            DepartmentId = departmentId,
            SortBy = sortBy,
            SortDirection = sortDirection,
            Page = page,
            PageSize = pageSize
        };
    }

    #endregion

    #region GetStaffListAsync Tests

    [Fact]
    public async Task GetStaffListAsync_ReturnsPaginatedStaff_WhenValidRequest()
    {
        // Arrange
        var filter = CreateTestFilter(page: 1, pageSize: 10);
        var testStaff = new List<Staff>
        {
            CreateTestStaff(1, 1, "Nguyễn Văn A", "staff1@example.com"),
            CreateTestStaff(2, 2, "Trần Thị B", "staff2@example.com"),
            CreateTestStaff(3, 3, "Lê Văn C", "staff3@example.com")
        };

        var queryableStaff = testStaff.AsQueryable();
        var totalCount = testStaff.Count;

        _mockStaffManagementRepository
            .Setup(repo => repo.GetStaffQueryAsync(
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((queryableStaff, totalCount));

        // Act
        var result = await _staffManagementService.GetStaffListAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(3);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(3);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetStaffListAsync_AppliesPagination_WhenMultiplePages()
    {
        // Arrange
        var filter = CreateTestFilter(page: 2, pageSize: 2);
        var testStaff = new List<Staff>
        {
            CreateTestStaff(1, 1, "Nguyễn Văn A"),
            CreateTestStaff(2, 2, "Trần Thị B"),
            CreateTestStaff(3, 3, "Lê Văn C"),
            CreateTestStaff(4, 4, "Phạm Thị D")
        };

        var queryableStaff = testStaff.AsQueryable();
        var totalCount = testStaff.Count;

        _mockStaffManagementRepository
            .Setup(repo => repo.GetStaffQueryAsync(
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((queryableStaff, totalCount));

        // Act
        var result = await _staffManagementService.GetStaffListAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(2);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.TotalCount.Should().Be(4);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetStaffListAsync_SortsByNameAscending_WhenSortByIsName()
    {
        // Arrange
        var filter = CreateTestFilter(sortBy: "name", sortDirection: "asc");
        var testStaff = new List<Staff>
        {
            CreateTestStaff(1, 1, "Nguyễn Văn A"),
            CreateTestStaff(2, 2, "Trần Thị B"),
            CreateTestStaff(3, 3, "Lê Văn C")
        };

        var queryableStaff = testStaff.AsQueryable();
        var totalCount = testStaff.Count;

        _mockStaffManagementRepository
            .Setup(repo => repo.GetStaffQueryAsync(
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((queryableStaff, totalCount));

        // Act
        var result = await _staffManagementService.GetStaffListAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(3);
        result.Data[0].FullName.Should().Be("Lê Văn C");
        result.Data[1].FullName.Should().Be("Nguyễn Văn A");
        result.Data[2].FullName.Should().Be("Trần Thị B");
    }

    [Fact]
    public async Task GetStaffListAsync_FiltersByManagerDepartment_WhenManagerDepartmentIdProvided()
    {
        // Arrange
        var filter = CreateTestFilter();
        var managerDepartmentId = 1;
        var testStaff = new List<Staff>
        {
            CreateTestStaff(1, 1, "Nguyễn Văn A", departmentId: 1),
            CreateTestStaff(2, 2, "Trần Thị B", departmentId: 1),
            CreateTestStaff(3, 3, "Lê Văn C", departmentId: 2) // Different department
        };

        var queryableStaff = testStaff.Where(s => s.DepartmentId == managerDepartmentId).AsQueryable();
        var totalCount = 2;

        _mockStaffManagementRepository
            .Setup(repo => repo.GetStaffQueryAsync(
                managerDepartmentId,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((queryableStaff, totalCount));

        // Act
        var result = await _staffManagementService.GetStaffListAsync(filter, managerDepartmentId);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(2);
        result.Data.All(s => s.DepartmentId == managerDepartmentId).Should().BeTrue();
    }

    [Fact]
    public async Task GetStaffListAsync_ValidatesPageSize_WhenPageSizeExceedsMax()
    {
        // Arrange
        var filter = CreateTestFilter(pageSize: 150); // Exceeds max of 100
        var testStaff = new List<Staff> { CreateTestStaff(1, 1) };
        var queryableStaff = testStaff.AsQueryable();

        _mockStaffManagementRepository
            .Setup(repo => repo.GetStaffQueryAsync(
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((queryableStaff, 1));

        // Act
        var result = await _staffManagementService.GetStaffListAsync(filter);

        // Assert
        result.Should().NotBeNull();
        result.PageSize.Should().Be(20); // Should default to 20
    }

    #endregion

    #region GetStaffDetailAsync Tests

    [Fact]
    public async Task GetStaffDetailAsync_ReturnsStaffDetail_WhenStaffExists()
    {
        // Arrange
        var staffId = 1;
        var staff = CreateTestStaff(staffId, 1, "Nguyễn Văn A", "staff1@example.com");
        staff.Positions = new List<Position>
        {
            CreateTestPosition(1, "Waiter"),
            CreateTestPosition(2, "Cashier")
        };

        _mockStaffManagementRepository
            .Setup(repo => repo.GetStaffByIdAsync(staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);

        // Act
        var result = await _staffManagementService.GetStaffDetailAsync(staffId);

        // Assert
        result.Should().NotBeNull();
        result!.StaffId.Should().Be(staffId);
        result.FullName.Should().Be("Nguyễn Văn A");
        result.Email.Should().Be("staff1@example.com");
        result.Positions.Should().HaveCount(2);
        result.Positions[0].PositionName.Should().Be("Waiter");
        result.Positions[1].PositionName.Should().Be("Cashier");
    }

    [Fact]
    public async Task GetStaffDetailAsync_ReturnsNull_WhenStaffNotFound()
    {
        // Arrange
        var staffId = 999;

        _mockStaffManagementRepository
            .Setup(repo => repo.GetStaffByIdAsync(staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Staff?)null);

        // Act
        var result = await _staffManagementService.GetStaffDetailAsync(staffId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStaffDetailAsync_ReturnsNull_WhenUserIsNull()
    {
        // Arrange
        var staffId = 1;
        var staff = new Staff
        {
            StaffId = staffId,
            UserId = 1,
            User = null! // User is null
        };

        _mockStaffManagementRepository
            .Setup(repo => repo.GetStaffByIdAsync(staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);

        // Act
        var result = await _staffManagementService.GetStaffDetailAsync(staffId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateStaffAsync Tests

    [Fact]
    public async Task CreateStaffAsync_CreatesStaff_WhenValidRequest()
    {
        // Arrange
        var dto = new StaffCreateDto
        {
            FullName = "Nguyễn Văn Mới",
            Email = "newstaff@example.com",
            Phone = "0987654321",
            BaseSalary = 6000000,
            HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
            RoleId = 4,
            PositionId = 1, // Single position only
            Password = "Password123!"
        };

        var position = CreateTestPosition(1, "Waiter");

        var createdStaff = CreateTestStaff(1, 1, dto.FullName, dto.Email, dto.Phone, null, dto.BaseSalary);
        createdStaff.Positions = new List<Position> { position };

        _mockStaffManagementRepository
            .Setup(repo => repo.EmailExistsAsync(dto.Email, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockPositionRepository
            .Setup(repo => repo.GetByIdAsync(dto.PositionId))
            .ReturnsAsync(position);

        _mockStaffManagementRepository
            .Setup(repo => repo.CreateStaffAsync(It.IsAny<Staff>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdStaff);

        _mockAuditLogService
            .Setup(service => service.LogEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _staffManagementService.CreateStaffAsync(dto, createdBy: 10, ipAddress: "127.0.0.1");

        // Assert
        result.Success.Should().BeTrue();
        result.StaffId.Should().Be(1);
        result.Message.Should().Contain("created successfully");
        result.Message.Should().Contain("Password");

        _mockStaffManagementRepository.Verify(
            repo => repo.CreateStaffAsync(It.IsAny<Staff>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _mockAuditLogService.Verify(
            service => service.LogEventAsync(
                "staff_created",
                "Staff",
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                10,
                "127.0.0.1",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateStaffAsync_GeneratesPassword_WhenPasswordNotProvided()
    {
        // Arrange
        var dto = new StaffCreateDto
        {
            FullName = "Nguyễn Văn Mới",
            Email = "newstaff@example.com",
            Phone = "0987654321",
            BaseSalary = 6000000,
            HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
            RoleId = 4,
            PositionId = 1, // Single position only
            Password = null // No password provided
        };

        var position = CreateTestPosition(1, "Waiter");
        var createdStaff = CreateTestStaff(1, 1, dto.FullName, dto.Email);

        _mockStaffManagementRepository
            .Setup(repo => repo.EmailExistsAsync(dto.Email, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockPositionRepository
            .Setup(repo => repo.GetByIdAsync(dto.PositionId))
            .ReturnsAsync(position);

        _mockStaffManagementRepository
            .Setup(repo => repo.CreateStaffAsync(It.IsAny<Staff>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdStaff);

        _mockAuditLogService
            .Setup(service => service.LogEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _staffManagementService.CreateStaffAsync(dto, createdBy: 10);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Password"); // Generated password should be in message
    }

    [Fact]
    public async Task CreateStaffAsync_ReturnsError_WhenEmailExists()
    {
        // Arrange
        var dto = new StaffCreateDto
        {
            FullName = "Nguyễn Văn Mới",
            Email = "existing@example.com",
            BaseSalary = 6000000,
            HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
            RoleId = 4,
            PositionId = 1 // Single position only
        };

        _mockStaffManagementRepository
            .Setup(repo => repo.EmailExistsAsync(dto.Email, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _staffManagementService.CreateStaffAsync(dto, createdBy: 10);

        // Assert
        result.Success.Should().BeFalse();
        result.StaffId.Should().BeNull();
        result.Message.Should().Contain("Email already exists");

        _mockStaffManagementRepository.Verify(
            repo => repo.CreateStaffAsync(It.IsAny<Staff>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateStaffAsync_ReturnsError_WhenInvalidPositions()
    {
        // Arrange
        var dto = new StaffCreateDto
        {
            FullName = "Nguyễn Văn Mới",
            Email = "newstaff@example.com",
            BaseSalary = 6000000,
            HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
            RoleId = 4,
            PositionId = 999 // Invalid position ID (doesn't exist)
        };

        _mockStaffManagementRepository
            .Setup(repo => repo.EmailExistsAsync(dto.Email, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockPositionRepository
            .Setup(repo => repo.GetByIdAsync(dto.PositionId))
            .ReturnsAsync((Position?)null); // Position not found

        // Act
        var result = await _staffManagementService.CreateStaffAsync(dto, createdBy: 10);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("invalid");

        _mockStaffManagementRepository.Verify(
            repo => repo.CreateStaffAsync(It.IsAny<Staff>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region UpdateStaffAsync Tests

    [Fact]
    public async Task UpdateStaffAsync_UpdatesStaff_WhenValidRequest()
    {
        // Arrange
        var staffId = 1;
        var existingStaff = CreateTestStaff(staffId, 1, "Nguyễn Văn Cũ", "staff1@example.com");
        existingStaff.Positions = new List<Position> { CreateTestPosition(1, "Waiter") };

        var dto = new StaffUpdateDto
        {
            StaffId = staffId,
            FullName = "Nguyễn Văn Mới",
            Phone = "0987654321",
            BaseSalary = 7000000,
            Status = 1,
            PositionId = 2, // Single position only (changed from Waiter to Cashier)
            AvatarUrl = "https://example.com/avatar.jpg"
        };

        var newPosition = CreateTestPosition(2, "Cashier");

        _mockStaffManagementRepository
            .Setup(repo => repo.GetStaffByIdAsync(staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStaff);

        _mockPositionRepository
            .Setup(repo => repo.GetByIdAsync(dto.PositionId))
            .ReturnsAsync(newPosition);

        _mockStaffManagementRepository
            .Setup(repo => repo.UpdateStaffAsync(It.IsAny<Staff>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockAuditLogService
            .Setup(service => service.LogEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _staffManagementService.UpdateStaffAsync(dto, modifiedBy: 10, ipAddress: "127.0.0.1");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("updated successfully");

        existingStaff.User.FullName.Should().Be(dto.FullName);
        existingStaff.User.Phone.Should().Be(dto.Phone);
        existingStaff.SalaryBase.Should().Be(dto.BaseSalary);
        existingStaff.Status.Should().Be(dto.Status);

        _mockAuditLogService.Verify(
            service => service.LogEventAsync(
                "staff_updated",
                "Staff",
                staffId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                10,
                "127.0.0.1",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStaffAsync_ReturnsError_WhenStaffNotFound()
    {
        // Arrange
        var dto = new StaffUpdateDto
        {
            StaffId = 999,
            FullName = "Nguyễn Văn Mới",
            BaseSalary = 7000000,
            Status = 1,
            PositionId = 1 // Single position only
        };

        _mockStaffManagementRepository
            .Setup(repo => repo.GetStaffByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Staff?)null);

        // Act
        var result = await _staffManagementService.UpdateStaffAsync(dto, modifiedBy: 10);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");

        _mockStaffManagementRepository.Verify(
            repo => repo.UpdateStaffAsync(It.IsAny<Staff>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStaffAsync_ReturnsError_WhenInvalidPositions()
    {
        // Arrange
        var staffId = 1;
        var existingStaff = CreateTestStaff(staffId, 1);

        var dto = new StaffUpdateDto
        {
            StaffId = staffId,
            FullName = "Nguyễn Văn Mới",
            BaseSalary = 7000000,
            Status = 1,
            PositionId = 999 // Invalid position ID (doesn't exist)
        };

        _mockStaffManagementRepository
            .Setup(repo => repo.GetStaffByIdAsync(staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStaff);

        _mockPositionRepository
            .Setup(repo => repo.GetByIdAsync(dto.PositionId))
            .ReturnsAsync((Position?)null); // Position not found

        // Act
        var result = await _staffManagementService.UpdateStaffAsync(dto, modifiedBy: 10);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("invalid");

        _mockStaffManagementRepository.Verify(
            repo => repo.UpdateStaffAsync(It.IsAny<Staff>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region DeactivateStaffAsync Tests

    [Fact]
    public async Task DeactivateStaffAsync_DeactivatesStaff_WhenValidRequest()
    {
        // Arrange
        var staffId = 1;
        var staff = CreateTestStaff(staffId, 1, "Nguyễn Văn A");

        var dto = new StaffDeactivateDto
        {
            StaffId = staffId,
            Reason = "Resigned"
        };

        _mockStaffManagementRepository
            .Setup(repo => repo.GetStaffByIdAsync(staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);

        _mockStaffManagementRepository
            .Setup(repo => repo.DeactivateStaffAsync(staffId, dto.Reason, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockAuditLogService
            .Setup(service => service.LogEventAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _staffManagementService.DeactivateStaffAsync(dto, deletedBy: 10, ipAddress: "127.0.0.1");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("deactivated successfully");

        _mockAuditLogService.Verify(
            service => service.LogEventAsync(
                "staff_deactivated",
                "Staff",
                staffId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                10,
                "127.0.0.1",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeactivateStaffAsync_ReturnsError_WhenStaffNotFound()
    {
        // Arrange
        var dto = new StaffDeactivateDto
        {
            StaffId = 999,
            Reason = "Resigned"
        };

        _mockStaffManagementRepository
            .Setup(repo => repo.GetStaffByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Staff?)null);

        // Act
        var result = await _staffManagementService.DeactivateStaffAsync(dto, deletedBy: 10);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");

        _mockStaffManagementRepository.Verify(
            repo => repo.DeactivateStaffAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeactivateStaffAsync_ReturnsError_WhenDeactivationFails()
    {
        // Arrange
        var staffId = 1;
        var staff = CreateTestStaff(staffId, 1);

        var dto = new StaffDeactivateDto
        {
            StaffId = staffId,
            Reason = "Resigned"
        };

        _mockStaffManagementRepository
            .Setup(repo => repo.GetStaffByIdAsync(staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staff);

        _mockStaffManagementRepository
            .Setup(repo => repo.DeactivateStaffAsync(staffId, dto.Reason, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _staffManagementService.DeactivateStaffAsync(dto, deletedBy: 10);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Failed");
    }

    #endregion

    #region GetActivePositionsAsync Tests

    [Fact]
    public async Task GetActivePositionsAsync_ReturnsActivePositions()
    {
        // Arrange
        var positions = new List<Position>
        {
            CreateTestPosition(1, "Waiter", 1),
            CreateTestPosition(2, "Cashier", 1),
            CreateTestPosition(3, "Chef", 1)
        };

        var positionDtos = positions.Select(p => new PositionDto
        {
            PositionId = p.PositionId,
            PositionName = p.PositionName,
            Status = p.Status,
            BaseSalary = p.BaseSalary
        }).ToList();

        _mockStaffManagementRepository
            .Setup(repo => repo.GetActivePositionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(positions);

        _mockMapper
            .Setup(m => m.Map<List<PositionDto>>(positions))
            .Returns(positionDtos);

        // Act
        var result = await _staffManagementService.GetActivePositionsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result[0].PositionName.Should().Be("Waiter");
    }

    #endregion

    #region CanManagerManageStaffAsync Tests

    [Fact]
    public async Task CanManagerManageStaffAsync_ReturnsTrue_WhenManagerAndStaffInSameDepartment()
    {
        // Arrange
        var managerId = 10;
        var staffId = 1;
        var departmentId = 1;

        var managerStaff = CreateTestStaff(10, managerId, "Manager", departmentId: departmentId);
        var targetStaff = CreateTestStaff(staffId, 1, "Staff", departmentId: departmentId);

        _mockStaffManagementRepository
            .Setup(repo => repo.GetStaffByUserIdAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(managerStaff);

        _mockStaffManagementRepository
            .Setup(repo => repo.GetStaffByIdAsync(staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetStaff);

        // Act
        var result = await _staffManagementService.CanManagerManageStaffAsync(managerId, staffId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanManagerManageStaffAsync_ReturnsFalse_WhenManagerAndStaffInDifferentDepartments()
    {
        // Arrange
        var managerId = 10;
        var staffId = 1;

        var managerStaff = CreateTestStaff(10, managerId, "Manager", departmentId: 1);
        var targetStaff = CreateTestStaff(staffId, 1, "Staff", departmentId: 2);

        _mockStaffManagementRepository
            .Setup(repo => repo.GetStaffByUserIdAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(managerStaff);

        _mockStaffManagementRepository
            .Setup(repo => repo.GetStaffByIdAsync(staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetStaff);

        // Act
        var result = await _staffManagementService.CanManagerManageStaffAsync(managerId, staffId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanManagerManageStaffAsync_ReturnsFalse_WhenManagerNotFound()
    {
        // Arrange
        var managerId = 999;
        var staffId = 1;

        _mockStaffManagementRepository
            .Setup(repo => repo.GetStaffByUserIdAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Staff?)null);

        // Act
        var result = await _staffManagementService.CanManagerManageStaffAsync(managerId, staffId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanManagerManageStaffAsync_ReturnsFalse_WhenManagerHasNoDepartment()
    {
        // Arrange
        var managerId = 10;
        var staffId = 1;

        var managerStaff = CreateTestStaff(10, managerId, "Manager", departmentId: null);

        _mockStaffManagementRepository
            .Setup(repo => repo.GetStaffByUserIdAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(managerStaff);

        // Act
        var result = await _staffManagementService.CanManagerManageStaffAsync(managerId, staffId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanManagerManageStaffAsync_ReturnsFalse_WhenTargetStaffNotFound()
    {
        // Arrange
        var managerId = 10;
        var staffId = 999;

        var managerStaff = CreateTestStaff(10, managerId, "Manager", departmentId: 1);

        _mockStaffManagementRepository
            .Setup(repo => repo.GetStaffByUserIdAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(managerStaff);

        _mockStaffManagementRepository
            .Setup(repo => repo.GetStaffByIdAsync(staffId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Staff?)null);

        // Act
        var result = await _staffManagementService.CanManagerManageStaffAsync(managerId, staffId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}

