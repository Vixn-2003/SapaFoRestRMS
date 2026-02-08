using Xunit;
using Moq;
using AutoMapper;
using System;
using System.Threading.Tasks;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.DTOs.Inventory;
using DataAccessLayer.UnitOfWork.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;

/// <summary>
/// Unit tests for StockTransactionService
/// 
/// Function: AddIdNewStock
/// - Nhận StockTransactionDTO từ client
/// - Map DTO sang StockTransaction entity
/// - Gọi repository để thêm vào database
/// - Trả về kết quả thành công/thất bại
/// - Test với các trường hợp: Export, Import, null values, boundary values
/// </summary>
public class StockTransactionServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IStockTransactionRepository> _mockStockTransactionRepo;
    private readonly Mock<IMapper> _mockMapper;
    private readonly StockTransactionService _service;

    public StockTransactionServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockStockTransactionRepo = new Mock<IStockTransactionRepository>();
        _mockMapper = new Mock<IMapper>();

        // Setup UnitOfWork to return mocked repository
        _mockUnitOfWork.Setup(x => x.StockTransaction).Returns(_mockStockTransactionRepo.Object);

        _service = new StockTransactionService(_mockUnitOfWork.Object, _mockMapper.Object);
    }

    #region AddIdNewStock Tests

    // UTCID01: Test thêm Export transaction thành công
    [Fact]
    public async Task AddIdNewStock_WithValidExportDTO_ReturnsTrue()
    {
        // Arrange
        var dto = new StockTransactionDTO
        {
            IngredientId = 1,
            BatchId = 10,
            Type = "Export",
            Quantity = 50,
            TransactionDate = DateTime.Now,
            Note = "Export for Order #123"
        };

        var entity = new StockTransaction
        {
            IngredientId = 1,
            BatchId = 10,
            Type = "Export",
            Quantity = 50,
            TransactionDate = dto.TransactionDate,
            Note = "Export for Order #123"
        };

        _mockMapper.Setup(m => m.Map<StockTransaction>(dto)).Returns(entity);
        _mockStockTransactionRepo.Setup(r => r.AddNewStockTransaction(entity))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AddIdNewStock(dto);

        // Assert
        Assert.True(result);
        _mockMapper.Verify(m => m.Map<StockTransaction>(dto), Times.Once);
        _mockStockTransactionRepo.Verify(r => r.AddNewStockTransaction(It.IsAny<StockTransaction>()), Times.Once);
    }

    // UTCID02: Test thêm Import transaction thành công
    [Fact]
    public async Task AddIdNewStock_WithValidImportDTO_ReturnsTrue()
    {
        // Arrange
        var dto = new StockTransactionDTO
        {
            IngredientId = 2,
            BatchId = 20,
            Type = "Import",
            Quantity = 100,
            TransactionDate = DateTime.Now,
            Note = "Import from Supplier ABC"
        };

        var entity = new StockTransaction
        {
            IngredientId = 2,
            BatchId = 20,
            Type = "Import",
            Quantity = 100,
            TransactionDate = dto.TransactionDate,
            Note = "Import from Supplier ABC"
        };

        _mockMapper.Setup(m => m.Map<StockTransaction>(dto)).Returns(entity);
        _mockStockTransactionRepo.Setup(r => r.AddNewStockTransaction(entity))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AddIdNewStock(dto);

        // Assert
        Assert.True(result);
        _mockMapper.Verify(m => m.Map<StockTransaction>(dto), Times.Once);
        _mockStockTransactionRepo.Verify(r => r.AddNewStockTransaction(It.IsAny<StockTransaction>()), Times.Once);
    }

    // UTCID03: Test với null DTO - mapper returns null, repo returns false
    [Fact]
    public async Task AddIdNewStock_WithNullDTO_ReturnsFalse()
    {
        // Arrange
        StockTransactionDTO dto = null;

        _mockMapper.Setup(m => m.Map<StockTransaction>(dto)).Returns((StockTransaction)null);
        _mockStockTransactionRepo.Setup(r => r.AddNewStockTransaction(null))
            .ReturnsAsync(false);

        // Act
        var result = await _service.AddIdNewStock(dto);

        // Assert
        Assert.False(result);
    }

    // UTCID04: Test với Quantity = 0 (boundary)
    [Fact]
    public async Task AddIdNewStock_WithZeroQuantity_ReturnsTrue()
    {
        // Arrange
        var dto = new StockTransactionDTO
        {
            IngredientId = 3,
            BatchId = 30,
            Type = "Export",
            Quantity = 0,
            TransactionDate = DateTime.Now,
            Note = "Zero quantity test"
        };

        var entity = new StockTransaction
        {
            IngredientId = 3,
            BatchId = 30,
            Type = "Export",
            Quantity = 0,
            TransactionDate = dto.TransactionDate,
            Note = "Zero quantity test"
        };

        _mockMapper.Setup(m => m.Map<StockTransaction>(dto)).Returns(entity);
        _mockStockTransactionRepo.Setup(r => r.AddNewStockTransaction(entity))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AddIdNewStock(dto);

        // Assert
        Assert.True(result);
    }

    // UTCID05: Test với Quantity âm (negative)
    [Fact]
    public async Task AddIdNewStock_WithNegativeQuantity_ReturnsTrue()
    {
        // Arrange
        var dto = new StockTransactionDTO
        {
            IngredientId = 4,
            BatchId = 40,
            Type = "Export",
            Quantity = -10,
            TransactionDate = DateTime.Now,
            Note = "Negative quantity for correction"
        };

        var entity = new StockTransaction
        {
            IngredientId = 4,
            BatchId = 40,
            Type = "Export",
            Quantity = -10,
            TransactionDate = dto.TransactionDate,
            Note = "Negative quantity for correction"
        };

        _mockMapper.Setup(m => m.Map<StockTransaction>(dto)).Returns(entity);
        _mockStockTransactionRepo.Setup(r => r.AddNewStockTransaction(entity))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AddIdNewStock(dto);

        // Assert
        Assert.True(result);
    }

    // UTCID06: Test với BatchId = null (optional)
    [Fact]
    public async Task AddIdNewStock_WithNullBatchId_ReturnsTrue()
    {
        // Arrange
        var dto = new StockTransactionDTO
        {
            IngredientId = 5,
            BatchId = null,
            Type = "Import",
            Quantity = 75,
            TransactionDate = DateTime.Now,
            Note = "Import without batch assignment"
        };

        var entity = new StockTransaction
        {
            IngredientId = 5,
            BatchId = null,
            Type = "Import",
            Quantity = 75,
            TransactionDate = dto.TransactionDate,
            Note = "Import without batch assignment"
        };

        _mockMapper.Setup(m => m.Map<StockTransaction>(dto)).Returns(entity);
        _mockStockTransactionRepo.Setup(r => r.AddNewStockTransaction(entity))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AddIdNewStock(dto);

        // Assert
        Assert.True(result);
    }

    // UTCID07: Test với TransactionDate = null
    [Fact]
    public async Task AddIdNewStock_WithNullTransactionDate_ReturnsTrue()
    {
        // Arrange
        var dto = new StockTransactionDTO
        {
            IngredientId = 6,
            BatchId = 60,
            Type = "Export",
            Quantity = 25,
            TransactionDate = null,
            Note = "No transaction date"
        };

        var entity = new StockTransaction
        {
            IngredientId = 6,
            BatchId = 60,
            Type = "Export",
            Quantity = 25,
            TransactionDate = null,
            Note = "No transaction date"
        };

        _mockMapper.Setup(m => m.Map<StockTransaction>(dto)).Returns(entity);
        _mockStockTransactionRepo.Setup(r => r.AddNewStockTransaction(entity))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AddIdNewStock(dto);

        // Assert
        Assert.True(result);
    }

    // UTCID08: Test với Note = null (optional)
    [Fact]
    public async Task AddIdNewStock_WithNullNote_ReturnsTrue()
    {
        // Arrange
        var dto = new StockTransactionDTO
        {
            IngredientId = 7,
            BatchId = 70,
            Type = "Import",
            Quantity = 150,
            TransactionDate = DateTime.Now,
            Note = null
        };

        var entity = new StockTransaction
        {
            IngredientId = 7,
            BatchId = 70,
            Type = "Import",
            Quantity = 150,
            TransactionDate = dto.TransactionDate,
            Note = null
        };

        _mockMapper.Setup(m => m.Map<StockTransaction>(dto)).Returns(entity);
        _mockStockTransactionRepo.Setup(r => r.AddNewStockTransaction(entity))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AddIdNewStock(dto);

        // Assert
        Assert.True(result);
    }

    // UTCID09: Test với IngredientId = 0 (boundary)
    [Fact]
    public async Task AddIdNewStock_WithZeroIngredientId_ReturnsTrue()
    {
        // Arrange
        var dto = new StockTransactionDTO
        {
            IngredientId = 0,
            BatchId = 80,
            Type = "Export",
            Quantity = 30,
            TransactionDate = DateTime.Now
        };

        var entity = new StockTransaction
        {
            IngredientId = 0,
            BatchId = 80,
            Type = "Export",
            Quantity = 30,
            TransactionDate = dto.TransactionDate
        };

        _mockMapper.Setup(m => m.Map<StockTransaction>(dto)).Returns(entity);
        _mockStockTransactionRepo.Setup(r => r.AddNewStockTransaction(entity))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AddIdNewStock(dto);

        // Assert
        Assert.True(result);
    }

    // UTCID10: Test với large Quantity value
    [Fact]
    public async Task AddIdNewStock_WithLargeQuantity_ReturnsTrue()
    {
        // Arrange
        var dto = new StockTransactionDTO
        {
            IngredientId = 8,
            BatchId = 90,
            Type = "Import",
            Quantity = 999999.99m,
            TransactionDate = DateTime.Now,
            Note = "Large quantity import"
        };

        var entity = new StockTransaction
        {
            IngredientId = 8,
            BatchId = 90,
            Type = "Import",
            Quantity = 999999.99m,
            TransactionDate = dto.TransactionDate,
            Note = "Large quantity import"
        };

        _mockMapper.Setup(m => m.Map<StockTransaction>(dto)).Returns(entity);
        _mockStockTransactionRepo.Setup(r => r.AddNewStockTransaction(entity))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AddIdNewStock(dto);

        // Assert
        Assert.True(result);
    }

    // UTCID11: Test repository returns false
    [Fact]
    public async Task AddIdNewStock_WhenRepositoryFails_ReturnsFalse()
    {
        // Arrange
        var dto = new StockTransactionDTO
        {
            IngredientId = 9,
            BatchId = 100,
            Type = "Export",
            Quantity = 40,
            TransactionDate = DateTime.Now
        };

        var entity = new StockTransaction
        {
            IngredientId = 9,
            BatchId = 100,
            Type = "Export",
            Quantity = 40,
            TransactionDate = dto.TransactionDate
        };

        _mockMapper.Setup(m => m.Map<StockTransaction>(dto)).Returns(entity);
        _mockStockTransactionRepo.Setup(r => r.AddNewStockTransaction(entity))
            .ReturnsAsync(false); // Repository fails

        // Act
        var result = await _service.AddIdNewStock(dto);

        // Assert
        Assert.False(result);
    }

    // UTCID12: Test với different Type values
    [Theory]
    [InlineData("Export")]
    [InlineData("Import")]
    [InlineData("Adjustment")]
    [InlineData("Loss")]
    public async Task AddIdNewStock_WithDifferentTypes_ReturnsTrue(string type)
    {
        // Arrange
        var dto = new StockTransactionDTO
        {
            IngredientId = 10,
            BatchId = 110,
            Type = type,
            Quantity = 55,
            TransactionDate = DateTime.Now
        };

        var entity = new StockTransaction
        {
            IngredientId = 10,
            BatchId = 110,
            Type = type,
            Quantity = 55,
            TransactionDate = dto.TransactionDate
        };

        _mockMapper.Setup(m => m.Map<StockTransaction>(dto)).Returns(entity);
        _mockStockTransactionRepo.Setup(r => r.AddNewStockTransaction(entity))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AddIdNewStock(dto);

        // Assert
        Assert.True(result);
    }

    // UTCID13: Test mapper is called correctly
    [Fact]
    public async Task AddIdNewStock_VerifyMapperIsCalled_WithCorrectDTO()
    {
        // Arrange
        var dto = new StockTransactionDTO
        {
            IngredientId = 11,
            BatchId = 120,
            Type = "Export",
            Quantity = 60,
            TransactionDate = DateTime.Now,
            Note = "Verify mapper test"
        };

        var entity = new StockTransaction();

        _mockMapper.Setup(m => m.Map<StockTransaction>(dto)).Returns(entity);
        _mockStockTransactionRepo.Setup(r => r.AddNewStockTransaction(It.IsAny<StockTransaction>()))
            .ReturnsAsync(true);

        // Act
        await _service.AddIdNewStock(dto);

        // Assert
        _mockMapper.Verify(m => m.Map<StockTransaction>(dto), Times.Once);
        _mockMapper.Verify(m => m.Map<StockTransaction>(It.IsAny<StockTransactionDTO>()), Times.Once);
    }

    // UTCID14: Test repository is called with mapped entity
    [Fact]
    public async Task AddIdNewStock_VerifyRepositoryIsCalled_WithMappedEntity()
    {
        // Arrange
        var dto = new StockTransactionDTO
        {
            IngredientId = 12,
            BatchId = 130,
            Type = "Import",
            Quantity = 70,
            TransactionDate = DateTime.Now
        };

        var entity = new StockTransaction
        {
            IngredientId = 12,
            BatchId = 130,
            Type = "Import",
            Quantity = 70
        };

        _mockMapper.Setup(m => m.Map<StockTransaction>(dto)).Returns(entity);
        _mockStockTransactionRepo.Setup(r => r.AddNewStockTransaction(entity))
            .ReturnsAsync(true);

        // Act
        await _service.AddIdNewStock(dto);

        // Assert
        _mockStockTransactionRepo.Verify(r => r.AddNewStockTransaction(entity), Times.Once);
    }

    // UTCID15: Test with all optional fields null
    [Fact]
    public async Task AddIdNewStock_WithAllOptionalFieldsNull_ReturnsTrue()
    {
        // Arrange
        var dto = new StockTransactionDTO
        {
            IngredientId = 13,
            Type = "Export",
            Quantity = 80,
            BatchId = null,
            TransactionDate = null,
            Note = null,
            IngredientName = null,
            BatchName = null
        };

        var entity = new StockTransaction
        {
            IngredientId = 13,
            Type = "Export",
            Quantity = 80,
            BatchId = null,
            TransactionDate = null,
            Note = null
        };

        _mockMapper.Setup(m => m.Map<StockTransaction>(dto)).Returns(entity);
        _mockStockTransactionRepo.Setup(r => r.AddNewStockTransaction(entity))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AddIdNewStock(dto);

        // Assert
        Assert.True(result);
    }

    // UTCID16: Test exception from repository is propagated
    [Fact]
    public async Task AddIdNewStock_WhenRepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var dto = new StockTransactionDTO
        {
            IngredientId = 14,
            Type = "Export",
            Quantity = 90
        };

        var entity = new StockTransaction();

        _mockMapper.Setup(m => m.Map<StockTransaction>(dto)).Returns(entity);
        _mockStockTransactionRepo.Setup(r => r.AddNewStockTransaction(entity))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _service.AddIdNewStock(dto)
        );
    }

    #endregion
}