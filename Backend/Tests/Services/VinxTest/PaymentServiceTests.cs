using AutoMapper;
using BusinessAccessLayer.Constants;
using BusinessAccessLayer.DTOs.Payment;
using BusinessAccessLayer.DTOs.Kitchen;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Enums;
using DomainAccessLayer.Models;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services.VinxTest;

/// <summary>
/// Unit Tests cho PaymentService
/// Test độc lập các phương thức trong PaymentService sử dụng xUnit + Moq
/// </summary>
public class PaymentServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IKitchenDisplayService> _mockKitchenDisplayService;
    private readonly Mock<IPaymentRepository> _mockPaymentRepository;
    private readonly Mock<IOrderLockRepository> _mockOrderLockRepository;
    private readonly PaymentService _paymentService;

    public PaymentServiceTests()
    {
        // Khởi tạo mocks
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockKitchenDisplayService = new Mock<IKitchenDisplayService>();
        _mockPaymentRepository = new Mock<IPaymentRepository>();
        _mockOrderLockRepository = new Mock<IOrderLockRepository>();

        // Setup IUnitOfWork.Payments trả về mock repository
        _mockUnitOfWork.Setup(uow => uow.Payments).Returns(_mockPaymentRepository.Object);
        _mockUnitOfWork.Setup(uow => uow.OrderLocks).Returns(_mockOrderLockRepository.Object);

        // Setup SaveChangesAsync
        _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync()).ReturnsAsync(1);

        // Khởi tạo PaymentService với mocked dependencies
        _paymentService = new PaymentService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockAuditLogService.Object,
            _mockServiceProvider.Object,
            _mockKitchenDisplayService.Object
        );
    }

    #region Test Data Helpers

    /// <summary>
    /// Tạo Order test data
    /// </summary>
    private Order CreateTestOrder(int orderId = 1, string status = "Confirmed", int? customerId = 1, int? reservationId = null)
    {
        return new Order
        {
            OrderId = orderId,
            Status = status,
            CustomerId = customerId,
            ReservationId = reservationId,
            OrderType = "DineIn",
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
                Status = "Active",
                Customer = new Customer
                {
                    CustomerId = customerId ?? 1,
                    User = new User
                    {
                        UserId = customerId ?? 1,
                        FullName = "Test Customer",
                        Phone = "0123456789",
                        Email = "customer@test.com"
                    }
                }
            } : null
        };
    }

    /// <summary>
    /// Tạo OrderDto test data
    /// </summary>
    private OrderDto CreateTestOrderDto(int orderId = 1, string status = "Confirmed")
    {
        return new OrderDto
        {
            OrderId = orderId,
            Status = status,
            CustomerId = 1,
            CustomerName = "Test Customer",
            CustomerPhone = "0123456789",
            OrderType = "DineIn",
            Subtotal = 200000,
            VatAmount = 20000,
            ServiceFee = 10000,
            DiscountAmount = 0,
            TotalAmount = 230000,
            CreatedAt = DateTime.UtcNow,
            OrderItems = new List<OrderItemDto>
            {
                new OrderItemDto
                {
                    OrderDetailId = 1,
                    MenuItemId = 1,
                    MenuItemName = "Test Item",
                    Quantity = 2,
                    QuantityUsed = 2,
                    UnitPrice = 100000,
                    Status = "Done"
                }
            }
        };
    }

    /// <summary>
    /// Map Order sang OrderDto (mock AutoMapper behavior)
    /// </summary>
    private OrderDto MapOrderToDto(Order order)
    {
        return new OrderDto
        {
            OrderId = order.OrderId,
            Status = order.Status,
            CustomerId = order.CustomerId,
            ReservationId = order.ReservationId,
            OrderType = order.OrderType,
            CreatedAt = order.CreatedAt,
            OrderItems = order.OrderDetails?.Select(od => new OrderItemDto
            {
                OrderDetailId = od.OrderDetailId,
                MenuItemId = od.MenuItemId,
                MenuItemName = od.MenuItem?.Name ?? "Unknown",
                Quantity = od.Quantity,
                QuantityUsed = od.QuantityUsed ?? 0,
                UnitPrice = od.UnitPrice,
                Status = od.Status
            }).ToList() ?? new List<OrderItemDto>()
        };
    }

    #endregion

    #region Test 1: GetOrdersAsync

    [Fact]
    public async Task GetOrdersAsync_ReturnsOrders_WithDefaultParameters()
    {
        // Arrange
        var testOrders = new List<Order>
        {
            CreateTestOrder(1, "Confirmed"),
            CreateTestOrder(2, "Paid")
        };

        _mockPaymentRepository
            .Setup(repo => repo.GetAllOrdersWithDetailsAsync())
            .ReturnsAsync(testOrders);

        // Setup mapper for all orders
        _mockMapper
            .Setup(m => m.Map<OrderDto>(It.IsAny<Order>()))
            .Returns<Order>(order => MapOrderToDto(order));

        // Act
        var result = await _paymentService.GetOrdersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Orders.Should().NotBeNull();
        result.Orders.Should().HaveCount(2);
        result.TotalOrders.Should().Be(2);
    }

    [Fact]
    public async Task GetOrdersAsync_FiltersByDate_WhenDateProvided()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        var order1 = CreateTestOrder(1, "Confirmed");
        order1.CreatedAt = today.ToDateTime(TimeOnly.MinValue);
        
        var order2 = CreateTestOrder(2, "Paid");
        order2.CreatedAt = yesterday.ToDateTime(TimeOnly.MinValue);

        var testOrders = new List<Order> { order1, order2 };

        _mockPaymentRepository
            .Setup(repo => repo.GetAllOrdersWithDetailsAsync())
            .ReturnsAsync(testOrders);

        _mockMapper
            .Setup(m => m.Map<OrderDto>(It.IsAny<Order>()))
            .Returns<Order>(order => MapOrderToDto(order));

        // Act
        var result = await _paymentService.GetOrdersAsync(today);

        // Assert
        result.Should().NotBeNull();
        result.SelectedDate.Should().Be(today);
        result.Orders.Should().HaveCount(1);
        result.Orders.First().OrderId.Should().Be(1);
    }

    [Fact]
    public async Task GetOrdersAsync_FiltersByStatus_WhenStatusFilterProvided()
    {
        // Arrange
        var testOrders = new List<Order>
        {
            CreateTestOrder(1, "Confirmed"),
            CreateTestOrder(2, "Paid")
        };

        _mockPaymentRepository
            .Setup(repo => repo.GetAllOrdersWithDetailsAsync())
            .ReturnsAsync(testOrders);

        _mockMapper
            .Setup(m => m.Map<OrderDto>(It.IsAny<Order>()))
            .Returns<Order>(order => MapOrderToDto(order));

        // Act
        var result = await _paymentService.GetOrdersAsync(statusFilter: "pending");

        // Assert
        result.Should().NotBeNull();
        result.Orders.Should().HaveCount(1);
        result.Orders.First().Status.Should().Be("Confirmed");
    }

    [Fact]
    public async Task GetOrdersAsync_SortsOrders_ByCreatedAt()
    {
        // Arrange
        var order1 = CreateTestOrder(1, "Confirmed");
        order1.CreatedAt = DateTime.UtcNow.AddDays(-2);
        
        var order2 = CreateTestOrder(2, "Paid");
        order2.CreatedAt = DateTime.UtcNow.AddDays(-1);

        var testOrders = new List<Order> { order1, order2 };

        _mockPaymentRepository
            .Setup(repo => repo.GetAllOrdersWithDetailsAsync())
            .ReturnsAsync(testOrders);

        _mockMapper
            .Setup(m => m.Map<OrderDto>(It.IsAny<Order>()))
            .Returns<Order>(order => MapOrderToDto(order));

        // Act
        var result = await _paymentService.GetOrdersAsync(sortOrder: "asc");

        // Assert
        result.Should().NotBeNull();
        result.Orders.Should().HaveCount(2);
        result.Orders.First().OrderId.Should().Be(1); // Older first
    }

    #endregion

    #region Test 2: GetOrderDetailAsync

    [Fact]
    public async Task GetOrderDetailAsync_ReturnsOrderDetail_WhenOrderExists()
    {
        // Arrange
        var orderId = 1;
        var order = CreateTestOrder(orderId, "Confirmed");

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        var orderDto = CreateTestOrderDto(orderId, "Confirmed");
        _mockMapper
            .Setup(m => m.Map<OrderDto>(order))
            .Returns(orderDto);

        _mockKitchenDisplayService
            .Setup(kds => kds.GetOrderDetailsWithAllItemsAsync(orderId))
            .ReturnsAsync((KitchenOrderCardDto?)null);

        // Act
        var result = await _paymentService.GetOrderDetailAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result!.OrderId.Should().Be(orderId);
        result.Status.Should().Be("Confirmed");
        _mockPaymentRepository.Verify(repo => repo.GetOrderWithItemsAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task GetOrderDetailAsync_ReturnsNull_WhenOrderNotFound()
    {
        // Arrange
        var orderId = 999;

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _paymentService.GetOrderDetailAsync(orderId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Test 3: ApplyDiscountAsync

    [Fact]
    public async Task ApplyDiscountAsync_AppliesDiscount_WhenValidRequest()
    {
        // Arrange
        var orderId = 1;
        var order = CreateTestOrder(orderId, "Confirmed");
        order.OrderDetails = new List<OrderDetail>
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
        };

        var request = new DiscountRequestDto
        {
            OrderId = orderId,
            DiscountAmount = 10000
        };

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        var orderDto = CreateTestOrderDto(orderId);
        _mockMapper
            .Setup(m => m.Map<OrderDto>(order))
            .Returns(orderDto);

        _mockPaymentRepository
            .Setup(repo => repo.UpdateAsync(order))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _paymentService.ApplyDiscountAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.DiscountAmount.Should().BeGreaterThan(0);
        _mockPaymentRepository.Verify(repo => repo.UpdateAsync(order), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ApplyDiscountAsync_ThrowsException_WhenOrderNotFound()
    {
        // Arrange
        var orderId = 999;
        var request = new DiscountRequestDto
        {
            OrderId = orderId,
            DiscountAmount = 10000
        };

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _paymentService.ApplyDiscountAsync(request));
    }

    #endregion

    #region Test 4: ProcessPaymentAsync

    [Fact]
    public async Task ProcessPaymentAsync_ProcessesPayment_WhenOrderIsConfirmed()
    {
        // Arrange
        var orderId = 1;
        var userId = 10;
        var order = CreateTestOrder(orderId, "Confirmed");

        var request = new PaymentRequestDto
        {
            OrderId = orderId,
            Amount = 230000,
            PaymentMethod = "Cash",
            CashGiven = 250000
        };

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        var orderDto = CreateTestOrderDto(orderId, "Confirmed");
        _mockMapper
            .Setup(m => m.Map<OrderDto>(order))
            .Returns(orderDto);

        _mockPaymentRepository
            .Setup(repo => repo.UpdateAsync(order))
            .Returns(Task.CompletedTask);

        _mockOrderLockRepository
            .Setup(repo => repo.GetActiveLockAsync(orderId))
            .ReturnsAsync((OrderLock?)null);

        _mockOrderLockRepository
            .Setup(repo => repo.RemoveLockAsync(orderId))
            .Returns(Task.CompletedTask);

        var transaction = new Transaction
        {
            TransactionId = 1,
            OrderId = orderId,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            Status = "Paid",
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        _mockPaymentRepository
            .Setup(repo => repo.SaveTransactionAsync(It.IsAny<Transaction>()))
            .ReturnsAsync(transaction);

        var transactionDto = new TransactionDto
        {
            TransactionId = transaction.TransactionId,
            OrderId = orderId,
            Amount = transaction.Amount,
            PaymentMethod = transaction.PaymentMethod,
            Status = transaction.Status
        };

        _mockMapper
            .Setup(m => m.Map<TransactionDto>(It.IsAny<Transaction>()))
            .Returns(transactionDto);

        // Setup service provider for post-payment actions
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(ICustomerVipService)))
            .Returns((object?)null);

        // Setup services for post-payment actions
        var mockReceiptService = new Mock<IReceiptService>();
        mockReceiptService
            .Setup(s => s.GenerateReceiptPdfAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.com/receipt.pdf");

        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IReceiptService)))
            .Returns(mockReceiptService.Object);

        // Setup for ReleaseTablesAndCompleteReservationAsync
        _mockUnitOfWork.Setup(uow => uow.Reservations).Returns(new Mock<IReservationRepository>().Object);
        _mockUnitOfWork.Setup(uow => uow.Tables).Returns(new Mock<ITableRepository>().Object);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request, userId);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Paid");
        result.OrderId.Should().Be(orderId);
        _mockPaymentRepository.Verify(repo => repo.UpdateAsync(order), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ThrowsException_WhenOrderNotConfirmed()
    {
        // Arrange
        var orderId = 1;
        var userId = 10;
        var order = CreateTestOrder(orderId, "Pending");

        var request = new PaymentRequestDto
        {
            OrderId = orderId,
            Amount = 230000,
            PaymentMethod = "Cash"
        };

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _paymentService.ProcessPaymentAsync(request, userId));
    }

    [Fact]
    public async Task ProcessPaymentAsync_ThrowsException_WhenOrderNotFound()
    {
        // Arrange
        var orderId = 999;
        var userId = 10;

        var request = new PaymentRequestDto
        {
            OrderId = orderId,
            Amount = 230000,
            PaymentMethod = "Cash"
        };

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _paymentService.ProcessPaymentAsync(request, userId));
    }

    #endregion

    #region Test 5: ConfirmOrderAsync

    [Fact]
    public async Task ConfirmOrderAsync_ConfirmsOrder_WhenValidRequest()
    {
        // Arrange
        var orderId = 1;
        var order = CreateTestOrder(orderId, "WaitingConfirmation");
        order.OrderDetails = new List<OrderDetail>
        {
            new OrderDetail
            {
                OrderDetailId = 1,
                OrderId = orderId,
                MenuItemId = 1,
                Quantity = 2,
                QuantityUsed = null,
                UnitPrice = 100000,
                Status = "Cooking",
                CreatedAt = DateTime.UtcNow,
                MenuItem = new MenuItem
                {
                    MenuItemId = 1,
                    Name = "Test Item",
                    BillingType = ItemBillingType.KitchenPrepared
                }
            }
        };

        var request = new CustomerConfirmRequestDto
        {
            OrderId = orderId,
            Items = new List<CustomerConfirmedItemDto>
            {
                new CustomerConfirmedItemDto
                {
                    OrderDetailId = 1,
                    QuantityUsed = 2,
                    IsRemoved = false
                }
            }
        };

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        var orderDto = CreateTestOrderDto(orderId, "Confirmed");
        _mockMapper
            .Setup(m => m.Map<OrderDto>(order))
            .Returns(orderDto);

        // Setup service provider for inventory service (if needed)
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IInventoryIngredientService)))
            .Returns((object?)null);

        // Act
        var result = await _paymentService.ConfirmOrderAsync(request, 1); // userId = 1

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Confirmed");
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ConfirmOrderAsync_ThrowsException_WhenOrderNotFound()
    {
        // Arrange
        var orderId = 999;
        var request = new CustomerConfirmRequestDto
        {
            OrderId = orderId,
            Items = new List<CustomerConfirmedItemDto>()
        };

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _paymentService.ConfirmOrderAsync(request, 1)); // userId = 1
    }

    [Fact]
    public async Task ConfirmOrderAsync_ThrowsException_WhenOrderHasNoItems()
    {
        // Arrange
        var orderId = 1;
        var order = CreateTestOrder(orderId, "WaitingConfirmation");
        order.OrderDetails = new List<OrderDetail>();

        var request = new CustomerConfirmRequestDto
        {
            OrderId = orderId,
            Items = new List<CustomerConfirmedItemDto>()
        };

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _paymentService.ConfirmOrderAsync(request, 1)); // userId = 1
    }

    #endregion

    #region Test 6: LockOrderAsync / UnlockOrderAsync

    [Fact]
    public async Task LockOrderAsync_LocksOrder_WhenNotAlreadyLocked()
    {
        // Arrange
        var orderId = 1;
        var userId = 10;
        var request = new OrderLockRequestDto
        {
            OrderId = orderId,
            Reason = "Payment in progress"
        };

        _mockOrderLockRepository
            .Setup(repo => repo.GetActiveLockAsync(orderId))
            .ReturnsAsync((OrderLock?)null);

        _mockOrderLockRepository
            .Setup(repo => repo.RemoveExpiredLocksAsync())
            .Returns(Task.CompletedTask);

        _mockOrderLockRepository
            .Setup(repo => repo.AddAsync(It.IsAny<OrderLock>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _paymentService.LockOrderAsync(request, userId);

        // Assert
        result.Should().BeTrue();
        _mockOrderLockRepository.Verify(repo => repo.AddAsync(It.IsAny<OrderLock>()), Times.Once);
    }

    [Fact]
    public async Task LockOrderAsync_ThrowsException_WhenOrderAlreadyLocked()
    {
        // Arrange
        var orderId = 1;
        var userId = 10;
        var request = new OrderLockRequestDto
        {
            OrderId = orderId
        };

        var existingLock = new OrderLock
        {
            OrderLockId = 1,
            OrderId = orderId,
            LockedByUserId = 5,
            LockedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        _mockOrderLockRepository
            .Setup(repo => repo.GetActiveLockAsync(orderId))
            .ReturnsAsync(existingLock);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _paymentService.LockOrderAsync(request, userId));
    }

    [Fact]
    public async Task UnlockOrderAsync_UnlocksOrder_WhenCalled()
    {
        // Arrange
        var orderId = 1;

        _mockOrderLockRepository
            .Setup(repo => repo.RemoveLockAsync(orderId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _paymentService.UnlockOrderAsync(orderId);

        // Assert
        result.Should().BeTrue();
        _mockOrderLockRepository.Verify(repo => repo.RemoveLockAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task IsOrderLockedAsync_ReturnsTrue_WhenOrderIsLocked()
    {
        // Arrange
        var orderId = 1;

        _mockOrderLockRepository
            .Setup(repo => repo.IsOrderLockedAsync(orderId))
            .ReturnsAsync(true);

        // Act
        var result = await _paymentService.IsOrderLockedAsync(orderId);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Test 7: CancelItemAsync

    [Fact]
    public async Task CancelItemAsync_CancelsItem_WhenItemCanBeCancelled()
    {
        // Arrange
        var orderDetailId = 1;
        var reason = "Customer request";
        var orderDetail = new OrderDetail
        {
            OrderDetailId = orderDetailId,
            OrderId = 1,
            MenuItemId = 1,
            Quantity = 2,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            MenuItem = new MenuItem
            {
                MenuItemId = 1,
                Name = "Test Item",
                BillingType = ItemBillingType.KitchenPrepared
            }
        };

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderDetailByIdAsync(orderDetailId))
            .ReturnsAsync(orderDetail);

        var mockInventoryService = new Mock<IInventoryIngredientService>();
        mockInventoryService
            .Setup(s => s.ReleaseReservedBatchesForOrderDetailAsync(orderDetailId))
            .ReturnsAsync((true, "Success"));

        // Mock GetService instead of GetRequiredService (extension method cannot be mocked)
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IInventoryIngredientService)))
            .Returns(mockInventoryService.Object);

        // Act
        var result = await _paymentService.CancelItemAsync(orderDetailId, reason);

        // Assert
        result.Should().BeTrue();
        orderDetail.Status.Should().Be("Removed");
        orderDetail.Quantity.Should().Be(0);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CancelItemAsync_ThrowsException_WhenItemNotFound()
    {
        // Arrange
        var orderDetailId = 999;
        var reason = "Customer request";

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderDetailByIdAsync(orderDetailId))
            .ReturnsAsync((OrderDetail?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _paymentService.CancelItemAsync(orderDetailId, reason));
    }

    [Fact]
    public async Task ValidateCanCancelItemAsync_ReturnsFalse_WhenItemIsDone()
    {
        // Arrange
        var orderDetailId = 1;
        var orderDetail = new OrderDetail
        {
            OrderDetailId = orderDetailId,
            OrderId = 1,
            MenuItemId = 1,
            Quantity = 2,
            Status = "Done",
            CreatedAt = DateTime.UtcNow,
            MenuItem = new MenuItem
            {
                MenuItemId = 1,
                Name = "Test Item",
                BillingType = ItemBillingType.KitchenPrepared
            }
        };

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderDetailByIdAsync(orderDetailId))
            .ReturnsAsync(orderDetail);

        // Act
        var (canCancel, reason) = await _paymentService.ValidateCanCancelItemAsync(orderDetailId);

        // Assert
        canCancel.Should().BeFalse();
        reason.Should().Contain("hoàn thành");
    }

    #endregion

    #region Test 8: CancelOrderAsync

    [Fact]
    public async Task CancelOrderAsync_CancelsOrder_WhenOrderCanBeCancelled()
    {
        // Arrange
        var orderId = 1;
        var reason = "Customer left";
        var order = CreateTestOrder(orderId, "WaitingConfirmation");
        order.OrderDetails = new List<OrderDetail>
        {
            new OrderDetail
            {
                OrderDetailId = 1,
                OrderId = orderId,
                MenuItemId = 1,
                Quantity = 2,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                MenuItem = new MenuItem
                {
                    MenuItemId = 1,
                    Name = "Test Item",
                    BillingType = ItemBillingType.KitchenPrepared
                }
            }
        };

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        _mockPaymentRepository
            .Setup(repo => repo.UpdateAsync(order))
            .Returns(Task.CompletedTask);

        var mockInventoryService = new Mock<IInventoryIngredientService>();
        mockInventoryService
            .Setup(s => s.ReleaseReservedBatchesForOrderDetailAsync(It.IsAny<int>()))
            .ReturnsAsync((true, "Success"));

        // Mock GetService instead of GetRequiredService (extension method cannot be mocked)
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IInventoryIngredientService)))
            .Returns(mockInventoryService.Object);

        // Act
        var result = await _paymentService.CancelOrderAsync(orderId, reason);

        // Assert
        result.Should().BeTrue();
        order.Status.Should().Be("Cancelled");
        _mockPaymentRepository.Verify(repo => repo.UpdateAsync(order), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CancelOrderAsync_ThrowsException_WhenOrderAlreadyPaid()
    {
        // Arrange
        var orderId = 1;
        var reason = "Customer left";
        var order = CreateTestOrder(orderId, "Paid");

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _paymentService.CancelOrderAsync(orderId, reason));
    }

    [Fact]
    public async Task CancelOrderAsync_ThrowsException_WhenOrderNotFound()
    {
        // Arrange
        var orderId = 999;
        var reason = "Customer left";

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _paymentService.CancelOrderAsync(orderId, reason));
    }

    #endregion

    #region Test 9: ProcessCashPaymentAsync

    [Fact]
    public async Task ProcessCashPaymentAsync_ProcessesCashPayment_WhenValidRequest()
    {
        // Arrange
        var orderId = 1;
        var userId = 10;
        var order = CreateTestOrder(orderId, "Confirmed");

        var request = new CashPaymentRequestDto
        {
            OrderId = orderId,
            AmountReceived = 250000,
            Notes = "Payment received"
        };

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        var orderDto = CreateTestOrderDto(orderId, "Confirmed");
        _mockMapper
            .Setup(m => m.Map<OrderDto>(order))
            .Returns(orderDto);

        _mockPaymentRepository
            .Setup(repo => repo.UpdateAsync(order))
            .Returns(Task.CompletedTask);

        _mockOrderLockRepository
            .Setup(repo => repo.GetActiveLockAsync(orderId))
            .ReturnsAsync((OrderLock?)null);

        _mockOrderLockRepository
            .Setup(repo => repo.RemoveLockAsync(orderId))
            .Returns(Task.CompletedTask);

        var transaction = new Transaction
        {
            TransactionId = 1,
            OrderId = orderId,
            Amount = 230000,
            AmountReceived = request.AmountReceived,
            RefundAmount = 20000,
            PaymentMethod = "Cash",
            Status = "Paid",
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        _mockPaymentRepository
            .Setup(repo => repo.SaveTransactionAsync(It.IsAny<Transaction>()))
            .ReturnsAsync(transaction);

        var transactionDto = new TransactionDto
        {
            TransactionId = transaction.TransactionId,
            OrderId = orderId,
            Amount = transaction.Amount,
            PaymentMethod = transaction.PaymentMethod,
            Status = transaction.Status
        };

        _mockMapper
            .Setup(m => m.Map<TransactionDto>(It.IsAny<Transaction>()))
            .Returns(transactionDto);

        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(ICustomerVipService)))
            .Returns((object?)null);

        // Setup services for post-payment actions
        var mockReceiptService = new Mock<IReceiptService>();
        mockReceiptService
            .Setup(s => s.GenerateReceiptPdfAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.com/receipt.pdf");

        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IReceiptService)))
            .Returns(mockReceiptService.Object);

        // Setup for ReleaseTablesAndCompleteReservationAsync
        _mockUnitOfWork.Setup(uow => uow.Reservations).Returns(new Mock<IReservationRepository>().Object);
        _mockUnitOfWork.Setup(uow => uow.Tables).Returns(new Mock<ITableRepository>().Object);

        // Act
        var result = await _paymentService.ProcessCashPaymentAsync(request, userId);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Paid");
        result.PaymentMethod.Should().Be("Cash");
        _mockPaymentRepository.Verify(repo => repo.UpdateAsync(order), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ProcessCashPaymentAsync_ThrowsException_WhenAmountInsufficient()
    {
        // Arrange
        var orderId = 1;
        var userId = 10;
        var order = CreateTestOrder(orderId, "Confirmed");

        var request = new CashPaymentRequestDto
        {
            OrderId = orderId,
            AmountReceived = 200000 // Less than total
        };

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        var orderDto = CreateTestOrderDto(orderId);
        _mockMapper
            .Setup(m => m.Map<OrderDto>(order))
            .Returns(orderDto);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _paymentService.ProcessCashPaymentAsync(request, userId));
    }

    #endregion

    #region Test 10: GenerateVietQRAsync

    [Fact]
    public async Task GenerateVietQRAsync_GeneratesQR_WhenValidRequest()
    {
        // Arrange
        var orderId = 1;
        var bankCode = "VCB";
        var account = "1234567890";
        var order = CreateTestOrder(orderId, "Confirmed");

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync(order);

        var orderDto = CreateTestOrderDto(orderId);
        _mockMapper
            .Setup(m => m.Map<OrderDto>(order))
            .Returns(orderDto);

        // Act
        var result = await _paymentService.GenerateVietQRAsync(orderId, bankCode, account);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(orderId);
        result.QrUrl.Should().NotBeNullOrEmpty();
        result.QrUrl.Should().Contain(bankCode);
        result.QrUrl.Should().Contain(account);
    }

    [Fact]
    public async Task GenerateVietQRAsync_ThrowsException_WhenOrderNotFound()
    {
        // Arrange
        var orderId = 999;
        var bankCode = "VCB";
        var account = "1234567890";

        _mockPaymentRepository
            .Setup(repo => repo.GetOrderWithItemsAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _paymentService.GenerateVietQRAsync(orderId, bankCode, account));
    }

    #endregion
}

