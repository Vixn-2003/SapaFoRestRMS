using AutoMapper;
using BusinessAccessLayer.DTOs.Users;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
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

namespace Tests.Services;

/// <summary>
/// Unit Tests cho UserService
/// Test độc lập các phương thức trong UserService sử dụng xUnit + Moq
/// </summary>
public class UserServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        // Khởi tạo mocks
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockEmailService = new Mock<IEmailService>();
        _mockUserRepository = new Mock<IUserRepository>();

        // Setup IUnitOfWork.Users trả về mock repository
        _mockUnitOfWork.Setup(uow => uow.Users).Returns(_mockUserRepository.Object);

        // Khởi tạo UserService với mocked dependencies
        _userService = new UserService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockRoleRepository.Object,
            _mockEmailService.Object
        );
    }

    #region Test Data Helpers

    /// <summary>
    /// Tạo danh sách User test data (in-memory)
    /// </summary>
    private List<User> CreateTestUsers()
    {
        return new List<User>
        {
            new User
            {
                UserId = 1,
                FullName = "Nguyễn Văn A",
                Email = "user1@example.com",
                Phone = "0123456789",
                PasswordHash = "hashed_password_1",
                RoleId = 2, // Admin
                Status = 1,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new User
            {
                UserId = 2,
                FullName = "Trần Thị B",
                Email = "user2@example.com",
                Phone = "0987654321",
                PasswordHash = "hashed_password_2",
                RoleId = 3, // Manager
                Status = 1,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new User
            {
                UserId = 3,
                FullName = "Lê Văn C",
                Email = "user3@example.com",
                Phone = "0111222333",
                PasswordHash = "hashed_password_3",
                RoleId = 4, // Staff
                Status = 0,
                IsDeleted = true, // User đã bị xóa
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                DeletedAt = DateTime.UtcNow.AddDays(-1)
            },
            new User
            {
                UserId = 4,
                FullName = "Phạm Thị D",
                Email = "user4@example.com",
                Phone = "0444555666",
                PasswordHash = "hashed_password_4",
                RoleId = 5, // Customer
                Status = 1,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            }
        };
    }

    /// <summary>
    /// Tạo Role test data
    /// </summary>
    private Role CreateTestRole(int roleId, string roleName)
    {
        return new Role
        {
            RoleId = roleId,
            RoleName = roleName
        };
    }

    /// <summary>
    /// Map User sang UserDto (mock AutoMapper behavior)
    /// </summary>
    private UserDto MapUserToDto(User user, string roleName)
    {
        return new UserDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            RoleId = user.RoleId,
            RoleName = roleName,
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            IsDeleted = user.IsDeleted
        };
    }

    #endregion

    #region Test 1: GetAllAsync_ReturnsActiveUsers

    [Fact]
    public async Task GetAllAsync_ReturnsActiveUsers_ShouldFilterDeletedUsers()
    {
        // Arrange
        var testUsers = CreateTestUsers();
        var activeUsers = testUsers.Where(u => u.IsDeleted == false).ToList();

        // Mock repository trả về tất cả users (bao gồm cả deleted)
        _mockUserRepository
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(testUsers);

        // Mock role repository cho từng user
        var roleMap = new Dictionary<int, string>
        {
            { 2, "Admin" },
            { 3, "Manager" },
            { 5, "Customer" }
        };

        foreach (var user in activeUsers)
        {
            var role = CreateTestRole(user.RoleId, roleMap[user.RoleId]);
            _mockRoleRepository
                .Setup(repo => repo.GetByIdAsync(user.RoleId))
                .ReturnsAsync(role);

            // Mock AutoMapper
            var expectedDto = MapUserToDto(user, role.RoleName);
            _mockMapper
                .Setup(m => m.Map<UserDto>(user))
                .Returns(expectedDto);
        }

        // Act
        var result = await _userService.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3); // Chỉ có 3 user active (user 1, 2, 4)
        result.Should().OnlyContain(u => u.IsDeleted == false);

        // Verify repository được gọi
        _mockUserRepository.Verify(repo => repo.GetAllAsync(), Times.Once);

        // Verify role repository được gọi cho mỗi active user
        _mockRoleRepository.Verify(repo => repo.GetByIdAsync(It.IsAny<int>()), Times.Exactly(3));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoActiveUsers()
    {
        // Arrange
        var deletedUsers = CreateTestUsers().Where(u => u.IsDeleted == true).ToList();
        _mockUserRepository
            .Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(deletedUsers);

        // Act
        var result = await _userService.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region Test 2: GetByIdAsync_ReturnsCorrectUser

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectUser_WhenUserExistsAndNotDeleted()
    {
        // Arrange
        var userId = 1;
        var testUser = CreateTestUsers().First(u => u.UserId == userId);
        var testRole = CreateTestRole(2, "Admin");

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync(testUser);

        _mockRoleRepository
            .Setup(repo => repo.GetByIdAsync(testUser.RoleId))
            .ReturnsAsync(testRole);

        var expectedDto = MapUserToDto(testUser, testRole.RoleName);
        _mockMapper
            .Setup(m => m.Map<UserDto>(testUser))
            .Returns(expectedDto);

        // Act
        var result = await _userService.GetByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.Email.Should().Be(testUser.Email);
        result.FullName.Should().Be(testUser.FullName);
        result.RoleName.Should().Be("Admin");

        // Verify
        _mockUserRepository.Verify(repo => repo.GetByIdAsync(userId), Times.Once);
        _mockRoleRepository.Verify(repo => repo.GetByIdAsync(testUser.RoleId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenUserIsDeleted()
    {
        // Arrange
        var userId = 3; // User đã bị xóa
        var deletedUser = CreateTestUsers().First(u => u.UserId == userId);

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync(deletedUser);

        // Act
        var result = await _userService.GetByIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenUserNotFound()
    {
        // Arrange
        var userId = 999;
        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.GetByIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Test 3: CreateAsync_AddsNewUser

    [Fact]
    public async Task CreateAsync_AddsNewUser_WhenEmailNotExists()
    {
        // Arrange
        var createRequest = new UserCreateRequest
        {
            FullName = "Nguyễn Văn E",
            Email = "user5@example.com",
            Phone = "0777888999",
            RoleId = 2, // Admin
            Password = "Password123!",
            Status = 1
        };

        var testRole = CreateTestRole(2, "Admin");

        // Mock email không tồn tại
        _mockUserRepository
            .Setup(repo => repo.IsEmailExistsAsync(createRequest.Email))
            .ReturnsAsync(false);

        // Mock AddAsync và SaveChangesAsync
        _mockUserRepository
            .Setup(repo => repo.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(uow => uow.SaveChangesAsync())
            .ReturnsAsync(1);

        // Mock role repository
        _mockRoleRepository
            .Setup(repo => repo.GetByIdAsync(createRequest.RoleId))
            .ReturnsAsync(testRole);

        // Mock email service (không throw exception)
        _mockEmailService
            .Setup(es => es.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Mock mapper cho response
        User? capturedUser = null;
        _mockMapper
            .Setup(m => m.Map<UserDto>(It.IsAny<User>()))
            .Returns<User>((user) =>
            {
                capturedUser = user;
                return MapUserToDto(user, testRole.RoleName);
            });

        // Act
        var result = await _userService.CreateAsync(createRequest);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(createRequest.Email);
        result.FullName.Should().Be(createRequest.FullName);
        result.RoleName.Should().Be("Admin");

        // Verify AddAsync được gọi 1 lần
        _mockUserRepository.Verify(
            repo => repo.AddAsync(It.Is<User>(u =>
                u.Email == createRequest.Email &&
                u.FullName == createRequest.FullName &&
                u.IsDeleted == false
            )),
            Times.Once
        );

        // Verify SaveChangesAsync được gọi 1 lần
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);

        // Verify email service được gọi
        _mockEmailService.Verify(
            es => es.SendAsync(createRequest.Email, It.IsAny<string>(), It.IsAny<string>()),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateAsync_ThrowsException_WhenEmailAlreadyExists()
    {
        // Arrange
        var createRequest = new UserCreateRequest
        {
            FullName = "Nguyễn Văn E",
            Email = "user1@example.com", // Email đã tồn tại
            Phone = "0777888999",
            RoleId = 2,
            Password = "Password123!",
            Status = 1
        };

        _mockUserRepository
            .Setup(repo => repo.IsEmailExistsAsync(createRequest.Email))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _userService.CreateAsync(createRequest)
        );

        // Verify AddAsync KHÔNG được gọi
        _mockUserRepository.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Never);
    }

    #endregion

    #region Test 4: UpdateAsync_UpdatesUserSuccessfully

    [Fact]
    public async Task UpdateAsync_UpdatesUserSuccessfully_WhenUserExists()
    {
        // Arrange
        var userId = 1;
        var existingUser = CreateTestUsers().First(u => u.UserId == userId);
        var updateRequest = new UserUpdateRequest
        {
            FullName = "Nguyễn Văn A Updated",
            Email = "user1_updated@example.com",
            Phone = "0999888777",
            RoleId = 3, // Manager
            Status = 1
        };

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);

        // Mock email mới không tồn tại
        _mockUserRepository
            .Setup(repo => repo.IsEmailExistsAsync(updateRequest.Email))
            .ReturnsAsync(false);

        _mockUserRepository
            .Setup(repo => repo.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(uow => uow.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _userService.UpdateAsync(userId, updateRequest);

        // Assert
        // Verify GetByIdAsync được gọi
        _mockUserRepository.Verify(repo => repo.GetByIdAsync(userId), Times.Once);

        // Verify UpdateAsync được gọi với user đã được cập nhật
        _mockUserRepository.Verify(
            repo => repo.UpdateAsync(It.Is<User>(u =>
                u.UserId == userId &&
                u.FullName == updateRequest.FullName &&
                u.Email == updateRequest.Email &&
                u.Phone == updateRequest.Phone &&
                u.RoleId == updateRequest.RoleId &&
                u.Status == updateRequest.Status &&
                u.ModifiedAt != null
            )),
            Times.Once
        );

        // Verify SaveChangesAsync được gọi 1 lần
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsException_WhenUserNotFound()
    {
        // Arrange
        var userId = 999;
        var updateRequest = new UserUpdateRequest
        {
            FullName = "Test User",
            Email = "test@example.com",
            Phone = "0123456789",
            RoleId = 2,
            Status = 1
        };

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _userService.UpdateAsync(userId, updateRequest)
        );

        // Verify UpdateAsync KHÔNG được gọi
        _mockUserRepository.Verify(repo => repo.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsException_WhenUserIsDeleted()
    {
        // Arrange
        var userId = 3; // User đã bị xóa
        var deletedUser = CreateTestUsers().First(u => u.UserId == userId);
        var updateRequest = new UserUpdateRequest
        {
            FullName = "Test User",
            Email = "test@example.com",
            Phone = "0123456789",
            RoleId = 2,
            Status = 1
        };

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync(deletedUser);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _userService.UpdateAsync(userId, updateRequest)
        );
    }

    [Fact]
    public async Task UpdateAsync_ThrowsException_WhenNewEmailExists()
    {
        // Arrange
        var userId = 1;
        var existingUser = CreateTestUsers().First(u => u.UserId == userId);
        var updateRequest = new UserUpdateRequest
        {
            FullName = "Nguyễn Văn A Updated",
            Email = "user2@example.com", // Email đã tồn tại (của user khác)
            Phone = "0999888777",
            RoleId = 2,
            Status = 1
        };

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);

        _mockUserRepository
            .Setup(repo => repo.IsEmailExistsAsync(updateRequest.Email))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _userService.UpdateAsync(userId, updateRequest)
        );
    }

    #endregion

    #region Test 5: DeleteAsync_SoftDeletesUser

    [Fact]
    public async Task DeleteAsync_SoftDeletesUser_WhenUserExists()
    {
        // Arrange
        var userId = 1;
        var existingUser = CreateTestUsers().First(u => u.UserId == userId);
        existingUser.IsDeleted = false; // Đảm bảo user chưa bị xóa

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);

        _mockUserRepository
            .Setup(repo => repo.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(uow => uow.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _userService.DeleteAsync(userId);

        // Assert
        // Verify GetByIdAsync được gọi
        _mockUserRepository.Verify(repo => repo.GetByIdAsync(userId), Times.Once);

        // Verify UpdateAsync được gọi với user có IsDeleted = true
        _mockUserRepository.Verify(
            repo => repo.UpdateAsync(It.Is<User>(u =>
                u.UserId == userId &&
                u.IsDeleted == true &&
                u.DeletedAt != null
            )),
            Times.Once
        );

        // Verify SaveChangesAsync được gọi 1 lần
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsException_WhenUserNotFound()
    {
        // Arrange
        var userId = 999;
        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _userService.DeleteAsync(userId)
        );

        // Verify UpdateAsync KHÔNG được gọi
        _mockUserRepository.Verify(repo => repo.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsException_WhenUserAlreadyDeleted()
    {
        // Arrange
        var userId = 3; // User đã bị xóa
        var deletedUser = CreateTestUsers().First(u => u.UserId == userId);

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync(deletedUser);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _userService.DeleteAsync(userId)
        );
    }

    #endregion
}

