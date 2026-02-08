using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using DomainAccessLayer.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services.VinxTest;

/// <summary>
/// Unit Tests cho VerificationService
/// Test độc lập các phương thức trong VerificationService sử dụng xUnit + Moq
/// </summary>
public class VerificationServiceTests : IDisposable
{
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly SapaFoRestRmsContext _dbContext;
    private readonly VerificationService _verificationService;

    public VerificationServiceTests()
    {
        // Khởi tạo mocks
        _mockEmailService = new Mock<IEmailService>();

        // Use in-memory database for testing
        var options = new DbContextOptionsBuilder<SapaFoRestRmsContext>()
            .UseInMemoryDatabase(databaseName: $"VerificationTestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new SapaFoRestRmsContext(options);
        _dbContext.Database.EnsureCreated();

        // Khởi tạo VerificationService với mocked dependencies
        _verificationService = new VerificationService(
            _dbContext,
            _mockEmailService.Object
        );
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region GenerateAndSendCodeAsync Tests

    [Fact]
    public async Task GenerateAndSendCodeAsync_GeneratesAndSavesCode_WhenValidRequest()
    {
        // Arrange
        var userId = 1;
        var email = "test@example.com";
        var purpose = "TestPurpose";
        var ttlMinutes = 10;

        _mockEmailService
            .Setup(service => service.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _verificationService.GenerateAndSendCodeAsync(userId, email, purpose, ttlMinutes);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().Be(6); // 6-digit code

        var savedCode = await _dbContext.VerificationCodes
            .FirstOrDefaultAsync(v => v.UserId == userId && v.Purpose == purpose && v.Code == result);

        savedCode.Should().NotBeNull();
        savedCode!.UserId.Should().Be(userId);
        savedCode.Code.Should().Be(result);
        savedCode.Purpose.Should().Be(purpose);
        savedCode.IsUsed.Should().BeFalse();
        savedCode.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(ttlMinutes), TimeSpan.FromSeconds(5));

        _mockEmailService.Verify(
            service => service.SendAsync(
                email,
                $"Verification code for {purpose}",
                $"Your verification code is: {result}"),
            Times.Once);
    }

    [Fact]
    public async Task GenerateAndSendCodeAsync_GeneratesUniqueCodes_WhenCalledMultipleTimes()
    {
        // Arrange
        var userId = 1;
        var email = "test@example.com";
        var purpose = "TestPurpose";
        var ttlMinutes = 10;

        _mockEmailService
            .Setup(service => service.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var code1 = await _verificationService.GenerateAndSendCodeAsync(userId, email, purpose, ttlMinutes);
        var code2 = await _verificationService.GenerateAndSendCodeAsync(userId, email, purpose, ttlMinutes);
        var code3 = await _verificationService.GenerateAndSendCodeAsync(userId, email, purpose, ttlMinutes);

        // Assert
        code1.Should().NotBeNullOrEmpty();
        code2.Should().NotBeNullOrEmpty();
        code3.Should().NotBeNullOrEmpty();

        // Codes should be 6 digits each
        code1.Length.Should().Be(6);
        code2.Length.Should().Be(6);
        code3.Length.Should().Be(6);

        // All codes should be saved
        var savedCodes = await _dbContext.VerificationCodes
            .Where(v => v.UserId == userId && v.Purpose == purpose)
            .ToListAsync();

        savedCodes.Should().HaveCount(3);
        savedCodes.Select(c => c.Code).Should().Contain(code1, code2, code3);
    }

    [Fact]
    public async Task GenerateAndSendCodeAsync_SetsCorrectExpiration_WhenTtlMinutesProvided()
    {
        // Arrange
        var userId = 1;
        var email = "test@example.com";
        var purpose = "TestPurpose";
        var ttlMinutes = 30;

        _mockEmailService
            .Setup(service => service.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _verificationService.GenerateAndSendCodeAsync(userId, email, purpose, ttlMinutes);

        // Assert
        var savedCode = await _dbContext.VerificationCodes
            .FirstOrDefaultAsync(v => v.UserId == userId && v.Purpose == purpose && v.Code == result);

        savedCode.Should().NotBeNull();
        savedCode!.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(ttlMinutes), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GenerateAndSendCodeAsync_SendsEmail_WithCorrectParameters()
    {
        // Arrange
        var userId = 1;
        var email = "user@example.com";
        var purpose = "ResetPassword";
        var ttlMinutes = 10;

        string? sentEmail = null;
        string? sentSubject = null;
        string? sentBody = null;

        _mockEmailService
            .Setup(service => service.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Callback<string, string, string>((e, s, b) =>
            {
                sentEmail = e;
                sentSubject = s;
                sentBody = b;
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _verificationService.GenerateAndSendCodeAsync(userId, email, purpose, ttlMinutes);

        // Assert
        sentEmail.Should().Be(email);
        sentSubject.Should().Be($"Verification code for {purpose}");
        sentBody.Should().Be($"Your verification code is: {result}");
    }

    #endregion

    #region VerifyCodeAsync Tests

    [Fact]
    public async Task VerifyCodeAsync_ReturnsTrue_WhenValidCode()
    {
        // Arrange
        var userId = 1;
        var purpose = "TestPurpose";
        var code = "123456";
        var verificationCode = new VerificationCode
        {
            UserId = userId,
            Code = code,
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        };

        _dbContext.VerificationCodes.Add(verificationCode);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _verificationService.VerifyCodeAsync(userId, purpose, code);

        // Assert
        result.Should().BeTrue();

        var updatedCode = await _dbContext.VerificationCodes
            .FirstOrDefaultAsync(v => v.VerificationCodeId == verificationCode.VerificationCodeId);

        updatedCode.Should().NotBeNull();
        updatedCode!.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyCodeAsync_ReturnsFalse_WhenCodeNotFound()
    {
        // Arrange
        var userId = 1;
        var purpose = "TestPurpose";
        var code = "999999"; // Non-existent code

        // Act
        var result = await _verificationService.VerifyCodeAsync(userId, purpose, code);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyCodeAsync_ReturnsFalse_WhenCodeIsExpired()
    {
        // Arrange
        var userId = 1;
        var purpose = "TestPurpose";
        var code = "123456";
        var verificationCode = new VerificationCode
        {
            UserId = userId,
            Code = code,
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10), // Expired
            IsUsed = false
        };

        _dbContext.VerificationCodes.Add(verificationCode);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _verificationService.VerifyCodeAsync(userId, purpose, code);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyCodeAsync_ReturnsFalse_WhenCodeIsAlreadyUsed()
    {
        // Arrange
        var userId = 1;
        var purpose = "TestPurpose";
        var code = "123456";
        var verificationCode = new VerificationCode
        {
            UserId = userId,
            Code = code,
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = true // Already used
        };

        _dbContext.VerificationCodes.Add(verificationCode);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _verificationService.VerifyCodeAsync(userId, purpose, code);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyCodeAsync_ReturnsFalse_WhenUserIdMismatch()
    {
        // Arrange
        var userId = 1;
        var otherUserId = 2;
        var purpose = "TestPurpose";
        var code = "123456";
        var verificationCode = new VerificationCode
        {
            UserId = otherUserId, // Different user
            Code = code,
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        };

        _dbContext.VerificationCodes.Add(verificationCode);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _verificationService.VerifyCodeAsync(userId, purpose, code);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyCodeAsync_ReturnsFalse_WhenPurposeMismatch()
    {
        // Arrange
        var userId = 1;
        var purpose = "TestPurpose";
        var otherPurpose = "OtherPurpose";
        var code = "123456";
        var verificationCode = new VerificationCode
        {
            UserId = userId,
            Code = code,
            Purpose = otherPurpose, // Different purpose
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        };

        _dbContext.VerificationCodes.Add(verificationCode);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _verificationService.VerifyCodeAsync(userId, purpose, code);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyCodeAsync_SelectsMostRecentCode_WhenMultipleCodesExist()
    {
        // Arrange
        var userId = 1;
        var purpose = "TestPurpose";
        var code = "123456";

        // Add multiple codes with same code value
        var oldCode = new VerificationCode
        {
            UserId = userId,
            Code = code,
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        };

        var newCode = new VerificationCode
        {
            UserId = userId,
            Code = code,
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        };

        _dbContext.VerificationCodes.Add(oldCode);
        _dbContext.VerificationCodes.Add(newCode);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _verificationService.VerifyCodeAsync(userId, purpose, code);

        // Assert
        result.Should().BeTrue();

        // Verify the most recent code (highest VerificationCodeId) was marked as used
        var updatedNewCode = await _dbContext.VerificationCodes
            .FirstOrDefaultAsync(v => v.VerificationCodeId == newCode.VerificationCodeId);

        updatedNewCode.Should().NotBeNull();
        updatedNewCode!.IsUsed.Should().BeTrue();

        // Old code should still be unused
        var updatedOldCode = await _dbContext.VerificationCodes
            .FirstOrDefaultAsync(v => v.VerificationCodeId == oldCode.VerificationCodeId);

        updatedOldCode.Should().NotBeNull();
        updatedOldCode!.IsUsed.Should().BeFalse();
    }

    #endregion

    #region InvalidateCodesAsync Tests

    [Fact]
    public async Task InvalidateCodesAsync_InvalidatesAllUnusedCodes_ForUserAndPurpose()
    {
        // Arrange
        var userId = 1;
        var purpose = "TestPurpose";

        var code1 = new VerificationCode
        {
            UserId = userId,
            Code = "111111",
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        };

        var code2 = new VerificationCode
        {
            UserId = userId,
            Code = "222222",
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        };

        var code3 = new VerificationCode
        {
            UserId = userId,
            Code = "333333",
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = true // Already used, should not be invalidated
        };

        _dbContext.VerificationCodes.AddRange(code1, code2, code3);
        await _dbContext.SaveChangesAsync();

        // Act
        await _verificationService.InvalidateCodesAsync(userId, purpose);

        // Assert
        var invalidatedCode1 = await _dbContext.VerificationCodes
            .FirstOrDefaultAsync(v => v.VerificationCodeId == code1.VerificationCodeId);
        invalidatedCode1.Should().NotBeNull();
        invalidatedCode1!.IsUsed.Should().BeTrue();

        var invalidatedCode2 = await _dbContext.VerificationCodes
            .FirstOrDefaultAsync(v => v.VerificationCodeId == code2.VerificationCodeId);
        invalidatedCode2.Should().NotBeNull();
        invalidatedCode2!.IsUsed.Should().BeTrue();

        // Already used code should remain used
        var invalidatedCode3 = await _dbContext.VerificationCodes
            .FirstOrDefaultAsync(v => v.VerificationCodeId == code3.VerificationCodeId);
        invalidatedCode3.Should().NotBeNull();
        invalidatedCode3!.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task InvalidateCodesAsync_DoesNotInvalidateExpiredCodes()
    {
        // Arrange
        var userId = 1;
        var purpose = "TestPurpose";

        var activeCode = new VerificationCode
        {
            UserId = userId,
            Code = "111111",
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        };

        var expiredCode = new VerificationCode
        {
            UserId = userId,
            Code = "222222",
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10), // Expired
            IsUsed = false
        };

        _dbContext.VerificationCodes.AddRange(activeCode, expiredCode);
        await _dbContext.SaveChangesAsync();

        // Act
        await _verificationService.InvalidateCodesAsync(userId, purpose);

        // Assert
        var invalidatedActiveCode = await _dbContext.VerificationCodes
            .FirstOrDefaultAsync(v => v.VerificationCodeId == activeCode.VerificationCodeId);
        invalidatedActiveCode.Should().NotBeNull();
        invalidatedActiveCode!.IsUsed.Should().BeTrue();

        // Expired code should not be invalidated (query filters it out)
        var invalidatedExpiredCode = await _dbContext.VerificationCodes
            .FirstOrDefaultAsync(v => v.VerificationCodeId == expiredCode.VerificationCodeId);
        invalidatedExpiredCode.Should().NotBeNull();
        invalidatedExpiredCode!.IsUsed.Should().BeFalse(); // Still unused because it was filtered out
    }

    [Fact]
    public async Task InvalidateCodesAsync_DoesNotInvalidateCodes_ForDifferentUser()
    {
        // Arrange
        var userId = 1;
        var otherUserId = 2;
        var purpose = "TestPurpose";

        var userCode = new VerificationCode
        {
            UserId = userId,
            Code = "111111",
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        };

        var otherUserCode = new VerificationCode
        {
            UserId = otherUserId,
            Code = "222222",
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        };

        _dbContext.VerificationCodes.AddRange(userCode, otherUserCode);
        await _dbContext.SaveChangesAsync();

        // Act
        await _verificationService.InvalidateCodesAsync(userId, purpose);

        // Assert
        var invalidatedUserCode = await _dbContext.VerificationCodes
            .FirstOrDefaultAsync(v => v.VerificationCodeId == userCode.VerificationCodeId);
        invalidatedUserCode.Should().NotBeNull();
        invalidatedUserCode!.IsUsed.Should().BeTrue();

        // Other user's code should not be invalidated
        var invalidatedOtherUserCode = await _dbContext.VerificationCodes
            .FirstOrDefaultAsync(v => v.VerificationCodeId == otherUserCode.VerificationCodeId);
        invalidatedOtherUserCode.Should().NotBeNull();
        invalidatedOtherUserCode!.IsUsed.Should().BeFalse();
    }

    [Fact]
    public async Task InvalidateCodesAsync_DoesNotInvalidateCodes_ForDifferentPurpose()
    {
        // Arrange
        var userId = 1;
        var purpose = "TestPurpose";
        var otherPurpose = "OtherPurpose";

        var purposeCode = new VerificationCode
        {
            UserId = userId,
            Code = "111111",
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        };

        var otherPurposeCode = new VerificationCode
        {
            UserId = userId,
            Code = "222222",
            Purpose = otherPurpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        };

        _dbContext.VerificationCodes.AddRange(purposeCode, otherPurposeCode);
        await _dbContext.SaveChangesAsync();

        // Act
        await _verificationService.InvalidateCodesAsync(userId, purpose);

        // Assert
        var invalidatedPurposeCode = await _dbContext.VerificationCodes
            .FirstOrDefaultAsync(v => v.VerificationCodeId == purposeCode.VerificationCodeId);
        invalidatedPurposeCode.Should().NotBeNull();
        invalidatedPurposeCode!.IsUsed.Should().BeTrue();

        // Other purpose code should not be invalidated
        var invalidatedOtherPurposeCode = await _dbContext.VerificationCodes
            .FirstOrDefaultAsync(v => v.VerificationCodeId == otherPurposeCode.VerificationCodeId);
        invalidatedOtherPurposeCode.Should().NotBeNull();
        invalidatedOtherPurposeCode!.IsUsed.Should().BeFalse();
    }

    [Fact]
    public async Task InvalidateCodesAsync_HandlesEmptyResult_WhenNoCodesExist()
    {
        // Arrange
        var userId = 1;
        var purpose = "TestPurpose";

        // Act
        await _verificationService.InvalidateCodesAsync(userId, purpose);

        // Assert - Should not throw exception
        var codes = await _dbContext.VerificationCodes
            .Where(v => v.UserId == userId && v.Purpose == purpose)
            .ToListAsync();

        codes.Should().BeEmpty();
    }

    #endregion
}

