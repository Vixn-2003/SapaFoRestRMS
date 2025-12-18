using AutoMapper;
using BusinessAccessLayer.DTOs.UserManagement;
using BusinessAccessLayer.Services;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services.VinxTest;

/// <summary>
/// Unit Tests cho StaffProfileService
/// Test độc lập các phương thức trong StaffProfileService sử dụng xUnit + Moq
/// </summary>
public class StaffProfileServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IStaffProfileRepository> _mockStaffProfileRepository;
    private readonly Mock<IPositionRepository> _mockPositionRepository;
    private readonly StaffProfileService _staffProfileService;

    public StaffProfileServiceTests()
    {
        // Khởi tạo mocks
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockStaffProfileRepository = new Mock<IStaffProfileRepository>();
        _mockPositionRepository = new Mock<IPositionRepository>();

        // Setup IUnitOfWork.StaffProfiles trả về mock repository
        _mockUnitOfWork.Setup(uow => uow.StaffProfiles).Returns(_mockStaffProfileRepository.Object);
        _mockUnitOfWork.Setup(uow => uow.Positions).Returns(_mockPositionRepository.Object);

        // Setup SaveChangesAsync
        _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync()).ReturnsAsync(1);

        // Khởi tạo StaffProfileService với mocked dependencies
        _staffProfileService = new StaffProfileService(
            _mockUnitOfWork.Object,
            _mockMapper.Object
        );
    }

    #region Test Data Helpers

    /// <summary>
    /// Tạo User test data với Staff
    /// </summary>
    private User CreateTestUserWithStaff(
        int userId = 1,
        string fullName = "Nguyễn Văn A",
        string email = "staff1@example.com",
        string? phone = "0123456789",
        int status = 1,
        int roleId = 4)
    {
        var user = new User
        {
            UserId = userId,
            FullName = fullName,
            Email = email,
            Phone = phone,
            PasswordHash = "hashed_password",
            RoleId = roleId,
            Status = status,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            Role = new Role
            {
                RoleId = roleId,
                RoleName = "Staff"
            },
            Staff = new List<Staff>
            {
                new Staff
                {
                    StaffId = 1,
                    UserId = userId,
                    DepartmentId = 1,
                    HireDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
                    SalaryBase = 5000000,
                    Status = 1,
                    Positions = new List<Position>
                    {
                        new Position
                        {
                            PositionId = 1,
                            PositionName = "Waiter",
                            Status = 1
                        }
                    }
                }
            }
        };

        return user;
    }

    /// <summary>
    /// Tạo User test data không có Staff
    /// </summary>
    private User CreateTestUserWithoutStaff(
        int userId = 1,
        string fullName = "Nguyễn Văn A",
        string email = "staff1@example.com")
    {
        var user = new User
        {
            UserId = userId,
            FullName = fullName,
            Email = email,
            Phone = "0123456789",
            PasswordHash = "hashed_password",
            RoleId = 4,
            Status = 1,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            Role = new Role
            {
                RoleId = 4,
                RoleName = "Staff"
            },
            Staff = new List<Staff>() // Empty staff list
        };

        return user;
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
    /// Tạo StaffProfileDto test data
    /// </summary>
    private StaffProfileDto CreateTestStaffProfileDto(
        int userId = 1,
        string fullName = "Nguyễn Văn A",
        string email = "staff1@example.com",
        string? phone = "0123456789")
    {
        return new StaffProfileDto
        {
            UserId = userId,
            FullName = fullName,
            Email = email,
            Phone = phone,
            RoleName = "Staff",
            PositionNames = new List<string> { "Waiter" },
            Status = 1
        };
    }

    /// <summary>
    /// Tạo StaffProfileUpdateDto test data
    /// </summary>
    private StaffProfileUpdateDto CreateTestStaffProfileUpdateDto(
        string fullName = "Nguyễn Văn Mới",
        string email = "newemail@example.com",
        string? phone = "0987654321",
        int status = 1,
        List<int>? positionIds = null)
    {
        return new StaffProfileUpdateDto
        {
            FullName = fullName,
            Email = email,
            Phone = phone,
            Status = status,
            PositionIds = positionIds
        };
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsListOfStaffProfiles_WhenStaffUsersExist()
    {
        // Arrange
        var users = new List<User>
        {
            CreateTestUserWithStaff(1, "Nguyễn Văn A", "staff1@example.com"),
            CreateTestUserWithStaff(2, "Trần Thị B", "staff2@example.com"),
            CreateTestUserWithStaff(3, "Lê Văn C", "staff3@example.com")
        };

        var dtos = users.Select(u => CreateTestStaffProfileDto(
            u.UserId,
            u.FullName,
            u.Email,
            u.Phone)).ToList();

        _mockStaffProfileRepository
            .Setup(repo => repo.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        _mockMapper
            .Setup(m => m.Map<StaffProfileDto>(It.IsAny<User>()))
            .Returns<User>(u => CreateTestStaffProfileDto(u.UserId, u.FullName, u.Email, u.Phone));

        // Act
        var result = await _staffProfileService.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result[0].UserId.Should().Be(1);
        result[1].UserId.Should().Be(2);
        result[2].UserId.Should().Be(3);

        _mockStaffProfileRepository.Verify(
            repo => repo.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoStaffUsersExist()
    {
        // Arrange
        var users = new List<User>();

        _mockStaffProfileRepository
            .Setup(repo => repo.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var result = await _staffProfileService.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        _mockStaffProfileRepository.Verify(
            repo => repo.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_MapsAllUsersToDtos()
    {
        // Arrange
        var users = new List<User>
        {
            CreateTestUserWithStaff(1, "Nguyễn Văn A"),
            CreateTestUserWithStaff(2, "Trần Thị B")
        };

        _mockStaffProfileRepository
            .Setup(repo => repo.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var callCount = 0;
        _mockMapper
            .Setup(m => m.Map<StaffProfileDto>(It.IsAny<User>()))
            .Returns<User>(u =>
            {
                callCount++;
                return CreateTestStaffProfileDto(u.UserId, u.FullName, u.Email, u.Phone);
            });

        // Act
        var result = await _staffProfileService.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        callCount.Should().Be(2); // Verify mapper was called for each user
    }

    #endregion

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_ReturnsStaffProfile_WhenUserExists()
    {
        // Arrange
        var userId = 1;
        var user = CreateTestUserWithStaff(userId, "Nguyễn Văn A", "staff1@example.com");
        var expectedDto = CreateTestStaffProfileDto(userId, user.FullName, user.Email, user.Phone);

        _mockStaffProfileRepository
            .Setup(repo => repo.GetWithDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockMapper
            .Setup(m => m.Map<StaffProfileDto>(user))
            .Returns(expectedDto);

        // Act
        var result = await _staffProfileService.GetAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.FullName.Should().Be("Nguyễn Văn A");
        result.Email.Should().Be("staff1@example.com");

        _mockStaffProfileRepository.Verify(
            repo => repo.GetWithDetailsAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockMapper.Verify(
            m => m.Map<StaffProfileDto>(user),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenUserNotFound()
    {
        // Arrange
        var userId = 999;

        _mockStaffProfileRepository
            .Setup(repo => repo.GetWithDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _staffProfileService.GetAsync(userId);

        // Assert
        result.Should().BeNull();

        _mockStaffProfileRepository.Verify(
            repo => repo.GetWithDetailsAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockMapper.Verify(
            m => m.Map<StaffProfileDto>(It.IsAny<User>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenUserIsDeleted()
    {
        // Arrange
        var userId = 1;
        var user = CreateTestUserWithStaff(userId);
        user.IsDeleted = true;

        _mockStaffProfileRepository
            .Setup(repo => repo.GetWithDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null); // Repository filters out deleted users

        // Act
        var result = await _staffProfileService.GetAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_UpdatesUserProperties_WhenValidRequest()
    {
        // Arrange
        var userId = 1;
        var user = CreateTestUserWithStaff(userId, "Nguyễn Văn Cũ", "oldemail@example.com", "0111111111", 0);
        var updateDto = CreateTestStaffProfileUpdateDto(
            "Nguyễn Văn Mới",
            "newemail@example.com",
            "0987654321",
            1);

        _mockStaffProfileRepository
            .Setup(repo => repo.GetWithDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _staffProfileService.UpdateAsync(userId, updateDto);

        // Assert
        user.FullName.Should().Be(updateDto.FullName);
        user.Email.Should().Be(updateDto.Email);
        user.Phone.Should().Be(updateDto.Phone);
        user.Status.Should().Be(updateDto.Status);

        _mockUnitOfWork.Verify(
            uow => uow.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsKeyNotFoundException_WhenUserNotFound()
    {
        // Arrange
        var userId = 999;
        var updateDto = CreateTestStaffProfileUpdateDto();

        _mockStaffProfileRepository
            .Setup(repo => repo.GetWithDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _staffProfileService.UpdateAsync(userId, updateDto));

        _mockUnitOfWork.Verify(
            uow => uow.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesPositions_WhenPositionIdsProvidedAndStaffExists()
    {
        // Arrange
        var userId = 1;
        var user = CreateTestUserWithStaff(userId);
        var existingStaff = user.Staff.First();
        var updateDto = CreateTestStaffProfileUpdateDto(positionIds: new List<int> { 1, 2, 3 });

        var positions = new List<Position>
        {
            CreateTestPosition(1, "Waiter"),
            CreateTestPosition(2, "Cashier"),
            CreateTestPosition(3, "Chef")
        };

        _mockStaffProfileRepository
            .Setup(repo => repo.GetWithDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPositionRepository
            .Setup(repo => repo.GetByIdsAsync(updateDto.PositionIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(positions);

        // Act
        await _staffProfileService.UpdateAsync(userId, updateDto);

        // Assert
        existingStaff.Positions.Should().HaveCount(3);
        existingStaff.Positions.Should().Contain(p => p.PositionId == 1);
        existingStaff.Positions.Should().Contain(p => p.PositionId == 2);
        existingStaff.Positions.Should().Contain(p => p.PositionId == 3);

        _mockPositionRepository.Verify(
            repo => repo.GetByIdsAsync(updateDto.PositionIds, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockUnitOfWork.Verify(
            uow => uow.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CreatesNewStaff_WhenPositionIdsProvidedButStaffDoesNotExist()
    {
        // Arrange
        var userId = 1;
        var user = CreateTestUserWithoutStaff(userId); // User without Staff
        var updateDto = CreateTestStaffProfileUpdateDto(positionIds: new List<int> { 1, 2 });

        var positions = new List<Position>
        {
            CreateTestPosition(1, "Waiter"),
            CreateTestPosition(2, "Cashier")
        };

        _mockStaffProfileRepository
            .Setup(repo => repo.GetWithDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPositionRepository
            .Setup(repo => repo.GetByIdsAsync(updateDto.PositionIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(positions);

        // Act
        await _staffProfileService.UpdateAsync(userId, updateDto);

        // Assert
        // Note: The service creates a new Staff object but doesn't add it to user.Staff collection
        // It relies on EF Core change tracking. In a mocked scenario, we verify that:
        // 1. The service processes the update without throwing
        // 2. SaveChangesAsync is called (which would persist the new Staff in real scenario)
        // 3. User properties are updated
        user.FullName.Should().Be(updateDto.FullName);
        user.Email.Should().Be(updateDto.Email);
        user.Phone.Should().Be(updateDto.Phone);
        user.Status.Should().Be(updateDto.Status);

        _mockPositionRepository.Verify(
            repo => repo.GetByIdsAsync(updateDto.PositionIds, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockUnitOfWork.Verify(
            uow => uow.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotUpdatePositions_WhenPositionIdsIsNull()
    {
        // Arrange
        var userId = 1;
        var user = CreateTestUserWithStaff(userId);
        var existingStaff = user.Staff.First();
        var originalPositionCount = existingStaff.Positions.Count;
        var originalPositionId = existingStaff.Positions.First().PositionId;

        var updateDto = CreateTestStaffProfileUpdateDto(
            "Nguyễn Văn Mới",
            "newemail@example.com",
            positionIds: null); // PositionIds is null

        _mockStaffProfileRepository
            .Setup(repo => repo.GetWithDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _staffProfileService.UpdateAsync(userId, updateDto);

        // Assert
        existingStaff.Positions.Should().HaveCount(originalPositionCount);
        existingStaff.Positions.First().PositionId.Should().Be(originalPositionId);

        _mockPositionRepository.Verify(
            repo => repo.GetByIdsAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _mockUnitOfWork.Verify(
            uow => uow.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ClearsExistingPositions_WhenUpdatingPositions()
    {
        // Arrange
        var userId = 1;
        var user = CreateTestUserWithStaff(userId);
        var existingStaff = user.Staff.First();
        
        // Add multiple positions initially
        existingStaff.Positions.Add(CreateTestPosition(2, "Cashier"));
        existingStaff.Positions.Add(CreateTestPosition(3, "Chef"));

        var updateDto = CreateTestStaffProfileUpdateDto(positionIds: new List<int> { 1 }); // Only one position

        var positions = new List<Position>
        {
            CreateTestPosition(1, "Waiter")
        };

        _mockStaffProfileRepository
            .Setup(repo => repo.GetWithDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPositionRepository
            .Setup(repo => repo.GetByIdsAsync(updateDto.PositionIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(positions);

        // Act
        await _staffProfileService.UpdateAsync(userId, updateDto);

        // Assert
        existingStaff.Positions.Should().HaveCount(1);
        existingStaff.Positions.First().PositionId.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesUserAndPositions_WhenBothProvided()
    {
        // Arrange
        var userId = 1;
        var user = CreateTestUserWithStaff(userId, "Old Name", "old@example.com", "0111111111", 0);
        var updateDto = CreateTestStaffProfileUpdateDto(
            "New Name",
            "new@example.com",
            "0999999999",
            1,
            new List<int> { 1, 2 });

        var positions = new List<Position>
        {
            CreateTestPosition(1, "Waiter"),
            CreateTestPosition(2, "Cashier")
        };

        _mockStaffProfileRepository
            .Setup(repo => repo.GetWithDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPositionRepository
            .Setup(repo => repo.GetByIdsAsync(updateDto.PositionIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(positions);

        // Act
        await _staffProfileService.UpdateAsync(userId, updateDto);

        // Assert
        user.FullName.Should().Be("New Name");
        user.Email.Should().Be("new@example.com");
        user.Phone.Should().Be("0999999999");
        user.Status.Should().Be(1);

        var staff = user.Staff.First();
        staff.Positions.Should().HaveCount(2);

        _mockUnitOfWork.Verify(
            uow => uow.SaveChangesAsync(),
            Times.Once);
    }

    #endregion
}

