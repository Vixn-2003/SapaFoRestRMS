using BusinessAccessLayer.DTOs.Auth;
using BusinessAccessLayer.Services;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services.VinxTest;

/// <summary>
/// Unit Tests for AuthService
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IConfigurationSection> _mockJwtSection;
    private readonly SapaFoRestRmsContext _dbContext;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockJwtSection = new Mock<IConfigurationSection>();

        // Setup JWT configuration
        _mockJwtSection.Setup(s => s["Key"]).Returns("TestSecretKeyForJwtTokenGeneration123456789");
        _mockJwtSection.Setup(s => s["Issuer"]).Returns("TestIssuer");
        _mockJwtSection.Setup(s => s["Audience"]).Returns("TestAudience");
        _mockJwtSection.Setup(s => s["ExpireMinutes"]).Returns("60");
        _mockJwtSection.Setup(s => s["RefreshExpireDays"]).Returns("7");

        _mockConfiguration.Setup(c => c.GetSection("Jwt")).Returns(_mockJwtSection.Object);

        // Use in-memory database for Staff queries
        var options = new DbContextOptionsBuilder<SapaFoRestRmsContext>()
            .UseInMemoryDatabase(databaseName: $"AuthTestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new SapaFoRestRmsContext(options);
        _dbContext.Database.EnsureCreated();

        _authService = new AuthService(_mockUserRepository.Object, _mockConfiguration.Object, _dbContext);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region Helper Methods

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private User CreateTestUser(int userId, string email, string password, int roleId, string roleName, int status = 0, bool isDeleted = false)
    {
        return new User
        {
            UserId = userId,
            Email = email,
            FullName = $"Test User {userId}",
            Phone = "0123456789",
            PasswordHash = HashPassword(password),
            RoleId = roleId,
            Status = status,
            IsDeleted = isDeleted,
            Role = new Role
            {
                RoleId = roleId,
                RoleName = roleName
            }
        };
    }

    private Staff CreateTestStaff(int staffId, int userId, List<Position> positions)
    {
        var staff = new Staff
        {
            StaffId = staffId,
            UserId = userId,
            HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
            SalaryBase = 1000000m,
            Status = 0,
            Positions = positions
        };
        return staff;
    }

    private Position CreateTestPosition(int positionId, string positionName)
    {
        return new Position
        {
            PositionId = positionId,
            PositionName = positionName,
            Status = 0,
            BaseSalary = 5000000m
        };
    }

    #endregion

    #region Test 1: LoginAsync_UserNotFound_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task LoginAsync_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "Password123"
        };

        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _authService.LoginAsync(request)
        );
    }

    #endregion

    #region Test 2: LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        var user = CreateTestUser(1, "test@example.com", "CorrectPassword", 2, "Admin");
        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _authService.LoginAsync(request)
        );
    }

    #endregion

    #region Test 3: LoginAsync_UserIsDeleted_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task LoginAsync_UserIsDeleted_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "deleted@example.com",
            Password = "Password123"
        };

        var user = CreateTestUser(1, "deleted@example.com", "Password123", 2, "Admin", isDeleted: true);
        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _authService.LoginAsync(request)
        );

        Assert.Contains("đã bị xóa", exception.Message);
    }

    #endregion

    #region Test 4: LoginAsync_UserStatusInactive_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task LoginAsync_UserStatusInactive_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "inactive@example.com",
            Password = "Password123"
        };

        var user = CreateTestUser(1, "inactive@example.com", "Password123", 2, "Admin", status: 1);
        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _authService.LoginAsync(request)
        );

        Assert.Contains("không còn hoạt động", exception.Message);
    }

    #endregion

    #region Test 5: LoginAsync_StaffWithoutPositions_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task LoginAsync_StaffWithoutPositions_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "staff@example.com",
            Password = "Password123"
        };

        var user = CreateTestUser(1, "staff@example.com", "Password123", 4, "Staff");
        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        // Staff exists but has no positions
        var staff = CreateTestStaff(1, 1, new List<Position>());
        _dbContext.Staffs.Add(staff);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _authService.LoginAsync(request)
        );

        Assert.Contains("no assigned position", exception.Message);
    }

    #endregion

    #region Test 6: LoginAsync_StaffNotFound_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task LoginAsync_StaffNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "staff@example.com",
            Password = "Password123"
        };

        var user = CreateTestUser(1, "staff@example.com", "Password123", 4, "Staff");
        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        // No staff record exists for this user

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _authService.LoginAsync(request)
        );

        Assert.Contains("no assigned position", exception.Message);
    }

    #endregion

    #region Test 7: LoginAsync_NonStaffUser_Success

    [Fact]
    public async Task LoginAsync_NonStaffUser_Success()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "admin@example.com",
            Password = "Password123"
        };

        var user = CreateTestUser(1, "admin@example.com", "Password123", 2, "Admin");
        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.UserId, result.UserId);
        Assert.Equal(user.FullName, result.FullName);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.Phone, result.Phone);
        Assert.Equal(user.RoleId, result.RoleId);
        Assert.Equal("Admin", result.RoleName);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.RefreshToken);
        Assert.Null(result.Positions);
        Assert.Null(result.PositionIds);
    }

    #endregion

    #region Test 8: LoginAsync_StaffWithPositions_Success

    [Fact]
    public async Task LoginAsync_StaffWithPositions_Success()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "staff@example.com",
            Password = "Password123"
        };

        var user = CreateTestUser(1, "staff@example.com", "Password123", 4, "Staff");
        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        var position1 = CreateTestPosition(1, "Waiter");
        var position2 = CreateTestPosition(2, "Cashier");
        _dbContext.Positions.AddRange(position1, position2);
        await _dbContext.SaveChangesAsync();

        var staff = CreateTestStaff(1, 1, new List<Position> { position1, position2 });
        _dbContext.Staffs.Add(staff);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.UserId, result.UserId);
        Assert.Equal("Staff", result.RoleName);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.RefreshToken);
        Assert.NotNull(result.Positions);
        Assert.Equal(2, result.Positions.Count);
        Assert.Contains("Waiter", result.Positions);
        Assert.Contains("Cashier", result.Positions);
        Assert.NotNull(result.PositionIds);
        Assert.Equal(2, result.PositionIds.Count);
        Assert.Contains(1, result.PositionIds);
        Assert.Contains(2, result.PositionIds);
    }

    #endregion

    #region Test 9: RefreshTokenAsync_InvalidToken_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task RefreshTokenAsync_InvalidToken_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _authService.RefreshTokenAsync(invalidToken)
        );
    }

    #endregion

    #region Test 10: RefreshTokenAsync_ValidTokenButNoRefreshClaim_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task RefreshTokenAsync_ValidTokenButNoRefreshClaim_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = CreateTestUser(1, "test@example.com", "Password123", 2, "Admin");
        
        // Generate a regular JWT token (not a refresh token)
        var regularToken = GenerateJwtTokenForTest(user, includeRefreshClaim: false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _authService.RefreshTokenAsync(regularToken)
        );
    }

    #endregion

    #region Test 11: RefreshTokenAsync_UserNotFound_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task RefreshTokenAsync_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = CreateTestUser(999, "nonexistent@example.com", "Password123", 2, "Admin");
        var refreshToken = GenerateJwtTokenForTest(user, includeRefreshClaim: true);

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(999))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _authService.RefreshTokenAsync(refreshToken)
        );

        Assert.Contains("User not found", exception.Message);
    }

    #endregion

    #region Test 12: RefreshTokenAsync_UserIsDeleted_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task RefreshTokenAsync_UserIsDeleted_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = CreateTestUser(1, "deleted@example.com", "Password123", 2, "Admin", isDeleted: true);
        var refreshToken = GenerateJwtTokenForTest(user, includeRefreshClaim: true);

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _authService.RefreshTokenAsync(refreshToken)
        );

        Assert.Contains("đã bị xóa", exception.Message);
    }

    #endregion

    #region Test 13: RefreshTokenAsync_UserStatusInactive_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task RefreshTokenAsync_UserStatusInactive_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = CreateTestUser(1, "inactive@example.com", "Password123", 2, "Admin", status: 1);
        var refreshToken = GenerateJwtTokenForTest(user, includeRefreshClaim: true);

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _authService.RefreshTokenAsync(refreshToken)
        );

        Assert.Contains("không còn hoạt động", exception.Message);
    }

    #endregion

    #region Test 14: RefreshTokenAsync_ValidToken_Success

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_Success()
    {
        // Arrange
        var user = CreateTestUser(1, "test@example.com", "Password123", 2, "Admin");
        var refreshToken = GenerateJwtTokenForTest(user, includeRefreshClaim: true);

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.RefreshTokenAsync(refreshToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.UserId, result.UserId);
        Assert.Equal(user.FullName, result.FullName);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.RoleId, result.RoleId);
        Assert.Equal("Admin", result.RoleName);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.RefreshToken);
    }

    #endregion

    #region Helper Method for Generating Test JWT Tokens

    private string GenerateJwtTokenForTest(User user, bool includeRefreshClaim)
    {
        var jwtConfig = _mockConfiguration.Object.GetSection("Jwt");
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtConfig["Key"]));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var claims = new List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new System.Security.Claims.Claim("userId", user.UserId.ToString())
        };

        if (includeRefreshClaim)
        {
            claims.Add(new System.Security.Claims.Claim("rt", "1"));
        }

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: jwtConfig["Issuer"],
            audience: jwtConfig["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }

    #endregion
}

