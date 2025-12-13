using Xunit;
using Moq;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.DTOs.Inventory;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;

/// <summary>
/// Unit tests for PurchaseOrderService
/// 
/// Function: AddIdNewIngredient
/// - Thêm IngredientId mới vào PurchaseOrderDetail
/// - Kiểm tra DetailOrder và Ingredient tồn tại
/// - Xử lý trường hợp thêm thành công và thất bại
/// 
/// Function: ConfirmOrder
/// - Xác nhận đơn hàng nhập (PurchaseOrder)
/// - Cập nhật trạng thái (status), người xác nhận (idChecker), thời gian xác nhận
/// - Validate PurchaseOrderId, status values
/// 
/// Function: CreateImportOrderAsync
/// - Tạo mới đơn nhập hàng (ImportOrder) và chi tiết (ImportDetails)
/// - Mapping từ ImportOrder/ImportDetail sang PurchaseOrder/PurchaseOrderDetail
/// - Validate dữ liệu đầu vào (null checks, empty lists)
/// - Transaction handling (tạo order + details cùng lúc)
/// </summary>
public class PurchaseOrderServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PurchaseOrderService _service;

    public PurchaseOrderServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _service = new PurchaseOrderService(_mockUnitOfWork.Object, _mockMapper.Object);
    }

    #region AddIdNewIngredient Tests

    // UTCID01: Test thêm IngredientId thành công
    [Fact]
    public async Task AddIdNewIngredient_WhenSuccessful_ReturnsTrue()
    {
        // Arrange
        int idDetailOrder = 1;
        int idIngredient = 10;

        _mockUnitOfWork.Setup(x => x.PurchaseOrder.AddIdNewIngredient(idDetailOrder, idIngredient))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AddIdNewIngredient(idDetailOrder, idIngredient);

        // Assert
        Assert.True(result);
        _mockUnitOfWork.Verify(x => x.PurchaseOrder.AddIdNewIngredient(idDetailOrder, idIngredient), Times.Once);
    }

    // UTCID02: Test thêm IngredientId thất bại
    [Fact]
    public async Task AddIdNewIngredient_WhenFailed_ReturnsFalse()
    {
        // Arrange
        int idDetailOrder = 999;
        int idIngredient = 10;

        _mockUnitOfWork.Setup(x => x.PurchaseOrder.AddIdNewIngredient(idDetailOrder, idIngredient))
            .ReturnsAsync(false);

        // Act
        var result = await _service.AddIdNewIngredient(idDetailOrder, idIngredient);

        // Assert
        Assert.False(result);
    }

    // UTCID03: Test với idDetailOrder = 0 (boundary)
    [Fact]
    public async Task AddIdNewIngredient_WithZeroDetailOrderId_ReturnsFalse()
    {
        // Arrange
        int idDetailOrder = 0;
        int idIngredient = 10;

        _mockUnitOfWork.Setup(x => x.PurchaseOrder.AddIdNewIngredient(idDetailOrder, idIngredient))
            .ReturnsAsync(false);

        // Act
        var result = await _service.AddIdNewIngredient(idDetailOrder, idIngredient);

        // Assert
        Assert.False(result);
    }

    // UTCID04: Test với idIngredient = 0 (boundary)
    [Fact]
    public async Task AddIdNewIngredient_WithZeroIngredientId_ReturnsFalse()
    {
        // Arrange
        int idDetailOrder = 1;
        int idIngredient = 0;

        _mockUnitOfWork.Setup(x => x.PurchaseOrder.AddIdNewIngredient(idDetailOrder, idIngredient))
            .ReturnsAsync(false);

        // Act
        var result = await _service.AddIdNewIngredient(idDetailOrder, idIngredient);

        // Assert
        Assert.False(result);
    }

    // UTCID05: Test repository throw exception
    [Fact]
    public async Task AddIdNewIngredient_WhenRepositoryThrowsException_PropagatesException()
    {
        // Arrange
        int idDetailOrder = 1;
        int idIngredient = 10;

        _mockUnitOfWork.Setup(x => x.PurchaseOrder.AddIdNewIngredient(idDetailOrder, idIngredient))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _service.AddIdNewIngredient(idDetailOrder, idIngredient)
        );
    }

    // UTCID06: Test với nhiều cặp ID khác nhau
    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 20)]
    [InlineData(3, 30)]
    public async Task AddIdNewIngredient_WithDifferentIds_ReturnsTrue(int detailId, int ingredientId)
    {
        // Arrange
        _mockUnitOfWork.Setup(x => x.PurchaseOrder.AddIdNewIngredient(detailId, ingredientId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AddIdNewIngredient(detailId, ingredientId);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region ConfirmOrder Tests

    // UTCID07: Test xác nhận order thành công
    [Fact]
    public async Task ConfirmOrder_WhenSuccessful_ReturnsTrue()
    {
        // Arrange
        string purchaseOrderId = "PO001";
        int idChecker = 5;
        DateTime time = DateTime.Now;
        string status = "Confirmed";

        _mockUnitOfWork.Setup(x => x.PurchaseOrder.ConfirmOrder(purchaseOrderId, idChecker, time, status))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ConfirmOrder(purchaseOrderId, idChecker, time, status);

        // Assert
        Assert.True(result);
        _mockUnitOfWork.Verify(x => x.PurchaseOrder.ConfirmOrder(purchaseOrderId, idChecker, time, status), Times.Once);
    }

    // UTCID08: Test xác nhận order thất bại (order không tồn tại)
    [Fact]
    public async Task ConfirmOrder_WhenOrderNotFound_ReturnsFalse()
    {
        // Arrange
        string purchaseOrderId = "PO999";
        int idChecker = 5;
        DateTime time = DateTime.Now;
        string status = "Confirmed";

        _mockUnitOfWork.Setup(x => x.PurchaseOrder.ConfirmOrder(purchaseOrderId, idChecker, time, status))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ConfirmOrder(purchaseOrderId, idChecker, time, status);

        // Assert
        Assert.False(result);
    }

    // UTCID09: Test với PurchaseOrderId null
    [Fact]
    public async Task ConfirmOrder_WithNullOrderId_ReturnsFalse()
    {
        // Arrange
        string purchaseOrderId = null;
        int idChecker = 5;
        DateTime time = DateTime.Now;
        string status = "Confirmed";

        _mockUnitOfWork.Setup(x => x.PurchaseOrder.ConfirmOrder(purchaseOrderId, idChecker, time, status))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ConfirmOrder(purchaseOrderId, idChecker, time, status);

        // Assert
        Assert.False(result);
    }

    // UTCID10: Test với idChecker = 0 (boundary)
    [Fact]
    public async Task ConfirmOrder_WithZeroCheckerId_ReturnsFalse()
    {
        // Arrange
        string purchaseOrderId = "PO001";
        int idChecker = 0;
        DateTime time = DateTime.Now;
        string status = "Confirmed";

        _mockUnitOfWork.Setup(x => x.PurchaseOrder.ConfirmOrder(purchaseOrderId, idChecker, time, status))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ConfirmOrder(purchaseOrderId, idChecker, time, status);

        // Assert
        Assert.False(result);
    }

    // UTCID11: Test với status khác nhau
    [Theory]
    [InlineData("Confirmed")]
    [InlineData("Rejected")]
    [InlineData("Pending")]
    public async Task ConfirmOrder_WithDifferentStatuses_ReturnsTrue(string status)
    {
        // Arrange
        string purchaseOrderId = "PO001";
        int idChecker = 5;
        DateTime time = DateTime.Now;

        _mockUnitOfWork.Setup(x => x.PurchaseOrder.ConfirmOrder(purchaseOrderId, idChecker, time, status))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ConfirmOrder(purchaseOrderId, idChecker, time, status);

        // Assert
        Assert.True(result);
    }

    // UTCID12: Test repository throw exception
    [Fact]
    public async Task ConfirmOrder_WhenRepositoryThrowsException_PropagatesException()
    {
        // Arrange
        string purchaseOrderId = "PO001";
        int idChecker = 5;
        DateTime time = DateTime.Now;
        string status = "Confirmed";

        _mockUnitOfWork.Setup(x => x.PurchaseOrder.ConfirmOrder(purchaseOrderId, idChecker, time, status))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _service.ConfirmOrder(purchaseOrderId, idChecker, time, status)
        );
    }

    #endregion

    #region CreateImportOrderAsync Tests

    // UTCID13: Test tạo import order thành công
    [Fact]
    public async Task CreateImportOrderAsync_WhenSuccessful_ReturnsTrue()
    {
        // Arrange
        var importOrder = new ImportOrder
        {
            ImportCode = "IMP001",
            SupplierId = 1,
            CreatorId = 10,
            ImportDate = DateTime.Now,
            ProofImagePath = "path/to/image.jpg",
            Status = "Processing",
            TotalAmount = 1000000
        };

        var importDetails = new List<ImportDetail>
        {
            new ImportDetail
            {
                IngredientId = 1,
                IngredientCode = "ING001",
                IngredientName = "Tomato",
                Unit = "kg",
                Quantity = 100,
                UnitPrice = 5000,
                TotalPrice = 500000,
                WarehouseName = "Main Warehouse",
                ExpiryDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(6))
            },
            new ImportDetail
            {
                IngredientId = 2,
                IngredientCode = "ING002",
                IngredientName = "Onion",
                Unit = "kg",
                Quantity = 50,
                UnitPrice = 10000,
                TotalPrice = 500000,
                WarehouseName = "Main Warehouse",
                ExpiryDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(3))
            }
        };

        _mockUnitOfWork.Setup(x => x.PurchaseOrder.CreatePurchaseOrderAsync(
            It.IsAny<PurchaseOrder>(),
            It.IsAny<List<PurchaseOrderDetail>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateImportOrderAsync(importOrder, importDetails);

        // Assert
        Assert.True(result);
        _mockUnitOfWork.Verify(x => x.PurchaseOrder.CreatePurchaseOrderAsync(
            It.Is<PurchaseOrder>(po =>
                po.PurchaseOrderId == "IMP001" &&
                po.SupplierId == 1 &&
                po.IdCreator == 10 &&
                po.Status == "Processing"),
            It.Is<List<PurchaseOrderDetail>>(details =>
                details.Count == 2 &&
                details[0].IngredientId == 1 &&
                details[1].IngredientId == 2)), Times.Once);
    }

    // UTCID14: Test với ImportOrder null
    [Fact]
    public async Task CreateImportOrderAsync_WithNullImportOrder_ThrowsException()
    {
        // Arrange
        ImportOrder importOrder = null;
        var importDetails = new List<ImportDetail>();

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(
            () => _service.CreateImportOrderAsync(importOrder, importDetails)
        );
    }

    // UTCID15: Test với ImportDetails null
    [Fact]
    public async Task CreateImportOrderAsync_WithNullImportDetails_ThrowsException()
    {
        // Arrange
        var importOrder = new ImportOrder
        {
            ImportCode = "IMP001",
            SupplierId = 1,
            CreatorId = 10,
            ImportDate = DateTime.Now
        };
        List<ImportDetail> importDetails = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.CreateImportOrderAsync(importOrder, importDetails)
        );
    }

    // UTCID16: Test với ImportDetails rỗng
    [Fact]
    public async Task CreateImportOrderAsync_WithEmptyImportDetails_ReturnsTrue()
    {
        // Arrange
        var importOrder = new ImportOrder
        {
            ImportCode = "IMP002",
            SupplierId = 2,
            CreatorId = 10,
            ImportDate = DateTime.Now,
            Status = "Processing"
        };
        var importDetails = new List<ImportDetail>();

        _mockUnitOfWork.Setup(x => x.PurchaseOrder.CreatePurchaseOrderAsync(
            It.IsAny<PurchaseOrder>(),
            It.IsAny<List<PurchaseOrderDetail>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateImportOrderAsync(importOrder, importDetails);

        // Assert
        Assert.True(result);
        _mockUnitOfWork.Verify(x => x.PurchaseOrder.CreatePurchaseOrderAsync(
            It.IsAny<PurchaseOrder>(),
            It.Is<List<PurchaseOrderDetail>>(details => details.Count == 0)), Times.Once);
    }

    // UTCID17: Test repository throw exception
    [Fact]
    public async Task CreateImportOrderAsync_WhenRepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var importOrder = new ImportOrder
        {
            ImportCode = "IMP003",
            SupplierId = 3,
            CreatorId = 10,
            ImportDate = DateTime.Now
        };
        var importDetails = new List<ImportDetail>
        {
            new ImportDetail
            {
                IngredientId = 1,
                IngredientCode = "ING001",
                IngredientName = "Test",
                Unit = "kg",
                Quantity = 10,
                UnitPrice = 1000,
                TotalPrice = 10000,
                WarehouseName = "Warehouse"
            }
        };

        _mockUnitOfWork.Setup(x => x.PurchaseOrder.CreatePurchaseOrderAsync(
            It.IsAny<PurchaseOrder>(),
            It.IsAny<List<PurchaseOrderDetail>>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _service.CreateImportOrderAsync(importOrder, importDetails)
        );
    }

    // UTCID18: Test mapping fields correctly
    [Fact]
    public async Task CreateImportOrderAsync_MapsFieldsCorrectly_ReturnsTrue()
    {
        // Arrange
        var importOrder = new ImportOrder
        {
            ImportCode = "IMP004",
            SupplierId = 4,
            CreatorId = 15,
            ImportDate = new DateTime(2024, 12, 15),
            ProofImagePath = "images/proof.jpg",
            Status = "Processing",
            TotalAmount = 2000000
        };

        var importDetails = new List<ImportDetail>
        {
            new ImportDetail
            {
                IngredientId = 5,
                IngredientCode = "ING005",
                IngredientName = "Chicken",
                Unit = "kg",
                Quantity = 20,
                UnitPrice = 80000,
                TotalPrice = 1600000,
                WarehouseName = "Cold Storage",
                ExpiryDate = DateOnly.FromDateTime(DateTime.Now.AddDays(7))
            }
        };

        _mockUnitOfWork.Setup(x => x.PurchaseOrder.CreatePurchaseOrderAsync(
            It.IsAny<PurchaseOrder>(),
            It.IsAny<List<PurchaseOrderDetail>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateImportOrderAsync(importOrder, importDetails);

        // Assert
        Assert.True(result);
        _mockUnitOfWork.Verify(x => x.PurchaseOrder.CreatePurchaseOrderAsync(
            It.Is<PurchaseOrder>(po =>
                po.PurchaseOrderId == "IMP004" &&
                po.SupplierId == 4 &&
                po.IdCreator == 15 &&
                po.UrlImg == "images/proof.jpg" &&
                po.Status == "Processing"),
            It.Is<List<PurchaseOrderDetail>>(details =>
                details.Count == 1 &&
                details[0].IngredientCode == "ING005" &&
                details[0].IngredientName == "Chicken" &&
                details[0].Quantity == 20 &&
                details[0].UnitPrice == 80000 &&
                details[0].Subtotal == 1600000 &&
                details[0].WarehouseName == "Cold Storage")), Times.Once);
    }

    // UTCID19: Test với nhiều details (3+ items)
    [Fact]
    public async Task CreateImportOrderAsync_WithMultipleDetails_ReturnsTrue()
    {
        // Arrange
        var importOrder = new ImportOrder
        {
            ImportCode = "IMP005",
            SupplierId = 5,
            CreatorId = 20,
            ImportDate = DateTime.Now
        };

        var importDetails = new List<ImportDetail>
        {
            new ImportDetail { IngredientId = 1, IngredientCode = "ING001", IngredientName = "Item1", Unit = "kg", Quantity = 10, UnitPrice = 1000, TotalPrice = 10000, WarehouseName = "WH1" },
            new ImportDetail { IngredientId = 2, IngredientCode = "ING002", IngredientName = "Item2", Unit = "kg", Quantity = 20, UnitPrice = 2000, TotalPrice = 40000, WarehouseName = "WH2" },
            new ImportDetail { IngredientId = 3, IngredientCode = "ING003", IngredientName = "Item3", Unit = "kg", Quantity = 30, UnitPrice = 3000, TotalPrice = 90000, WarehouseName = "WH3" },
            new ImportDetail { IngredientId = 4, IngredientCode = "ING004", IngredientName = "Item4", Unit = "kg", Quantity = 40, UnitPrice = 4000, TotalPrice = 160000, WarehouseName = "WH4" }
        };

        _mockUnitOfWork.Setup(x => x.PurchaseOrder.CreatePurchaseOrderAsync(
            It.IsAny<PurchaseOrder>(),
            It.IsAny<List<PurchaseOrderDetail>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateImportOrderAsync(importOrder, importDetails);

        // Assert
        Assert.True(result);
        _mockUnitOfWork.Verify(x => x.PurchaseOrder.CreatePurchaseOrderAsync(
            It.IsAny<PurchaseOrder>(),
            It.Is<List<PurchaseOrderDetail>>(details => details.Count == 4)), Times.Once);
    }

    // UTCID20: Test repository returns false
    [Fact]
    public async Task CreateImportOrderAsync_WhenRepositoryReturnsFalse_ReturnsFalse()
    {
        // Arrange
        var importOrder = new ImportOrder
        {
            ImportCode = "IMP006",
            SupplierId = 6,
            CreatorId = 25,
            ImportDate = DateTime.Now
        };
        var importDetails = new List<ImportDetail>
        {
            new ImportDetail { IngredientId = 1, IngredientCode = "ING001", IngredientName = "Test", Unit = "kg", Quantity = 5, UnitPrice = 500, TotalPrice = 2500, WarehouseName = "WH" }
        };

        _mockUnitOfWork.Setup(x => x.PurchaseOrder.CreatePurchaseOrderAsync(
            It.IsAny<PurchaseOrder>(),
            It.IsAny<List<PurchaseOrderDetail>>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CreateImportOrderAsync(importOrder, importDetails);

        // Assert
        Assert.False(result);
    }

    #endregion
}