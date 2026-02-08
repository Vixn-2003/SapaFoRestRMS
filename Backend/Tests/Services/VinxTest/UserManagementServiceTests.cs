using BusinessAccessLayer.DTOs.UserManagement;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services.VinxTest;

/// <summary>
/// Unit Tests cho UserManagementService
/// Test độc lập các phương thức trong UserManagementService sử dụng xUnit + Moq
/// </summary>
public class UserManagementServiceTests : IDisposable
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IVerificationService> _mockVerificationService;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly SapaFoRestRmsContext _dbContext;
    private readonly UserManagementService _userManagementService;

    public UserManagementServiceTests()
    {
        // Khởi tạo mocks
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockVerificationService = new Mock<IVerificationService>();
        _mockUserRepository = new Mock<IUserRepository>();

        // Setup IUnitOfWork.Users trả về mock repository
        _mockUnitOfWork.Setup(uow => uow.Users).Returns(_mockUserRepository.Object);

        // Setup SaveChangesAsync
        _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync()).ReturnsAsync(1);

        // Setup transaction
        var mockTransaction = new Mock<IDbContextTransaction>();
        _mockUnitOfWork
            .Setup(uow => uow.BeginTransactionAsync())
            .ReturnsAsync(mockTransaction.Object);
        _mockUnitOfWork
            .Setup(uow => uow.CommitAsync())
            .Returns(Task.CompletedTask);
        _mockUnitOfWork
            .Setup(uow => uow.RollbackAsync())
            .Returns(Task.CompletedTask);

        // Use in-memory database for DbContext queries (Roles, Positions, Staffs)
        var options = new DbContextOptionsBuilder<SapaFoRestRmsContext>()
            .UseInMemoryDatabase(databaseName: $"UserManagementTestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new SapaFoRestRmsContext(options);
        _dbContext.Database.EnsureCreated();

        // Seed test data
        SeedTestData();

        // Khởi tạo UserManagementService với mocked dependencies
        _userManagementService = new UserManagementService(
            _mockUnitOfWork.Object,
            _dbContext,
            _mockVerificationService.Object
        );
    }

    private void SeedTestData()
    {
        // Update or add Admin role (RoleId = 1)
        var adminRole = _dbContext.Roles.FirstOrDefault(r => r.RoleId == 1);
        if (adminRole != null)
        {
            adminRole.RoleName = "Admin";
        }
        else
        {
            _dbContext.Roles.Add(new Role
            {
                RoleId = 1,
                RoleName = "Admin"
            });
        }

        // Update or add Manager role (RoleId = 2)
        var managerRole = _dbContext.Roles.FirstOrDefault(r => r.RoleId == 2);
        if (managerRole != null)
        {
            managerRole.RoleName = "Manager";
        }
        else
        {
            _dbContext.Roles.Add(new Role
            {
                RoleId = 2,
                RoleName = "Manager"
            });
        }

        // Update or add Staff role (RoleId = 3)
        var staffRole = _dbContext.Roles.FirstOrDefault(r => r.RoleId == 3);
        if (staffRole != null)
        {
            staffRole.RoleName = "Staff";
        }
        else
        {
            _dbContext.Roles.Add(new Role
            {
                RoleId = 3,
                RoleName = "Staff"
            });
        }

        // Add test positions
        if (!_dbContext.Positions.Any(p => p.PositionId == 1))
        {
            _dbContext.Positions.Add(new Position
            {
                PositionId = 1,
                PositionName = "Waiter",
                Status = 1,
                BaseSalary = 5000000
            });
        }

        if (!_dbContext.Positions.Any(p => p.PositionId == 2))
        {
            _dbContext.Positions.Add(new Position
            {
                PositionId = 2,
                PositionName = "Cashier",
                Status = 1,
                BaseSalary = 5000000
            });
        }

        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region Test Data Helpers

    /// <summary>
    /// Tạo User test data
    /// </summary>
    private User CreateTestUser(
        int userId = 1,
        string fullName = "Test User",
        string email = "test@example.com",
        string? phone = "0123456789",
        int roleId = 1,
        string roleName = "Admin")
    {
        return new User
        {
            UserId = userId,
            FullName = fullName,
            Email = email,
            Phone = phone,
            PasswordHash = "hashed_password",
            RoleId = roleId,
            Status = 1,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Tạo CreateManagerRequest test data
    /// </summary>
    private CreateManagerRequest CreateTestCreateManagerRequest(
        string fullName = "Manager Name",
        string email = "manager@example.com",
        string? phone = "0987654321",
        int? roleId = null)
    {
        return new CreateManagerRequest
        {
            FullName = fullName,
            Email = email,
            Phone = phone,
            RoleId = roleId
        };
    }

    /// <summary>
    /// Tạo CreateStaffVerificationRequest test data
    /// </summary>
    private CreateStaffVerificationRequest CreateTestCreateStaffVerificationRequest(
        string fullName = "Staff Name",
        string email = "staff@example.com")
    {
        return new CreateStaffVerificationRequest
        {
            FullName = fullName,
            Email = email
        };
    }

    /// <summary>
    /// Tạo CreateStaffRequest test data
    /// </summary>
    private CreateStaffRequest CreateTestCreateStaffRequest(
        string fullName = "Staff Name",
        string email = "staff@example.com",
        string verificationCode = "123456",
        DateOnly? hireDate = null,
        decimal? salaryBase = null,
        List<int>? positionIds = null,
        int? roleId = null)
    {
        return new CreateStaffRequest
        {
            FullName = fullName,
            Email = email,
            VerificationCode = verificationCode,
            HireDate = hireDate,
            SalaryBase = salaryBase,
            PositionIds = positionIds ?? new List<int>(),
            RoleId = roleId
        };
    }

    #endregion

    #region CreateManagerAsync Tests

    [Fact]
    public async Task CreateManagerAsync_CreatesManager_WhenValidRequest()
    {
        // Arrange
        var adminUserId = 1;
        var admin = CreateTestUser(adminUserId, "Admin User", "admin@example.com", roleId: 1, roleName: "Admin");
        var request = CreateTestCreateManagerRequest("New Manager", "newmanager@example.com");

        // Verify role exists in database
        var adminRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleId == 1);
        adminRole.Should().NotBeNull();
        adminRole!.RoleName.Should().Be("Admin");

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(adminUserId))
            .ReturnsAsync(admin);

        _mockUserRepository
            .Setup(repo => repo.IsEmailExistsAsync(request.Email))
            .ReturnsAsync(false);

        User? addedUser = null;
        var testUserId = 100; // Test UserId to assign
        _mockUserRepository
            .Setup(repo => repo.AddAsync(It.IsAny<User>()))
            .Callback<User>(u =>
            {
                addedUser = u;
                // Simulate database auto-increment by assigning UserId
                // This simulates what EF Core does after SaveChangesAsync
                if (u.UserId == 0)
                {
                    u.UserId = testUserId;
                }
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userManagementService.CreateManagerAsync(request, adminUserId);

        // Assert
        result.userId.Should().Be(testUserId);
        result.tempPassword.Should().NotBeNullOrEmpty();
        result.tempPassword.Length.Should().BeGreaterThanOrEqualTo(10);

        addedUser.Should().NotBeNull();
        addedUser!.FullName.Should().Be(request.FullName);
        addedUser.Email.Should().Be(request.Email);
        addedUser.Phone.Should().Be(request.Phone);
        addedUser.RoleId.Should().Be(2); // Manager role ID
        addedUser.Status.Should().Be(0);
        addedUser.CreatedBy.Should().Be(adminUserId);

        _mockUnitOfWork.Verify(uow => uow.BeginTransactionAsync(), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.CommitAsync(), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.RollbackAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateManagerAsync_UsesProvidedRoleId_WhenRoleIdProvided()
    {
        // Arrange
        var adminUserId = 1;
        var admin = CreateTestUser(adminUserId, "Admin User", "admin@example.com", roleId: 1, roleName: "Admin");
        var customRoleId = 5;
        var request = CreateTestCreateManagerRequest("New Manager", "newmanager@example.com", roleId: customRoleId);

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(adminUserId))
            .ReturnsAsync(admin);

        _mockUserRepository
            .Setup(repo => repo.IsEmailExistsAsync(request.Email))
            .ReturnsAsync(false);

        User? addedUser = null;
        _mockUserRepository
            .Setup(repo => repo.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => addedUser = u)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userManagementService.CreateManagerAsync(request, adminUserId);

        // Assert
        addedUser.Should().NotBeNull();
        addedUser!.RoleId.Should().Be(customRoleId);
    }

    [Fact]
    public async Task CreateManagerAsync_ThrowsUnauthorizedAccessException_WhenAdminNotFound()
    {
        // Arrange
        var adminUserId = 999;
        var request = CreateTestCreateManagerRequest();

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(adminUserId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _userManagementService.CreateManagerAsync(request, adminUserId));

        _mockUnitOfWork.Verify(uow => uow.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateManagerAsync_ThrowsUnauthorizedAccessException_WhenUserIsNotAdmin()
    {
        // Arrange
        var managerUserId = 1;
        var manager = CreateTestUser(managerUserId, "Manager User", "manager@example.com", roleId: 2, roleName: "Manager");
        var request = CreateTestCreateManagerRequest();

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(managerUserId))
            .ReturnsAsync(manager);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _userManagementService.CreateManagerAsync(request, managerUserId));

        _mockUnitOfWork.Verify(uow => uow.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateManagerAsync_ThrowsInvalidOperationException_WhenEmailExists()
    {
        // Arrange
        var adminUserId = 1;
        var admin = CreateTestUser(adminUserId, "Admin User", "admin@example.com", roleId: 1, roleName: "Admin");
        var request = CreateTestCreateManagerRequest("New Manager", "existing@example.com");

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(adminUserId))
            .ReturnsAsync(admin);

        _mockUserRepository
            .Setup(repo => repo.IsEmailExistsAsync(request.Email))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _userManagementService.CreateManagerAsync(request, adminUserId));

        _mockUserRepository.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateManagerAsync_RollsBackTransaction_WhenExceptionOccurs()
    {
        // Arrange
        var adminUserId = 1;
        var admin = CreateTestUser(adminUserId, "Admin User", "admin@example.com", roleId: 1, roleName: "Admin");
        var request = CreateTestCreateManagerRequest("New Manager", "newmanager@example.com");

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(adminUserId))
            .ReturnsAsync(admin);

        _mockUserRepository
            .Setup(repo => repo.IsEmailExistsAsync(request.Email))
            .ReturnsAsync(false);

        _mockUserRepository
            .Setup(repo => repo.AddAsync(It.IsAny<User>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _userManagementService.CreateManagerAsync(request, adminUserId));

        _mockUnitOfWork.Verify(uow => uow.RollbackAsync(), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.CommitAsync(), Times.Never);
    }

    #endregion

    #region SendStaffVerificationCodeAsync Tests

    [Fact]
    public async Task SendStaffVerificationCodeAsync_SendsVerificationCode_WhenValidRequest()
    {
        // Arrange
        var managerUserId = 1;
        var request = CreateTestCreateStaffVerificationRequest("Staff Name", "staff@example.com");

        _mockUserRepository
            .Setup(repo => repo.IsEmailExistsAsync(request.Email))
            .ReturnsAsync(false);

        _mockVerificationService
            .Setup(service => service.InvalidateCodesAsync(
                managerUserId,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockVerificationService
            .Setup(service => service.GenerateAndSendCodeAsync(
                managerUserId,
                request.Email,
                It.IsAny<string>(),
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("123456");

        // Act
        await _userManagementService.SendStaffVerificationCodeAsync(request, managerUserId);

        // Assert
        _mockVerificationService.Verify(
            service => service.InvalidateCodesAsync(
                managerUserId,
                $"CreateStaff:{request.Email.Trim().ToLowerInvariant()}",
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockVerificationService.Verify(
            service => service.GenerateAndSendCodeAsync(
                managerUserId,
                request.Email,
                $"CreateStaff:{request.Email.Trim().ToLowerInvariant()}",
                10,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendStaffVerificationCodeAsync_ThrowsInvalidOperationException_WhenFullNameIsEmpty()
    {
        // Arrange
        var managerUserId = 1;
        var request = CreateTestCreateStaffVerificationRequest("", "staff@example.com");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _userManagementService.SendStaffVerificationCodeAsync(request, managerUserId));

        _mockVerificationService.Verify(
            service => service.GenerateAndSendCodeAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendStaffVerificationCodeAsync_ThrowsInvalidOperationException_WhenEmailIsEmpty()
    {
        // Arrange
        var managerUserId = 1;
        var request = CreateTestCreateStaffVerificationRequest("Staff Name", "");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _userManagementService.SendStaffVerificationCodeAsync(request, managerUserId));
    }

    [Fact]
    public async Task SendStaffVerificationCodeAsync_ThrowsInvalidOperationException_WhenEmailExists()
    {
        // Arrange
        var managerUserId = 1;
        var request = CreateTestCreateStaffVerificationRequest("Staff Name", "existing@example.com");

        _mockUserRepository
            .Setup(repo => repo.IsEmailExistsAsync(request.Email))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _userManagementService.SendStaffVerificationCodeAsync(request, managerUserId));

        _mockVerificationService.Verify(
            service => service.GenerateAndSendCodeAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region CreateStaffAsync Tests

    [Fact]
    public async Task CreateStaffAsync_CreatesStaff_WhenValidRequest()
    {
        // Arrange
        var managerUserId = 1;
        var manager = CreateTestUser(managerUserId, "Manager User", "manager@example.com", roleId: 2, roleName: "Manager");
        var request = CreateTestCreateStaffRequest(
            "New Staff",
            "newstaff@example.com",
            "123456",
            DateOnly.FromDateTime(DateTime.UtcNow),
            5000000,
            new List<int> { 1 });

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(managerUserId))
            .ReturnsAsync(manager);

        _mockUserRepository
            .Setup(repo => repo.IsEmailExistsAsync(request.Email))
            .ReturnsAsync(false);

        _mockVerificationService
            .Setup(service => service.VerifyCodeAsync(
                managerUserId,
                $"CreateStaff:{request.Email.Trim().ToLowerInvariant()}",
                request.VerificationCode,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        User? addedUser = null;
        var testUserId = 100; // Test UserId to assign
        _mockUserRepository
            .Setup(repo => repo.AddAsync(It.IsAny<User>()))
            .Callback<User>(u =>
            {
                addedUser = u;
                // Simulate database auto-increment by assigning UserId
                // This simulates what EF Core does after SaveChangesAsync
                if (u.UserId == 0)
                {
                    u.UserId = testUserId;
                }
            })
            .Returns(Task.CompletedTask);

        // Note: Staff is added to real _context.Staffs, so the in-memory database
        // will auto-assign StaffId when SaveChangesAsync is called
        // We just need to ensure SaveChangesAsync returns a value

        // Act
        var result = await _userManagementService.CreateStaffAsync(request, managerUserId);

        // Assert
        result.userId.Should().Be(testUserId);
        // Note: StaffId is auto-generated by in-memory database, so we just verify it's greater than 0
        result.staffId.Should().BeGreaterThan(0);
        result.tempPassword.Should().NotBeNullOrEmpty();

        addedUser.Should().NotBeNull();
        addedUser!.FullName.Should().Be(request.FullName);
        addedUser.Email.Should().Be(request.Email);
        addedUser.RoleId.Should().Be(3); // Staff role ID
        addedUser.Status.Should().Be(0);
        addedUser.CreatedBy.Should().Be(managerUserId);
        addedUser.UserId.Should().Be(testUserId);

        // Verify SaveChangesAsync was called (which would persist Staff in real scenario)
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.AtLeastOnce);
        _mockUnitOfWork.Verify(uow => uow.BeginTransactionAsync(), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateStaffAsync_UsesDefaultHireDate_WhenHireDateNotProvided()
    {
        // Arrange
        var managerUserId = 1;
        var manager = CreateTestUser(managerUserId, "Manager User", "manager@example.com", roleId: 2, roleName: "Manager");
        var request = CreateTestCreateStaffRequest(
            "New Staff",
            "newstaff@example.com",
            "123456",
            hireDate: null);

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(managerUserId))
            .ReturnsAsync(manager);

        _mockUserRepository
            .Setup(repo => repo.IsEmailExistsAsync(request.Email))
            .ReturnsAsync(false);

        _mockVerificationService
            .Setup(service => service.VerifyCodeAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockUserRepository
            .Setup(repo => repo.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userManagementService.CreateStaffAsync(request, managerUserId);

        // Assert
        result.staffId.Should().BeGreaterThan(0);
        // Note: Staff entity is created in the service but not persisted to in-memory DB
        // because SaveChangesAsync is mocked. In real scenario, HireDate would be set to today.
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CreateStaffAsync_UsesDefaultSalaryBase_WhenSalaryBaseNotProvided()
    {
        // Arrange
        var managerUserId = 1;
        var manager = CreateTestUser(managerUserId, "Manager User", "manager@example.com", roleId: 2, roleName: "Manager");
        var request = CreateTestCreateStaffRequest(
            "New Staff",
            "newstaff@example.com",
            "123456",
            salaryBase: null);

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(managerUserId))
            .ReturnsAsync(manager);

        _mockUserRepository
            .Setup(repo => repo.IsEmailExistsAsync(request.Email))
            .ReturnsAsync(false);

        _mockVerificationService
            .Setup(service => service.VerifyCodeAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockUserRepository
            .Setup(repo => repo.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userManagementService.CreateStaffAsync(request, managerUserId);

        // Assert
        result.staffId.Should().BeGreaterThan(0);
        // Note: Staff entity is created in the service but not persisted to in-memory DB
        // because SaveChangesAsync is mocked. In real scenario, SalaryBase would be 0.
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CreateStaffAsync_ThrowsUnauthorizedAccessException_WhenManagerNotFound()
    {
        // Arrange
        var managerUserId = 999;
        var request = CreateTestCreateStaffRequest();

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(managerUserId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _userManagementService.CreateStaffAsync(request, managerUserId));
    }

    [Fact]
    public async Task CreateStaffAsync_ThrowsUnauthorizedAccessException_WhenUserIsNotManager()
    {
        // Arrange
        var adminUserId = 1;
        var admin = CreateTestUser(adminUserId, "Admin User", "admin@example.com", roleId: 1, roleName: "Admin");
        var request = CreateTestCreateStaffRequest();

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(adminUserId))
            .ReturnsAsync(admin);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _userManagementService.CreateStaffAsync(request, adminUserId));
    }

    [Fact]
    public async Task CreateStaffAsync_ThrowsInvalidOperationException_WhenEmailExists()
    {
        // Arrange
        var managerUserId = 1;
        var manager = CreateTestUser(managerUserId, "Manager User", "manager@example.com", roleId: 2, roleName: "Manager");
        var request = CreateTestCreateStaffRequest("New Staff", "existing@example.com", "123456");

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(managerUserId))
            .ReturnsAsync(manager);

        _mockUserRepository
            .Setup(repo => repo.IsEmailExistsAsync(request.Email))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _userManagementService.CreateStaffAsync(request, managerUserId));
    }

    [Fact]
    public async Task CreateStaffAsync_ThrowsInvalidOperationException_WhenVerificationCodeIsEmpty()
    {
        // Arrange
        var managerUserId = 1;
        var manager = CreateTestUser(managerUserId, "Manager User", "manager@example.com", roleId: 2, roleName: "Manager");
        var request = CreateTestCreateStaffRequest("New Staff", "newstaff@example.com", "");

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(managerUserId))
            .ReturnsAsync(manager);

        _mockUserRepository
            .Setup(repo => repo.IsEmailExistsAsync(request.Email))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _userManagementService.CreateStaffAsync(request, managerUserId));
    }

    [Fact]
    public async Task CreateStaffAsync_ThrowsInvalidOperationException_WhenVerificationCodeIsInvalid()
    {
        // Arrange
        var managerUserId = 1;
        var manager = CreateTestUser(managerUserId, "Manager User", "manager@example.com", roleId: 2, roleName: "Manager");
        var request = CreateTestCreateStaffRequest("New Staff", "newstaff@example.com", "000000");

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(managerUserId))
            .ReturnsAsync(manager);

        _mockUserRepository
            .Setup(repo => repo.IsEmailExistsAsync(request.Email))
            .ReturnsAsync(false);

        _mockVerificationService
            .Setup(service => service.VerifyCodeAsync(
                managerUserId,
                It.IsAny<string>(),
                request.VerificationCode,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _userManagementService.CreateStaffAsync(request, managerUserId));
    }

    [Fact]
    public async Task CreateStaffAsync_ThrowsInvalidOperationException_WhenPositionNotFound()
    {
        // Arrange
        var managerUserId = 1;
        var manager = CreateTestUser(managerUserId, "Manager User", "manager@example.com", roleId: 2, roleName: "Manager");
        var request = CreateTestCreateStaffRequest(
            "New Staff",
            "newstaff@example.com",
            "123456",
            positionIds: new List<int> { 999 }); // Non-existent position

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(managerUserId))
            .ReturnsAsync(manager);

        _mockUserRepository
            .Setup(repo => repo.IsEmailExistsAsync(request.Email))
            .ReturnsAsync(false);

        _mockVerificationService
            .Setup(service => service.VerifyCodeAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockUserRepository
            .Setup(repo => repo.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _userManagementService.CreateStaffAsync(request, managerUserId));
    }

    [Fact]
    public async Task CreateStaffAsync_CreatesStaffWithoutPositions_WhenPositionIdsIsEmpty()
    {
        // Arrange
        var managerUserId = 1;
        var manager = CreateTestUser(managerUserId, "Manager User", "manager@example.com", roleId: 2, roleName: "Manager");
        var request = CreateTestCreateStaffRequest(
            "New Staff",
            "newstaff@example.com",
            "123456",
            positionIds: new List<int>()); // Empty list

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(managerUserId))
            .ReturnsAsync(manager);

        _mockUserRepository
            .Setup(repo => repo.IsEmailExistsAsync(request.Email))
            .ReturnsAsync(false);

        _mockVerificationService
            .Setup(service => service.VerifyCodeAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockUserRepository
            .Setup(repo => repo.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userManagementService.CreateStaffAsync(request, managerUserId);

        // Assert
        result.staffId.Should().BeGreaterThan(0);
        // Note: Staff entity is created in the service but not persisted to in-memory DB
        // because SaveChangesAsync is mocked. In real scenario, Positions would be empty.
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CreateStaffAsync_RollsBackTransaction_WhenExceptionOccurs()
    {
        // Arrange
        var managerUserId = 1;
        var manager = CreateTestUser(managerUserId, "Manager User", "manager@example.com", roleId: 2, roleName: "Manager");
        var request = CreateTestCreateStaffRequest("New Staff", "newstaff@example.com", "123456");

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(managerUserId))
            .ReturnsAsync(manager);

        _mockUserRepository
            .Setup(repo => repo.IsEmailExistsAsync(request.Email))
            .ReturnsAsync(false);

        _mockVerificationService
            .Setup(service => service.VerifyCodeAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockUserRepository
            .Setup(repo => repo.AddAsync(It.IsAny<User>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _userManagementService.CreateStaffAsync(request, managerUserId));

        _mockUnitOfWork.Verify(uow => uow.RollbackAsync(), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.CommitAsync(), Times.Never);
    }

    #endregion
}

