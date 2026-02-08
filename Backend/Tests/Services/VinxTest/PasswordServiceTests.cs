using BusinessAccessLayer.DTOs.Auth;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services.VinxTest;

/// <summary>
/// Unit Tests for PasswordService
/// </summary>
public class PasswordServiceTests : IDisposable
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IVerificationService> _mockVerificationService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly SapaFoRestRmsContext _context;
    private readonly PasswordService _passwordService;

    public PasswordServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockVerificationService = new Mock<IVerificationService>();
        _mockEmailService = new Mock<IEmailService>();

        // Use in-memory database for context
        var options = new DbContextOptionsBuilder<SapaFoRestRmsContext>()
            .UseInMemoryDatabase(databaseName: $"PasswordTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new SapaFoRestRmsContext(options);
        _context.Database.EnsureCreated();

        _passwordService = new PasswordService(
            _mockUserRepository.Object,
            _mockVerificationService.Object,
            _mockEmailService.Object,
            _context
        );
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    #region Helper Methods

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private User CreateTestUser(int userId, string email, string password, bool isDeleted = false)
    {
        return new User
        {
            UserId = userId,
            Email = email,
            FullName = $"Test User {userId}",
            Phone = "0123456789",
            PasswordHash = HashPassword(password),
            RoleId = 2,
            Status = 0,
            IsDeleted = isDeleted
        };
    }

    #endregion

    #region Test 1: RequestResetAsync_UserNotFound_ReturnsSilently

    [Fact]
    public async Task RequestResetAsync_UserNotFound_ReturnsSilently()
    {
        // Arrange
        var request = new RequestPasswordResetDto
        {
            Email = "nonexistent@example.com"
        };

        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        // Act
        await _passwordService.RequestResetAsync(request);

        // Assert
        // Should return silently without throwing
        _mockVerificationService.Verify(
            service => service.GenerateAndSendCodeAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );
    }

    #endregion

    #region Test 2: RequestResetAsync_UserIsDeleted_ReturnsSilently

    [Fact]
    public async Task RequestResetAsync_UserIsDeleted_ReturnsSilently()
    {
        // Arrange
        var request = new RequestPasswordResetDto
        {
            Email = "deleted@example.com"
        };

        var user = CreateTestUser(1, "deleted@example.com", "Password123", isDeleted: true);
        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        // Act
        await _passwordService.RequestResetAsync(request);

        // Assert
        // Should return silently without throwing
        _mockVerificationService.Verify(
            service => service.GenerateAndSendCodeAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );
    }

    #endregion

    #region Test 3: RequestResetAsync_ValidUser_GeneratesCodeAndSendsEmail

    [Fact]
    public async Task RequestResetAsync_ValidUser_GeneratesCodeAndSendsEmail()
    {
        // Arrange
        var request = new RequestPasswordResetDto
        {
            Email = "test@example.com"
        };

        var user = CreateTestUser(1, "test@example.com", "Password123");
        var verificationCode = "123456";

        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _mockVerificationService
            .Setup(service => service.GenerateAndSendCodeAsync(
                user.UserId,
                user.Email,
                "ResetPassword",
                10,
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(verificationCode);

        _mockEmailService
            .Setup(service => service.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ))
            .Returns(Task.CompletedTask);

        // Act
        await _passwordService.RequestResetAsync(request);

        // Assert
        _mockVerificationService.Verify(
            service => service.GenerateAndSendCodeAsync(
                user.UserId,
                user.Email,
                "ResetPassword",
                10,
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );

        _mockEmailService.Verify(
            service => service.SendAsync(
                user.Email,
                It.Is<string>(s => s.Contains("đặt lại mật khẩu")),
                It.Is<string>(body => body.Contains(verificationCode))
            ),
            Times.Once
        );
    }

    #endregion

    #region Test 4: VerifyResetAsync_UserNotFound_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task VerifyResetAsync_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new VerifyPasswordResetDto
        {
            Email = "nonexistent@example.com",
            Code = "123456"
        };

        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _passwordService.VerifyResetAsync(request)
        );
    }

    #endregion

    #region Test 5: VerifyResetAsync_UserIsDeleted_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task VerifyResetAsync_UserIsDeleted_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new VerifyPasswordResetDto
        {
            Email = "deleted@example.com",
            Code = "123456"
        };

        var user = CreateTestUser(1, "deleted@example.com", "Password123", isDeleted: true);
        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _passwordService.VerifyResetAsync(request)
        );
    }

    #endregion

    #region Test 6: VerifyResetAsync_InvalidCode_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task VerifyResetAsync_InvalidCode_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new VerifyPasswordResetDto
        {
            Email = "test@example.com",
            Code = "000000"
        };

        var user = CreateTestUser(1, "test@example.com", "Password123");
        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _mockVerificationService
            .Setup(service => service.VerifyCodeAsync(
                user.UserId,
                "ResetPassword",
                request.Code,
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _passwordService.VerifyResetAsync(request)
        );

        Assert.Contains("Invalid verification code", exception.Message);
    }

    #endregion

    #region Test 7: VerifyResetAsync_ValidCode_GeneratesNewPasswordAndUpdatesUser

    [Fact]
    public async Task VerifyResetAsync_ValidCode_GeneratesNewPasswordAndUpdatesUser()
    {
        // Arrange
        var request = new VerifyPasswordResetDto
        {
            Email = "test@example.com",
            Code = "123456"
        };

        var user = CreateTestUser(1, "test@example.com", "OldPassword123");
        var oldPasswordHash = user.PasswordHash;

        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _mockVerificationService
            .Setup(service => service.VerifyCodeAsync(
                user.UserId,
                "ResetPassword",
                request.Code,
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(true);

        _mockUserRepository
            .Setup(repo => repo.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _mockEmailService
            .Setup(service => service.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _passwordService.VerifyResetAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        _mockUserRepository.Verify(
            repo => repo.UpdateAsync(It.Is<User>(u => 
                u.UserId == user.UserId && 
                u.PasswordHash != oldPasswordHash
            )),
            Times.Once
        );

        _mockEmailService.Verify(
            service => service.SendAsync(
                user.Email,
                "Your new password",
                It.Is<string>(body => body.Contains(result))
            ),
            Times.Once
        );
    }

    #endregion

    #region Test 8: ResetPasswordAsync_UserNotFound_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task ResetPasswordAsync_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new ResetPasswordDto
        {
            Email = "nonexistent@example.com",
            Code = "123456",
            NewPassword = "NewPassword123",
            ConfirmPassword = "NewPassword123"
        };

        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _passwordService.ResetPasswordAsync(request)
        );

        Assert.Contains("Email không tồn tại", exception.Message);
    }

    #endregion

    #region Test 9: ResetPasswordAsync_UserIsDeleted_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task ResetPasswordAsync_UserIsDeleted_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new ResetPasswordDto
        {
            Email = "deleted@example.com",
            Code = "123456",
            NewPassword = "NewPassword123",
            ConfirmPassword = "NewPassword123"
        };

        var user = CreateTestUser(1, "deleted@example.com", "Password123", isDeleted: true);
        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _passwordService.ResetPasswordAsync(request)
        );

        Assert.Contains("Email không tồn tại", exception.Message);
    }

    #endregion

    #region Test 10: ResetPasswordAsync_InvalidCode_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task ResetPasswordAsync_InvalidCode_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new ResetPasswordDto
        {
            Email = "test@example.com",
            Code = "000000",
            NewPassword = "NewPassword123",
            ConfirmPassword = "NewPassword123"
        };

        var user = CreateTestUser(1, "test@example.com", "Password123");
        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _mockVerificationService
            .Setup(service => service.VerifyCodeAsync(
                user.UserId,
                "ResetPassword",
                request.Code,
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _passwordService.ResetPasswordAsync(request)
        );

        Assert.Contains("Mã xác nhận không hợp lệ", exception.Message);
    }

    #endregion

    #region Test 11: ResetPasswordAsync_PasswordTooShort_ThrowsArgumentException

    [Fact]
    public async Task ResetPasswordAsync_PasswordTooShort_ThrowsArgumentException()
    {
        // Arrange
        var request = new ResetPasswordDto
        {
            Email = "test@example.com",
            Code = "123456",
            NewPassword = "Short1",
            ConfirmPassword = "Short1"
        };

        var user = CreateTestUser(1, "test@example.com", "Password123");
        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _mockVerificationService
            .Setup(service => service.VerifyCodeAsync(
                user.UserId,
                "ResetPassword",
                request.Code,
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _passwordService.ResetPasswordAsync(request)
        );

        Assert.Contains("ít nhất 8 ký tự", exception.Message);
    }

    #endregion

    #region Test 12: ResetPasswordAsync_PasswordsDoNotMatch_ThrowsArgumentException

    [Fact]
    public async Task ResetPasswordAsync_PasswordsDoNotMatch_ThrowsArgumentException()
    {
        // Arrange
        var request = new ResetPasswordDto
        {
            Email = "test@example.com",
            Code = "123456",
            NewPassword = "NewPassword123",
            ConfirmPassword = "DifferentPassword123"
        };

        var user = CreateTestUser(1, "test@example.com", "Password123");
        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _mockVerificationService
            .Setup(service => service.VerifyCodeAsync(
                user.UserId,
                "ResetPassword",
                request.Code,
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _passwordService.ResetPasswordAsync(request)
        );

        Assert.Contains("không khớp", exception.Message);
    }

    #endregion

    #region Test 13: ResetPasswordAsync_ValidRequest_UpdatesPasswordAndSendsEmail

    [Fact]
    public async Task ResetPasswordAsync_ValidRequest_UpdatesPasswordAndSendsEmail()
    {
        // Arrange
        var request = new ResetPasswordDto
        {
            Email = "test@example.com",
            Code = "123456",
            NewPassword = "NewPassword123",
            ConfirmPassword = "NewPassword123"
        };

        var user = CreateTestUser(1, "test@example.com", "OldPassword123");
        var oldPasswordHash = user.PasswordHash;

        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _mockVerificationService
            .Setup(service => service.VerifyCodeAsync(
                user.UserId,
                "ResetPassword",
                request.Code,
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(true);

        _mockUserRepository
            .Setup(repo => repo.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _mockEmailService
            .Setup(service => service.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ))
            .Returns(Task.CompletedTask);

        // Act
        await _passwordService.ResetPasswordAsync(request);

        // Assert
        _mockUserRepository.Verify(
            repo => repo.UpdateAsync(It.Is<User>(u => 
                u.UserId == user.UserId && 
                u.PasswordHash != oldPasswordHash &&
                u.ModifiedAt != null
            )),
            Times.Once
        );

        _mockEmailService.Verify(
            service => service.SendAsync(
                user.Email,
                "Mật khẩu đã được đặt lại",
                It.IsAny<string>()
            ),
            Times.Once
        );
    }

    #endregion

    #region Test 14: RequestChangeAsync_UserNotFound_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task RequestChangeAsync_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new RequestChangePasswordDto
        {
            UserId = 999,
            CurrentPassword = "CurrentPassword123"
        };

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(request.UserId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _passwordService.RequestChangeAsync(request)
        );
    }

    #endregion

    #region Test 15: RequestChangeAsync_UserIsDeleted_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task RequestChangeAsync_UserIsDeleted_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new RequestChangePasswordDto
        {
            UserId = 1,
            CurrentPassword = "CurrentPassword123"
        };

        var user = CreateTestUser(1, "test@example.com", "CurrentPassword123", isDeleted: true);
        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(request.UserId))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _passwordService.RequestChangeAsync(request)
        );
    }

    #endregion

    #region Test 16: RequestChangeAsync_WrongCurrentPassword_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task RequestChangeAsync_WrongCurrentPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new RequestChangePasswordDto
        {
            UserId = 1,
            CurrentPassword = "WrongPassword123"
        };

        var user = CreateTestUser(1, "test@example.com", "CorrectPassword123");
        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(request.UserId))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _passwordService.RequestChangeAsync(request)
        );

        Assert.Contains("Invalid current password", exception.Message);
    }

    #endregion

    #region Test 17: RequestChangeAsync_ValidRequest_GeneratesCode

    [Fact]
    public async Task RequestChangeAsync_ValidRequest_GeneratesCode()
    {
        // Arrange
        var request = new RequestChangePasswordDto
        {
            UserId = 1,
            CurrentPassword = "CurrentPassword123"
        };

        var user = CreateTestUser(1, "test@example.com", "CurrentPassword123");
        var verificationCode = "123456";

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(request.UserId))
            .ReturnsAsync(user);

        _mockVerificationService
            .Setup(service => service.GenerateAndSendCodeAsync(
                user.UserId,
                user.Email,
                "ChangePassword",
                10,
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(verificationCode);

        // Act
        await _passwordService.RequestChangeAsync(request);

        // Assert
        _mockVerificationService.Verify(
            service => service.GenerateAndSendCodeAsync(
                user.UserId,
                user.Email,
                "ChangePassword",
                10,
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    #endregion

    #region Test 18: ChangeAsync_UserNotFound_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task ChangeAsync_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new VerifyChangePasswordDto
        {
            UserId = 999,
            Code = "123456",
            NewPassword = "NewPassword123"
        };

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(request.UserId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _passwordService.ChangeAsync(request)
        );
    }

    #endregion

    #region Test 19: ChangeAsync_UserIsDeleted_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task ChangeAsync_UserIsDeleted_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new VerifyChangePasswordDto
        {
            UserId = 1,
            Code = "123456",
            NewPassword = "NewPassword123"
        };

        var user = CreateTestUser(1, "test@example.com", "OldPassword123", isDeleted: true);
        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(request.UserId))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _passwordService.ChangeAsync(request)
        );
    }

    #endregion

    #region Test 20: ChangeAsync_InvalidCode_ThrowsUnauthorizedAccessException

    [Fact]
    public async Task ChangeAsync_InvalidCode_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new VerifyChangePasswordDto
        {
            UserId = 1,
            Code = "000000",
            NewPassword = "NewPassword123"
        };

        var user = CreateTestUser(1, "test@example.com", "OldPassword123");
        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(request.UserId))
            .ReturnsAsync(user);

        _mockVerificationService
            .Setup(service => service.VerifyCodeAsync(
                user.UserId,
                "ChangePassword",
                request.Code,
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _passwordService.ChangeAsync(request)
        );

        Assert.Contains("Invalid verification code", exception.Message);
    }

    #endregion

    #region Test 21: ChangeAsync_ValidRequest_UpdatesPassword

    [Fact]
    public async Task ChangeAsync_ValidRequest_UpdatesPassword()
    {
        // Arrange
        var request = new VerifyChangePasswordDto
        {
            UserId = 1,
            Code = "123456",
            NewPassword = "NewPassword123"
        };

        var user = CreateTestUser(1, "test@example.com", "OldPassword123");
        var oldPasswordHash = user.PasswordHash;

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(request.UserId))
            .ReturnsAsync(user);

        _mockVerificationService
            .Setup(service => service.VerifyCodeAsync(
                user.UserId,
                "ChangePassword",
                request.Code,
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(true);

        _mockUserRepository
            .Setup(repo => repo.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        await _passwordService.ChangeAsync(request);

        // Assert
        _mockUserRepository.Verify(
            repo => repo.UpdateAsync(It.Is<User>(u => 
                u.UserId == user.UserId && 
                u.PasswordHash != oldPasswordHash &&
                u.ModifiedAt != null
            )),
            Times.Once
        );
    }

    #endregion
}

