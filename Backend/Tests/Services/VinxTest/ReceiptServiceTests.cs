using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Enums;
using DomainAccessLayer.Models;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services.VinxTest;

/// <summary>
/// Unit Tests cho ReceiptService
/// Test độc lập các phương thức trong ReceiptService sử dụng xUnit + Moq
/// </summary>
public class ReceiptServiceTests : IDisposable
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<ReceiptService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IPaymentRepository> _mockPaymentRepository;
    private readonly Mock<ICloudinaryService> _mockCloudinaryService;
    private readonly string _tempWebRootPath;
    private readonly ReceiptService _receiptService;

    public ReceiptServiceTests()
    {
        // Configure QuestPDF license for testing
        QuestPDF.Settings.License = LicenseType.Community;

        // Khởi tạo mocks
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<ReceiptService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockPaymentRepository = new Mock<IPaymentRepository>();
        _mockCloudinaryService = new Mock<ICloudinaryService>();

        // Setup IUnitOfWork.Payments trả về mock repository
        _mockUnitOfWork.Setup(uow => uow.Payments).Returns(_mockPaymentRepository.Object);

        // Setup configuration
        var mockReceiptSection = new Mock<IConfigurationSection>();
        mockReceiptSection.Setup(s => s["RestaurantName"]).Returns("Test Restaurant");
        mockReceiptSection.Setup(s => s["RestaurantAddress"]).Returns("123 Test Street");
        mockReceiptSection.Setup(s => s["RestaurantPhone"]).Returns("0123456789");

        var mockReceiptSettingsSection = new Mock<IConfigurationSection>();
        mockReceiptSettingsSection.Setup(s => s["RestaurantName"]).Returns("Test Restaurant");
        mockReceiptSettingsSection.Setup(s => s["RestaurantAddress"]).Returns("123 Test Street");
        mockReceiptSettingsSection.Setup(s => s["RestaurantPhone"]).Returns("0123456789");

        _mockConfiguration.Setup(c => c.GetSection("ReceiptSettings")).Returns(mockReceiptSettingsSection.Object);

        // Create temporary directory for web root path
        _tempWebRootPath = Path.Combine(Path.GetTempPath(), $"ReceiptTest_{Guid.NewGuid()}");

        // Setup service provider to return CloudinaryService (optional)
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(ICloudinaryService)))
            .Returns(_mockCloudinaryService.Object);

        // Khởi tạo ReceiptService với mocked dependencies
        _receiptService = new ReceiptService(
            _mockUnitOfWork.Object,
            _tempWebRootPath,
            _mockLogger.Object,
            _mockConfiguration.Object,
            _mockServiceProvider.Object
        );
    }

    public void Dispose()
    {
        // Clean up temporary directory
        if (Directory.Exists(_tempWebRootPath))
        {
            try
            {
                Directory.Delete(_tempWebRootPath, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #region Test Data Helpers

    /// <summary>
    /// Tạo Order test data với status Paid
    /// </summary>
    private Order CreateTestPaidOrder(int orderId = 1, int? customerId = 1, int? reservationId = null)
    {
        return new Order
        {
            OrderId = orderId,
            Status = "Paid",
            CustomerId = customerId,
            ReservationId = reservationId,
            OrderType = "DineIn",
            TotalAmount = 230000,
            CreatedAt = DateTime.UtcNow,
            OrderDetails = new List<OrderDetail>
            {
                new OrderDetail
                {
                    OrderDetailId = 1,
                    OrderId = orderId,
                    MenuItemId = 1,
                    Quantity = 2,
                    QuantityUsed = 2,
                    UnitPrice = 100000,
                    Status = "Done",
                    CreatedAt = DateTime.UtcNow,
                    MenuItem = new MenuItem
                    {
                        MenuItemId = 1,
                        Name = "Test Item",
                        BillingType = ItemBillingType.KitchenPrepared
                    }
                }
            },
            Transactions = new List<Transaction>
            {
                new Transaction
                {
                    TransactionId = 1,
                    OrderId = orderId,
                    Amount = 230000,
                    PaymentMethod = "Cash",
                    Status = "Paid",
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow,
                    ConfirmedByUser = new User
                    {
                        UserId = 10,
                        FullName = "Test Staff"
                    }
                }
            },
            Payments = new List<Payment>
            {
                new Payment
                {
                    PaymentId = 1,
                    OrderId = orderId,
                    DiscountAmount = 0,
                    PaymentDate = DateTime.UtcNow
                }
            },
            Customer = customerId.HasValue ? new Customer
            {
                CustomerId = customerId.Value,
                User = new User
                {
                    UserId = customerId.Value,
                    FullName = "Test Customer",
                    Phone = "0123456789",
                    Email = "customer@test.com"
                }
            } : null,
            Reservation = reservationId.HasValue ? new Reservation
            {
                ReservationId = reservationId.Value,
                CustomerId = customerId ?? 1,
                Status = "Completed",
                ReservationTables = new List<ReservationTable>
                {
                    new ReservationTable
                    {
                        ReservationTableId = 1,
                        ReservationId = reservationId.Value,
                        TableId = 1,
                        Table = new Table
                        {
                            TableId = 1,
                            TableNumber = "T01"
                        }
                    }
                }
            } : null
        };
    }

    #endregion

    #region Test 1: GenerateReceiptPdfAsync

    [Fact]
    public async Task GenerateReceiptPdfAsync_GeneratesPdf_WhenOrderIsPaid()
    {
        // Arrange
        var orderId = 1;
        var order = CreateTestPaidOrder(orderId);

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var result = await _receiptService.GenerateReceiptPdfAsync(orderId);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("receipts");
        result.Should().Contain("RMS");
        
        // Verify PDF file was created
        var expectedPath = Path.Combine(_tempWebRootPath, "receipts", $"RMS{orderId:D6}.pdf");
        File.Exists(expectedPath).Should().BeTrue();
        
        _mockPaymentRepository.Verify(repo => repo.GetOrderWithItemsAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task GenerateReceiptPdfAsync_ThrowsException_WhenOrderNotFound()
    {
        // Arrange
        var orderId = 999;

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _receiptService.GenerateReceiptPdfAsync(orderId));
    }

    [Fact]
    public async Task GenerateReceiptPdfAsync_ThrowsException_WhenOrderNotPaid()
    {
        // Arrange
        var orderId = 1;
        var order = CreateTestPaidOrder(orderId);
        order.Status = "Confirmed"; // Not paid

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _receiptService.GenerateReceiptPdfAsync(orderId));
    }

    [Fact]
    public async Task GenerateReceiptPdfAsync_GeneratesPdf_WithDiscount()
    {
        // Arrange
        var orderId = 1;
        var order = CreateTestPaidOrder(orderId);
        order.Payments = new List<Payment>
        {
            new Payment
            {
                PaymentId = 1,
                OrderId = orderId,
                DiscountAmount = 10000,
                PaymentDate = DateTime.UtcNow
            }
        };

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var result = await _receiptService.GenerateReceiptPdfAsync(orderId);

        // Assert
        result.Should().NotBeNullOrEmpty();
        var expectedPath = Path.Combine(_tempWebRootPath, "receipts", $"RMS{orderId:D6}.pdf");
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateReceiptPdfAsync_GeneratesPdf_WithCloudinaryUpload()
    {
        // Arrange
        var orderId = 1;
        var order = CreateTestPaidOrder(orderId);
        var cloudinaryUrl = "https://cloudinary.com/receipts/RMS000001.pdf";

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        _mockCloudinaryService
            .Setup(s => s.UploadPdfAsync(It.IsAny<byte[]>(), It.IsAny<string>(), "receipts"))
            .ReturnsAsync(cloudinaryUrl);

        // Act
        var result = await _receiptService.GenerateReceiptPdfAsync(orderId);

        // Assert
        result.Should().Be(cloudinaryUrl);
        _mockCloudinaryService.Verify(s => s.UploadPdfAsync(It.IsAny<byte[]>(), It.IsAny<string>(), "receipts"), Times.Once);
    }

    [Fact]
    public async Task GenerateReceiptPdfAsync_ReturnsLocalPath_WhenCloudinaryUploadFails()
    {
        // Arrange
        var orderId = 1;
        var order = CreateTestPaidOrder(orderId);

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        _mockCloudinaryService
            .Setup(s => s.UploadPdfAsync(It.IsAny<byte[]>(), It.IsAny<string>(), "receipts"))
            .ReturnsAsync((string?)null); // Upload fails

        // Act
        var result = await _receiptService.GenerateReceiptPdfAsync(orderId);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("receipts");
        result.Should().Contain("RMS");
        // Should return local path when Cloudinary fails
    }

    [Fact]
    public async Task GenerateReceiptPdfAsync_HandlesConsumptionBasedItems()
    {
        // Arrange
        var orderId = 1;
        var order = CreateTestPaidOrder(orderId);
        order.OrderDetails = new List<OrderDetail>
        {
            new OrderDetail
            {
                OrderDetailId = 1,
                OrderId = orderId,
                MenuItemId = 1,
                Quantity = 5,
                QuantityUsed = 3, // Less than quantity
                UnitPrice = 50000,
                Status = "Done",
                CreatedAt = DateTime.UtcNow,
                MenuItem = new MenuItem
                {
                    MenuItemId = 1,
                    Name = "Test Consumption Item",
                    BillingType = ItemBillingType.ConsumptionBased
                }
            }
        };

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var result = await _receiptService.GenerateReceiptPdfAsync(orderId);

        // Assert
        result.Should().NotBeNullOrEmpty();
        var expectedPath = Path.Combine(_tempWebRootPath, "receipts", $"RMS{orderId:D6}.pdf");
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateReceiptPdfAsync_HandlesComboItems()
    {
        // Arrange
        var orderId = 1;
        var order = CreateTestPaidOrder(orderId);
        order.OrderDetails = new List<OrderDetail>
        {
            new OrderDetail
            {
                OrderDetailId = 1,
                OrderId = orderId,
                ComboId = 1,
                Quantity = 1,
                UnitPrice = 200000,
                Status = "Done",
                CreatedAt = DateTime.UtcNow,
                Combo = new Combo
                {
                    ComboId = 1,
                    Name = "Test Combo"
                },
                OrderComboItems = new List<OrderComboItem>
                {
                    new OrderComboItem
                    {
                        OrderComboItemId = 1,
                        OrderDetailId = 1,
                        MenuItemId = 1,
                        Status = "Done",
                        MenuItem = new MenuItem
                        {
                            MenuItemId = 1,
                            Name = "Combo Item 1"
                        }
                    }
                }
            }
        };

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var result = await _receiptService.GenerateReceiptPdfAsync(orderId);

        // Assert
        result.Should().NotBeNullOrEmpty();
        var expectedPath = Path.Combine(_tempWebRootPath, "receipts", $"RMS{orderId:D6}.pdf");
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateReceiptPdfAsync_ExcludesRemovedItems()
    {
        // Arrange
        var orderId = 1;
        var order = CreateTestPaidOrder(orderId);
        order.OrderDetails = new List<OrderDetail>
        {
            new OrderDetail
            {
                OrderDetailId = 1,
                OrderId = orderId,
                MenuItemId = 1,
                Quantity = 2,
                UnitPrice = 100000,
                Status = "Done",
                CreatedAt = DateTime.UtcNow,
                MenuItem = new MenuItem
                {
                    MenuItemId = 1,
                    Name = "Active Item",
                    BillingType = ItemBillingType.KitchenPrepared
                }
            },
            new OrderDetail
            {
                OrderDetailId = 2,
                OrderId = orderId,
                MenuItemId = 2,
                Quantity = 1,
                UnitPrice = 50000,
                Status = "Removed", // Should be excluded
                CreatedAt = DateTime.UtcNow,
                MenuItem = new MenuItem
                {
                    MenuItemId = 2,
                    Name = "Removed Item",
                    BillingType = ItemBillingType.KitchenPrepared
                }
            }
        };

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var result = await _receiptService.GenerateReceiptPdfAsync(orderId);

        // Assert
        result.Should().NotBeNullOrEmpty();
        var expectedPath = Path.Combine(_tempWebRootPath, "receipts", $"RMS{orderId:D6}.pdf");
        File.Exists(expectedPath).Should().BeTrue();
        // Removed items should not be included in subtotal calculation
    }

    [Fact]
    public async Task GenerateReceiptPdfAsync_UsesConfiguration_ForRestaurantInfo()
    {
        // Arrange
        var orderId = 1;
        var order = CreateTestPaidOrder(orderId);

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var result = await _receiptService.GenerateReceiptPdfAsync(orderId);

        // Assert
        result.Should().NotBeNullOrEmpty();
        // PDF should be generated with restaurant info from configuration
        var expectedPath = Path.Combine(_tempWebRootPath, "receipts", $"RMS{orderId:D6}.pdf");
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateReceiptPdfAsync_HandlesMissingCustomerInfo()
    {
        // Arrange
        var orderId = 1;
        var order = CreateTestPaidOrder(orderId);
        order.Customer = null; // No customer

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var result = await _receiptService.GenerateReceiptPdfAsync(orderId);

        // Assert
        result.Should().NotBeNullOrEmpty();
        var expectedPath = Path.Combine(_tempWebRootPath, "receipts", $"RMS{orderId:D6}.pdf");
        File.Exists(expectedPath).Should().BeTrue();
        // Should use "Khách vãng lai" as default customer name
    }

    [Fact]
    public async Task GenerateReceiptPdfAsync_HandlesMissingReservation()
    {
        // Arrange
        var orderId = 1;
        var order = CreateTestPaidOrder(orderId);
        order.Reservation = null; // No reservation

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var result = await _receiptService.GenerateReceiptPdfAsync(orderId);

        // Assert
        result.Should().NotBeNullOrEmpty();
        var expectedPath = Path.Combine(_tempWebRootPath, "receipts", $"RMS{orderId:D6}.pdf");
        File.Exists(expectedPath).Should().BeTrue();
        // Should use "N/A" for table number
    }

    #endregion
}

