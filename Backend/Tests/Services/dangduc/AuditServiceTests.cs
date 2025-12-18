using Xunit;
using Moq;
using AutoMapper;
using System;
using System.Threading.Tasks;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.DTOs.Inventory;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;

public class AuditServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly AuditService _service;

    public AuditServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _service = new AuditService(_mockUnitOfWork.Object, _mockMapper.Object);
    }

    #region ConfirmAuditAsync Tests

    // UTCID01: Test xác nhận audit thành công với IsAddition = true
    [Fact]
    public async Task ConfirmAuditAsync_WhenSuccessfulWithAddition_ReturnsTrue()
    {
        // Arrange
        string auditId = "AUD001";
        int batchId = 1;
        var request = new AuditInventoryResponseDTO
        {
            AuditId = auditId,
            OriginalQuantity = 100,
            AdjustmentQuantity = 20,
            IsAddition = true,
            ExpiryDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(6)),
            AuditStatus = "Confirmed"
        };

        var batch = new InventoryBatch
        {
            BatchId = batchId,
            QuantityRemaining = 100,
            ExpiryDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(5))
        };

        var auditEntity = new AuditInventory
        {
            AuditId = auditId,
            AdjustmentQuantity = 20,
            IsAddition = true
        };

        _mockUnitOfWork.Setup(x => x.AuditRepository.GetBatchIdByIdReAsync(auditId))
            .ReturnsAsync(batchId);
        _mockUnitOfWork.Setup(x => x.InventoryIngredient.getBatchByBatchId(batchId))
            .ReturnsAsync(batch);
        _mockMapper.Setup(x => x.Map<AuditInventory>(request))
            .Returns(auditEntity);
        _mockUnitOfWork.Setup(x => x.AuditRepository.ConfirmAuditReAsync(auditId, auditEntity))
            .ReturnsAsync(true);
        _mockUnitOfWork.Setup(x => x.InventoryIngredient.UpdateBatchByBatch(batch))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ConfirmAuditAsync(auditId, request);

        // Assert
        Assert.True(result);
        Assert.Equal(120, batch.QuantityRemaining); // 100 + 20
        Assert.Equal(request.ExpiryDate, batch.ExpiryDate);
        _mockUnitOfWork.Verify(x => x.AuditRepository.ConfirmAuditReAsync(auditId, auditEntity), Times.Once);
        _mockUnitOfWork.Verify(x => x.InventoryIngredient.UpdateBatchByBatch(batch), Times.Once);
    }

    // UTCID02: Test xác nhận audit thành công với IsAddition = false
    [Fact]
    public async Task ConfirmAuditAsync_WhenSuccessfulWithSubtraction_ReturnsTrue()
    {
        // Arrange
        string auditId = "AUD002";
        int batchId = 2;
        var request = new AuditInventoryResponseDTO
        {
            AuditId = auditId,
            OriginalQuantity = 100,
            AdjustmentQuantity = 15,
            IsAddition = false,
            ExpiryDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(6)),
            AuditStatus = "Confirmed"
        };

        var batch = new InventoryBatch
        {
            BatchId = batchId,
            QuantityRemaining = 100
        };

        var auditEntity = new AuditInventory { AuditId = auditId };

        _mockUnitOfWork.Setup(x => x.AuditRepository.GetBatchIdByIdReAsync(auditId))
            .ReturnsAsync(batchId);
        _mockUnitOfWork.Setup(x => x.InventoryIngredient.getBatchByBatchId(batchId))
            .ReturnsAsync(batch);
        _mockMapper.Setup(x => x.Map<AuditInventory>(request)).Returns(auditEntity);
        _mockUnitOfWork.Setup(x => x.AuditRepository.ConfirmAuditReAsync(auditId, auditEntity))
            .ReturnsAsync(true);
        _mockUnitOfWork.Setup(x => x.InventoryIngredient.UpdateBatchByBatch(batch))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ConfirmAuditAsync(auditId, request);

        // Assert
        Assert.True(result);
        Assert.Equal(85, batch.QuantityRemaining); // 100 - 15
    }

    // UTCID03: Test khi không tìm thấy BatchId
    [Fact]
    public async Task ConfirmAuditAsync_WhenBatchIdNotFound_ReturnsFalse()
    {
        // Arrange
        string auditId = "AUD999";
        var request = new AuditInventoryResponseDTO
        {
            AuditId = auditId,
            OriginalQuantity = 100,
            AdjustmentQuantity = 20,
            IsAddition = true
        };

        _mockUnitOfWork.Setup(x => x.AuditRepository.GetBatchIdByIdReAsync(auditId))
            .ReturnsAsync(0);

        // Act
        var result = await _service.ConfirmAuditAsync(auditId, request);

        // Assert
        Assert.False(result);
        _mockUnitOfWork.Verify(x => x.InventoryIngredient.getBatchByBatchId(It.IsAny<int>()), Times.Never);
    }

    // UTCID04: Test khi không tìm thấy InventoryBatch
    [Fact]
    public async Task ConfirmAuditAsync_WhenBatchNotFound_ReturnsFalse()
    {
        // Arrange
        string auditId = "AUD003";
        int batchId = 999;
        var request = new AuditInventoryResponseDTO
        {
            AuditId = auditId,
            OriginalQuantity = 100,
            AdjustmentQuantity = 20,
            IsAddition = true
        };

        _mockUnitOfWork.Setup(x => x.AuditRepository.GetBatchIdByIdReAsync(auditId))
            .ReturnsAsync(batchId);
        _mockUnitOfWork.Setup(x => x.InventoryIngredient.getBatchByBatchId(batchId))
            .ReturnsAsync((InventoryBatch)null);

        // Act
        var result = await _service.ConfirmAuditAsync(auditId, request);

        // Assert
        Assert.False(result);
        _mockUnitOfWork.Verify(x => x.AuditRepository.ConfirmAuditReAsync(It.IsAny<string>(), It.IsAny<AuditInventory>()), Times.Never);
    }

    // UTCID05: Test khi ConfirmAuditReAsync thất bại
    [Fact]
    public async Task ConfirmAuditAsync_WhenConfirmFails_ReturnsFalse()
    {
        // Arrange
        string auditId = "AUD004";
        int batchId = 4;
        var request = new AuditInventoryResponseDTO
        {
            AuditId = auditId,
            OriginalQuantity = 100,
            AdjustmentQuantity = 20,
            IsAddition = true
        };

        var batch = new InventoryBatch { BatchId = batchId, QuantityRemaining = 100 };
        var auditEntity = new AuditInventory { AuditId = auditId };

        _mockUnitOfWork.Setup(x => x.AuditRepository.GetBatchIdByIdReAsync(auditId))
            .ReturnsAsync(batchId);
        _mockUnitOfWork.Setup(x => x.InventoryIngredient.getBatchByBatchId(batchId))
            .ReturnsAsync(batch);
        _mockMapper.Setup(x => x.Map<AuditInventory>(request)).Returns(auditEntity);
        _mockUnitOfWork.Setup(x => x.AuditRepository.ConfirmAuditReAsync(auditId, auditEntity))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ConfirmAuditAsync(auditId, request);

        // Assert
        Assert.False(result);
        _mockUnitOfWork.Verify(x => x.InventoryIngredient.UpdateBatchByBatch(It.IsAny<InventoryBatch>()), Times.Never);
    }

    // UTCID06: Test khi UpdateBatchByBatch thất bại
    [Fact]
    public async Task ConfirmAuditAsync_WhenUpdateBatchFails_ReturnsFalse()
    {
        // Arrange
        string auditId = "AUD005";
        int batchId = 5;
        var request = new AuditInventoryResponseDTO
        {
            AuditId = auditId,
            OriginalQuantity = 100,
            AdjustmentQuantity = 20,
            IsAddition = true,
            ExpiryDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(6))
        };

        var batch = new InventoryBatch { BatchId = batchId, QuantityRemaining = 100 };
        var auditEntity = new AuditInventory { AuditId = auditId };

        _mockUnitOfWork.Setup(x => x.AuditRepository.GetBatchIdByIdReAsync(auditId))
            .ReturnsAsync(batchId);
        _mockUnitOfWork.Setup(x => x.InventoryIngredient.getBatchByBatchId(batchId))
            .ReturnsAsync(batch);
        _mockMapper.Setup(x => x.Map<AuditInventory>(request)).Returns(auditEntity);
        _mockUnitOfWork.Setup(x => x.AuditRepository.ConfirmAuditReAsync(auditId, auditEntity))
            .ReturnsAsync(true);
        _mockUnitOfWork.Setup(x => x.InventoryIngredient.UpdateBatchByBatch(batch))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ConfirmAuditAsync(auditId, request);

        // Assert
        Assert.False(result);
    }

    // UTCID07: Test xử lý exception
    [Fact]
    public async Task ConfirmAuditAsync_WhenExceptionThrown_ReturnsFalse()
    {
        // Arrange
        string auditId = "AUD006";
        var request = new AuditInventoryResponseDTO
        {
            AuditId = auditId,
            OriginalQuantity = 100,
            AdjustmentQuantity = 20,
            IsAddition = true
        };

        _mockUnitOfWork.Setup(x => x.AuditRepository.GetBatchIdByIdReAsync(auditId))
            .ThrowsAsync(new Exception("Database connection error"));

        // Act
        var result = await _service.ConfirmAuditAsync(auditId, request);

        // Assert
        Assert.False(result);
    }

    // UTCID08: Test với giá trị boundary - AdjustmentQuantity = 0
    [Fact]
    public async Task ConfirmAuditAsync_WhenAdjustmentQuantityIsZero_ReturnsTrue()
    {
        // Arrange
        string auditId = "AUD007";
        int batchId = 7;
        var request = new AuditInventoryResponseDTO
        {
            AuditId = auditId,
            OriginalQuantity = 100,
            AdjustmentQuantity = 0,
            IsAddition = true,
            ExpiryDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(6))
        };

        var batch = new InventoryBatch { BatchId = batchId, QuantityRemaining = 100 };
        var auditEntity = new AuditInventory { AuditId = auditId };

        _mockUnitOfWork.Setup(x => x.AuditRepository.GetBatchIdByIdReAsync(auditId))
            .ReturnsAsync(batchId);
        _mockUnitOfWork.Setup(x => x.InventoryIngredient.getBatchByBatchId(batchId))
            .ReturnsAsync(batch);
        _mockMapper.Setup(x => x.Map<AuditInventory>(request)).Returns(auditEntity);
        _mockUnitOfWork.Setup(x => x.AuditRepository.ConfirmAuditReAsync(auditId, auditEntity))
            .ReturnsAsync(true);
        _mockUnitOfWork.Setup(x => x.InventoryIngredient.UpdateBatchByBatch(batch))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ConfirmAuditAsync(auditId, request);

        // Assert
        Assert.True(result);
        Assert.Equal(100, batch.QuantityRemaining); // Không thay đổi
    }

    // UTCID09: Test với subtraction làm quantity âm
    [Fact]
    public async Task ConfirmAuditAsync_WhenSubtractionResultsInNegative_StillProcesses()
    {
        // Arrange
        string auditId = "AUD008";
        int batchId = 8;
        var request = new AuditInventoryResponseDTO
        {
            AuditId = auditId,
            OriginalQuantity = 50,
            AdjustmentQuantity = 70,
            IsAddition = false,
            ExpiryDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(6))
        };

        var batch = new InventoryBatch { BatchId = batchId, QuantityRemaining = 50 };
        var auditEntity = new AuditInventory { AuditId = auditId };

        _mockUnitOfWork.Setup(x => x.AuditRepository.GetBatchIdByIdReAsync(auditId))
            .ReturnsAsync(batchId);
        _mockUnitOfWork.Setup(x => x.InventoryIngredient.getBatchByBatchId(batchId))
            .ReturnsAsync(batch);
        _mockMapper.Setup(x => x.Map<AuditInventory>(request)).Returns(auditEntity);
        _mockUnitOfWork.Setup(x => x.AuditRepository.ConfirmAuditReAsync(auditId, auditEntity))
            .ReturnsAsync(true);
        _mockUnitOfWork.Setup(x => x.InventoryIngredient.UpdateBatchByBatch(batch))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ConfirmAuditAsync(auditId, request);

        // Assert
        Assert.True(result);
        Assert.Equal(-20, batch.QuantityRemaining); // 50 - 70 = -20
    }

    // UTCID10: Test với ExpiryDate null
    [Fact]
    public async Task ConfirmAuditAsync_WhenExpiryDateIsNull_HandlesCorrectly()
    {
        // Arrange
        string auditId = "AUD009";
        int batchId = 9;
        var request = new AuditInventoryResponseDTO
        {
            AuditId = auditId,
            OriginalQuantity = 100,
            AdjustmentQuantity = 10,
            IsAddition = true,
            ExpiryDate = null
        };

        var batch = new InventoryBatch { BatchId = batchId, QuantityRemaining = 100 };
        var auditEntity = new AuditInventory { AuditId = auditId };

        _mockUnitOfWork.Setup(x => x.AuditRepository.GetBatchIdByIdReAsync(auditId))
            .ReturnsAsync(batchId);
        _mockUnitOfWork.Setup(x => x.InventoryIngredient.getBatchByBatchId(batchId))
            .ReturnsAsync(batch);
        _mockMapper.Setup(x => x.Map<AuditInventory>(request)).Returns(auditEntity);
        _mockUnitOfWork.Setup(x => x.AuditRepository.ConfirmAuditReAsync(auditId, auditEntity))
            .ReturnsAsync(true);
        _mockUnitOfWork.Setup(x => x.InventoryIngredient.UpdateBatchByBatch(batch))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ConfirmAuditAsync(auditId, request);

        // Assert
        Assert.True(result);
        Assert.Null(batch.ExpiryDate);
    }

    #endregion

    #region CreateAuditAsync Tests

    // UTCID11: Test tạo audit thành công
    [Fact]
    public async Task CreateAuditAsync_WhenSuccessful_ReturnsTrue()
    {
        // Arrange
        var auditRecord = new AuditInventory
        {
            AuditId = "AUD010",
            BatchId = 1,
            PurchaseOrderId = "PO001",
            IngredientCode = "ING001",
            ingredientName = "Tomato",
            unit = "kg",
            OriginalQuantity = 100,
            CreatorId = 1,
            CreatedAt = DateTime.Now,
            CreatorName = "John Doe",
            CreatorPosition = "Manager",
            CreatorPhone = "0123456789",
            Reason = "Inventory check",
            AdjustmentQuantity = 5,
            IsAddition = true,
            AuditStatus = "Pending"
        };

        _mockUnitOfWork.Setup(x => x.AuditRepository.AddAsync(auditRecord))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateAuditAsync(auditRecord);

        // Assert
        Assert.True(result);
        _mockUnitOfWork.Verify(x => x.AuditRepository.AddAsync(auditRecord), Times.Once);
    }

    // UTCID12: Test tạo audit thất bại
    [Fact]
    public async Task CreateAuditAsync_WhenFailed_ReturnsFalse()
    {
        // Arrange
        var auditRecord = new AuditInventory
        {
            AuditId = "AUD011",
            BatchId = 2,
            PurchaseOrderId = "PO002",
            IngredientCode = "ING002",
            ingredientName = "Onion",
            unit = "kg",
            OriginalQuantity = 50,
            CreatorId = 2,
            CreatedAt = DateTime.Now,
            CreatorName = "Jane Smith",
            CreatorPosition = "Staff",
            CreatorPhone = "0987654321",
            Reason = "Damage check",
            AdjustmentQuantity = 3,
            IsAddition = false,
            AuditStatus = "Pending"
        };

        _mockUnitOfWork.Setup(x => x.AuditRepository.AddAsync(auditRecord))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CreateAuditAsync(auditRecord);

        // Assert
        Assert.False(result);
    }

    // UTCID13: Test với null audit record
    [Fact]
    public async Task CreateAuditAsync_WhenAuditRecordIsNull_ThrowsException()
    {
        // Arrange
        AuditInventory auditRecord = null;

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(
            () => _service.CreateAuditAsync(auditRecord)
        );
    }

    // UTCID14: Test với repository throw exception
    [Fact]
    public async Task CreateAuditAsync_WhenRepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var auditRecord = new AuditInventory
        {
            AuditId = "AUD012",
            BatchId = 3,
            PurchaseOrderId = "PO003",
            IngredientCode = "ING003",
            ingredientName = "Chicken",
            unit = "kg",
            OriginalQuantity = 30,
            CreatorId = 3,
            CreatedAt = DateTime.Now,
            CreatorName = "Bob Johnson",
            CreatorPosition = "Chef",
            CreatorPhone = "0111222333",
            Reason = "Quality check",
            AdjustmentQuantity = 2,
            IsAddition = true,
            AuditStatus = "Pending"
        };

        _mockUnitOfWork.Setup(x => x.AuditRepository.AddAsync(auditRecord))
            .ThrowsAsync(new Exception("Database constraint violation"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _service.CreateAuditAsync(auditRecord)
        );
    }

    // UTCID15: Test với AdjustmentQuantity = 0
    [Fact]
    public async Task CreateAuditAsync_WhenAdjustmentQuantityIsZero_ReturnsTrue()
    {
        // Arrange
        var auditRecord = new AuditInventory
        {
            AuditId = "AUD013",
            BatchId = 4,
            PurchaseOrderId = "PO004",
            IngredientCode = "ING004",
            ingredientName = "Salt",
            unit = "kg",
            OriginalQuantity = 20,
            CreatorId = 4,
            CreatedAt = DateTime.Now,
            CreatorName = "Alice Brown",
            CreatorPosition = "Supervisor",
            CreatorPhone = "0444555666",
            Reason = "Regular audit",
            AdjustmentQuantity = 0,
            IsAddition = true,
            AuditStatus = "Pending"
        };

        _mockUnitOfWork.Setup(x => x.AuditRepository.AddAsync(auditRecord))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateAuditAsync(auditRecord);

        // Assert
        Assert.True(result);
        _mockUnitOfWork.Verify(x => x.AuditRepository.AddAsync(auditRecord), Times.Once);
    }

    #endregion
}