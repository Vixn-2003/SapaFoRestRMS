using BusinessAccessLayer.DTOs.Kitchen;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Enums;
using DomainAccessLayer.Models;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services.Tuanlmhe
{
    /// <summary>
    /// Unit Tests cho KitchenDisplayService
    /// Test độc lập các phương thức trong KitchenDisplayService sử dụng xUnit + Moq
    /// </summary>
    public class KitchenDisplayServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IInventoryIngredientService> _mockInventoryService;
        private readonly Mock<IOrderRepository> _mockOrderRepository;
        private readonly Mock<IOrderDetailRepository> _mockOrderDetailRepository;
        private readonly Mock<IOrderComboItemRepository> _mockOrderComboItemRepository;
        private readonly Mock<IManagerMenuRepository> _mockMenuRepository;
        private readonly Mock<IInventoryIngredientRepository> _mockInventoryRepository;
        private readonly Mock<IPaymentRepository> _mockPaymentRepository;
        private readonly Mock<IManagerCategoryRepository> _mockCategoryRepository;
        private readonly KitchenDisplayService _service;

        public KitchenDisplayServiceTests()
        {
            // Khởi tạo mocks
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockInventoryService = new Mock<IInventoryIngredientService>();
            _mockOrderRepository = new Mock<IOrderRepository>();
            _mockOrderDetailRepository = new Mock<IOrderDetailRepository>();
            _mockOrderComboItemRepository = new Mock<IOrderComboItemRepository>();
            _mockMenuRepository = new Mock<IManagerMenuRepository>();
            _mockInventoryRepository = new Mock<IInventoryIngredientRepository>();
            _mockPaymentRepository = new Mock<IPaymentRepository>();
            _mockCategoryRepository = new Mock<IManagerCategoryRepository>();

            // Setup IUnitOfWork trả về mock repositories
            _mockUnitOfWork.Setup(uow => uow.Orders).Returns(_mockOrderRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.OrderDetails).Returns(_mockOrderDetailRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.OrderComboItems).Returns(_mockOrderComboItemRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.MenuItem).Returns(_mockMenuRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.InventoryIngredient).Returns(_mockInventoryRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.Payments).Returns(_mockPaymentRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.MenuCategory).Returns(_mockCategoryRepository.Object);

            // Khởi tạo KitchenDisplayService với mocked dependencies
            _service = new KitchenDisplayService(_mockUnitOfWork.Object, _mockInventoryService.Object);
        }

        #region Test Data Helpers

        /// <summary>
        /// Tạo Order test data
        /// </summary>
        private Order CreateTestOrder(int orderId, string status = "Pending", DateTime? createdAt = null, string? categoryName = null)
        {
            return new Order
            {
                OrderId = orderId,
                Status = status,
                CreatedAt = createdAt ?? DateTime.Now.AddMinutes(-30),
                OrderDetails = new List<OrderDetail>
                {
                    new OrderDetail
                    {
                        OrderDetailId = orderId * 10,
                        OrderId = orderId,
                        MenuItemId = 1,
                        Quantity = 2,
                        Status = "Pending",
                        CreatedAt = createdAt ?? DateTime.Now.AddMinutes(-30),
                        MenuItem = new MenuItem
                        {
                            MenuItemId = 1,
                            Name = "Phở Bò",
                            CourseType = "Món chính",
                            TimeCook = 15,
                            BatchSize = 5,
                            BillingType = ItemBillingType.KitchenPrepared,
                            Category = categoryName != null ? new MenuCategory
                            {
                                CategoryId = 1,
                                CategoryName = categoryName
                            } : null
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Tạo OrderDetail test data
        /// </summary>
        private OrderDetail CreateTestOrderDetail(int orderDetailId, int orderId, string status = "Pending", bool isUrgent = false, string? categoryName = null)
        {
            return new OrderDetail
            {
                OrderDetailId = orderDetailId,
                OrderId = orderId,
                MenuItemId = 1,
                Quantity = 2,
                Status = status,
                IsUrgent = isUrgent,
                CreatedAt = DateTime.Now.AddMinutes(-30),
                StartedAt = status == "Cooking" || status == "Late" ? DateTime.Now.AddMinutes(-20) : null,
                ReadyAt = status == "Ready" || status == "Done" ? DateTime.Now.AddMinutes(-5) : null,
                MenuItem = new MenuItem
                {
                    MenuItemId = 1,
                    Name = "Phở Bò",
                    CourseType = "Món chính",
                    TimeCook = 15,
                    BatchSize = 5,
                    BillingType = ItemBillingType.KitchenPrepared,
                    Category = categoryName != null ? new MenuCategory
                    {
                        CategoryId = 1,
                        CategoryName = categoryName
                    } : null
                }
            };
        }

        #endregion

        #region UpdateItemStatusAsync - Test Cases TC07-TC16

        /// <summary>
        /// TC07 – Pending → Ready (Invalid transition)
        /// Precondition: Order.Status = Pending, OrderComboItems = valid
        /// Action: NewStatus = Ready
        /// Expected: Return = FALSE, Log = "Invalid transition"
        /// Giải thích: Luồng nghiệp vụ không cho phép chuyển từ Pending → Ready trực tiếp.
        /// Muốn Ready thì phải Cooking trước. TC07 đảm bảo hàm chặn sai quy trình.
        /// </summary>
        [Fact]
        public async Task UpdateItemStatusAsync_TC07_PendingToReady_InvalidTransition_ReturnsFalse()
        {
            // Arrange
            var orderDetail = CreateTestOrderDetail(1, 1, "Pending");
            orderDetail.OrderComboItems = new List<OrderComboItem>
            {
                new OrderComboItem
                {
                    OrderComboItemId = 1,
                    OrderDetailId = 1,
                    MenuItemId = 2,
                    Quantity = 1,
                    Status = "Pending"
                }
            };
            var order = new Order { OrderId = 1, Status = "Pending" };
            var request = new UpdateItemStatusRequest
            {
                OrderDetailId = 1,
                NewStatus = "Ready",
                UserId = 1
            };

            _mockOrderDetailRepository
                .Setup(repo => repo.GetByIdWithMenuItemAsync(1))
                .ReturnsAsync(orderDetail);

            _mockOrderRepository
                .Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync(order);

            _mockPaymentRepository
                .Setup(repo => repo.GetOrderDetailByIdAsync(1))
                .ReturnsAsync((OrderDetail?)null);

            // Act
            var result = await _service.UpdateItemStatusAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse(); // Return FALSE
            var messageLower = result.Message?.ToLowerInvariant() ?? string.Empty;
            messageLower.Should().Contain("chuyển"); // Invalid transition message
        }

        /// <summary>
        /// TC08 – Pending + nguyên liệu thiếu → chuyển Cooking thất bại
        /// Precondition: Status = Pending, Ingredients: Insufficient, OrderComboItems = valid
        /// Action: NewStatus = Cooking
        /// Expected: Return = FALSE, Log = "Ingredient not enough"
        /// Giải thích: Order không được vào Cooking nếu kho báo thiếu nguyên liệu. TC này test nghiệp vụ Inventory.
        /// </summary>
        [Fact]
        public async Task UpdateItemStatusAsync_TC08_PendingToCooking_InsufficientIngredients_ReturnsFalse()
        {
            // Arrange
            var orderDetail = CreateTestOrderDetail(1, 1, "Pending");
            orderDetail.Quantity = 5; // Cần 5 món
            orderDetail.OrderComboItems = new List<OrderComboItem>
            {
                new OrderComboItem
                {
                    OrderComboItemId = 1,
                    OrderDetailId = 1,
                    MenuItemId = 2,
                    Quantity = 1,
                    Status = "Pending"
                }
            };

            var order = new Order { OrderId = 1, Status = "Pending" };
            var request = new UpdateItemStatusRequest
            {
                OrderDetailId = 1,
                NewStatus = "Cooking",
                UserId = 1
            };

            // Mock recipes với insufficient ingredients
            var recipe = new Recipe
            {
                RecipeId = 1,
                MenuItemId = 1,
                IngredientId = 1,
                QuantityNeeded = 2, // Cần 2 đơn vị nguyên liệu cho 1 món
                Ingredient = new Ingredient
                {
                    IngredientId = 1,
                    Name = "Thịt bò",
                    Unit = new Unit { UnitId = 1, UnitName = "kg" }
                }
            };

            // Mock batches với insufficient quantity (chỉ có 5, cần 10)
            var batches = new List<InventoryBatch>
            {
                new InventoryBatch
                {
                    BatchId = 1,
                    IngredientId = 1,
                    QuantityRemaining = 5, // Chỉ có 5
                    QuantityReserved = 0
                }
            };

            _mockOrderDetailRepository
                .Setup(repo => repo.GetByIdWithMenuItemAsync(1))
                .ReturnsAsync(orderDetail);

            _mockOrderRepository
                .Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync(order);

            _mockMenuRepository
                .Setup(repo => repo.GetRecipeByMenuItem(1))
                .ReturnsAsync(new List<Recipe> { recipe });

            _mockInventoryRepository
                .Setup(repo => repo.GetAllBatchesByIngredientAsync(1))
                .ReturnsAsync(batches);

            _mockPaymentRepository
                .Setup(repo => repo.GetOrderDetailByIdAsync(1))
                .ReturnsAsync((OrderDetail?)null);

            // Act
            var result = await _service.UpdateItemStatusAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse(); // Return FALSE
            var messageLower = result.Message?.ToLowerInvariant() ?? string.Empty;
            messageLower.Should().ContainAny("không đủ", "thiếu", "nguyên liệu"); // Ingredient not enough message
        }

        /// <summary>
        /// TC09 – Pending + nguyên liệu đủ → chuyển Cooking thành công
        /// Precondition: Status = Pending, Ingredients: Sufficient, OrderComboItems = valid
        /// Action: NewStatus = Cooking
        /// Expected: Return = TRUE, Log = "Status updated"
        /// Giải thích: Đây là luồng hợp lệ: Pending → Cooking. Kiểm tra nghiệp vụ đầy đủ nguyên liệu.
        /// </summary>
        [Fact]
        public async Task UpdateItemStatusAsync_TC09_PendingToCooking_SufficientIngredients_ReturnsTrue()
        {
            // Arrange
            var orderDetail = CreateTestOrderDetail(1, 1, "Pending");
            orderDetail.OrderComboItems = new List<OrderComboItem>
            {
                new OrderComboItem
                {
                    OrderComboItemId = 1,
                    OrderDetailId = 1,
                    MenuItemId = 2,
                    Quantity = 1,
                    Status = "Pending"
                }
            };
            var order = new Order { OrderId = 1, Status = "Pending" };
            var request = new UpdateItemStatusRequest
            {
                OrderDetailId = 1,
                NewStatus = "Cooking",
                UserId = 1
            };

            _mockOrderDetailRepository
                .Setup(repo => repo.GetByIdWithMenuItemAsync(1))
                .ReturnsAsync(orderDetail);

            _mockOrderRepository
                .Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync(order);

            _mockOrderRepository
                .Setup(repo => repo.GetByIdWithOrderDetailsAsync(1))
                .ReturnsAsync(new Order
                {
                    OrderId = 1,
                    Status = "Pending",
                    OrderDetails = new List<OrderDetail> { orderDetail }
                });

            _mockOrderRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);

            _mockOrderComboItemRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<OrderComboItem>()))
                .Returns(Task.CompletedTask);

            // Mock sufficient ingredients
            _mockMenuRepository
                .Setup(repo => repo.GetRecipeByMenuItem(1))
                .ReturnsAsync(new List<Recipe>()); // No recipes = sufficient

            _mockUnitOfWork
                .Setup(uow => uow.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.UpdateItemStatusAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue(); // Return TRUE
            orderDetail.Status.Should().Be("Cooking");
            result.Message.Should().Contain("updated"); // Status updated
        }

        /// <summary>
        /// TC10 – ComboId có nhưng OrderComboItems = null → tự tạo mới
        /// Precondition: ComboId exists, OrderComboItems = null
        /// Action: NewStatus = Cooking
        /// Expected: Return = TRUE, Log = "Status updated", Tự động tạo OrderComboItems
        /// Giải thích: TC này xác minh hệ thống tự tạo danh sách món trong combo nếu chưa có (lazy create).
        /// </summary>
        [Fact]
        public async Task UpdateItemStatusAsync_TC10_ComboIdExists_OrderComboItemsNull_AutoCreate_ReturnsTrue()
        {
            // Arrange
            var orderDetail = CreateTestOrderDetail(1, 1, "Pending");
            orderDetail.ComboId = 1; // Combo exists
            orderDetail.OrderComboItems = null; // OrderComboItems = null

            // Mock Combo với ComboItems để tự động tạo OrderComboItems
            var comboItem = new ComboItem
            {
                ComboItemId = 1,
                ComboId = 1,
                MenuItemId = 2,
                Quantity = 1,
                MenuItem = new MenuItem
                {
                    MenuItemId = 2,
                    Name = "Món trong combo",
                    BillingType = ItemBillingType.KitchenPrepared
                }
            };

            var orderDetailWithCombo = new OrderDetail
            {
                OrderDetailId = 1,
                ComboId = 1,
                Combo = new Combo
                {
                    ComboId = 1,
                    Name = "Combo Test",
                    ComboItems = new List<ComboItem> { comboItem }
                }
            };

            var order = new Order { OrderId = 1, Status = "Pending" };
            var request = new UpdateItemStatusRequest
            {
                OrderDetailId = 1,
                NewStatus = "Cooking",
                UserId = 1
            };

            _mockOrderDetailRepository
                .Setup(repo => repo.GetByIdWithMenuItemAsync(1))
                .ReturnsAsync(orderDetail);

            _mockOrderRepository
                .Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync(order);

            // Mock GetOrderDetailByIdAsync để trả về OrderDetail với Combo và ComboItems
            _mockPaymentRepository
                .Setup(repo => repo.GetOrderDetailByIdAsync(1))
                .ReturnsAsync(orderDetailWithCombo);

            // Mock để tự động tạo OrderComboItems
            _mockOrderComboItemRepository
                .Setup(repo => repo.AddAsync(It.IsAny<OrderComboItem>()))
                .Returns(Task.CompletedTask);

            _mockOrderComboItemRepository
                .Setup(repo => repo.GetByOrderDetailIdAsync(1))
                .ReturnsAsync(new List<OrderComboItem>()); // Ban đầu chưa có

            _mockOrderRepository
                .Setup(repo => repo.GetByIdWithOrderDetailsAsync(1))
                .ReturnsAsync(new Order
                {
                    OrderId = 1,
                    Status = "Pending",
                    OrderDetails = new List<OrderDetail> { orderDetail }
                });

            _mockOrderRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);

            _mockMenuRepository
                .Setup(repo => repo.GetRecipeByMenuItem(1))
                .ReturnsAsync(new List<Recipe>()); // Sufficient ingredients

            _mockUnitOfWork
                .Setup(uow => uow.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.UpdateItemStatusAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue(); // Return TRUE
            result.Message.Should().Contain("updated"); // Status updated
            // Verify OrderComboItems được tự động tạo
            _mockOrderComboItemRepository.Verify(repo => repo.AddAsync(It.IsAny<OrderComboItem>()), Times.AtLeastOnce);
        }

        /// <summary>
        /// TC11 – Cooking → Pending (Invalid transition)
        /// Precondition: Status = Cooking
        /// Action: NewStatus = Pending
        /// Expected: Return = FALSE, Log = "Invalid transition"
        /// Giải thích: Nghiệp vụ không cho phép Cooking quay lại Pending. TC11 đảm bảo state machine hoạt động đúng.
        /// </summary>
        [Fact]
        public async Task UpdateItemStatusAsync_TC11_CookingToPending_InvalidTransition_ReturnsFalse()
        {
            // Arrange
            var orderDetail = CreateTestOrderDetail(1, 1, "Cooking");
            var order = new Order { OrderId = 1, Status = "Cooking" };
            var request = new UpdateItemStatusRequest
            {
                OrderDetailId = 1,
                NewStatus = "Pending", // Invalid transition
                UserId = 1
            };

            _mockOrderDetailRepository
                .Setup(repo => repo.GetByIdWithMenuItemAsync(1))
                .ReturnsAsync(orderDetail);

            _mockOrderRepository
                .Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync(order);

            // Act
            var result = await _service.UpdateItemStatusAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse(); // Return FALSE
            var messageLower = result.Message?.ToLowerInvariant() ?? string.Empty;
            messageLower.Should().ContainAny("không thể", "chuyển"); // Invalid transition message
        }

        /// <summary>
        /// TC13 – Cooking → Ready (Valid)
        /// Precondition: Status = Cooking, OrderComboItems = valid
        /// Action: NewStatus = Ready
        /// Expected: Return = TRUE, Log = "Status updated"
        /// Giải thích: Luồng hợp lệ: món nấu xong thì được chuyển sang Ready.
        /// </summary>
        [Fact]
        public async Task UpdateItemStatusAsync_TC13_CookingToReady_Valid_ReturnsTrue()
        {
            // Arrange
            var orderDetail = CreateTestOrderDetail(1, 1, "Cooking");
            orderDetail.OrderComboItems = new List<OrderComboItem>
            {
                new OrderComboItem
                {
                    OrderComboItemId = 1,
                    OrderDetailId = 1,
                    MenuItemId = 2,
                    Quantity = 1,
                    Status = "Cooking"
                }
            };

            var order = new Order { OrderId = 1, Status = "Cooking" };
            var request = new UpdateItemStatusRequest
            {
                OrderDetailId = 1,
                NewStatus = "Ready",
                UserId = 1
            };

            _mockOrderDetailRepository
                .Setup(repo => repo.GetByIdWithMenuItemAsync(1))
                .ReturnsAsync(orderDetail);

            _mockOrderRepository
                .Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync(order);

            _mockOrderRepository
                .Setup(repo => repo.GetByIdWithOrderDetailsAsync(1))
                .ReturnsAsync(new Order
                {
                    OrderId = 1,
                    Status = "Cooking",
                    OrderDetails = new List<OrderDetail> { orderDetail }
                });

            _mockOrderRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);

            _mockInventoryService
                .Setup(service => service.ConsumeReservedBatchesForOrderDetailAsync(1))
                .ReturnsAsync((true, "Success"));

            _mockUnitOfWork
                .Setup(uow => uow.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.UpdateItemStatusAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue(); // Return TRUE
            result.Message.Should().Contain("updated"); // Status updated
            orderDetail.Status.Should().Be("Ready");
            orderDetail.ReadyAt.Should().NotBeNull();
        }

        /// <summary>
        /// TC14 – Ready → Cooking (Invalid transition)
        /// Precondition: Status = Ready
        /// Action: NewStatus = Cooking
        /// Expected: Return = FALSE, Log = "Invalid transition"
        /// Giải thích: Món đã Ready (chế biến xong) không thể quay ngược lại Cooking. TC14 đảm bảo không thể lùi trạng thái.
        /// </summary>
        [Fact]
        public async Task UpdateItemStatusAsync_TC14_ReadyToCooking_InvalidTransition_ReturnsFalse()
        {
            // Arrange
            var orderDetail = CreateTestOrderDetail(1, 1, "Ready");
            var order = new Order { OrderId = 1, Status = "Ready" };
            var request = new UpdateItemStatusRequest
            {
                OrderDetailId = 1,
                NewStatus = "Cooking", // Invalid transition
                UserId = 1
            };

            _mockOrderDetailRepository
                .Setup(repo => repo.GetByIdWithMenuItemAsync(1))
                .ReturnsAsync(orderDetail);

            _mockOrderRepository
                .Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync(order);

            // Act
            var result = await _service.UpdateItemStatusAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse(); // Return FALSE
            var messageLower = result.Message?.ToLowerInvariant() ?? string.Empty;
            messageLower.Should().ContainAny("không thể", "chuyển", "sẵn sàng"); // Invalid transition message
        }

        /// <summary>
        /// TC15 – Ready → Done (Valid)
        /// Precondition: Status = Ready
        /// Action: NewStatus = Done
        /// Expected: Return = TRUE, Log = "Status updated"
        /// Giải thích: Luồng đúng: món đã Ready → Done khi phục vụ xong.
        /// </summary>
        [Fact]
        public async Task UpdateItemStatusAsync_TC15_ReadyToDone_Valid_ReturnsTrue()
        {
            // Arrange
            var orderDetail = CreateTestOrderDetail(1, 1, "Ready");
            var order = new Order { OrderId = 1, Status = "Ready" };
            var request = new UpdateItemStatusRequest
            {
                OrderDetailId = 1,
                NewStatus = "Done",
                UserId = 1
            };

            _mockOrderDetailRepository
                .Setup(repo => repo.GetByIdWithMenuItemAsync(1))
                .ReturnsAsync(orderDetail);

            _mockOrderRepository
                .Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync(order);

            _mockOrderRepository
                .Setup(repo => repo.GetByIdWithOrderDetailsAsync(1))
                .ReturnsAsync(new Order
                {
                    OrderId = 1,
                    Status = "Ready",
                    OrderDetails = new List<OrderDetail> { orderDetail }
                });

            _mockOrderRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(uow => uow.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.UpdateItemStatusAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue(); // Return TRUE
            result.Message.Should().Contain("updated"); // Status updated
            orderDetail.Status.Should().Be("Done");
        }

        /// <summary>
        /// TC16 – Done → Pending (Invalid transition)
        /// Precondition: Status = Done
        /// Action: NewStatus = Pending
        /// Expected: Return = FALSE, Log = "Invalid transition"
        /// Giải thích: Sau khi Done thì đơn hàng hoàn tất → không thể quay lại Pending.
        /// </summary>
        [Fact]
        public async Task UpdateItemStatusAsync_TC16_DoneToPending_InvalidTransition_ReturnsFalse()
        {
            // Arrange
            var orderDetail = CreateTestOrderDetail(1, 1, "Done");
            var order = new Order { OrderId = 1, Status = "Done" };
            var request = new UpdateItemStatusRequest
            {
                OrderDetailId = 1,
                NewStatus = "Pending", // Invalid transition
                UserId = 1
            };

            _mockOrderDetailRepository
                .Setup(repo => repo.GetByIdWithMenuItemAsync(1))
                .ReturnsAsync(orderDetail);

            _mockOrderRepository
                .Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync(order);

            // Act
            var result = await _service.UpdateItemStatusAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse(); // Return FALSE
            var messageLower = result.Message?.ToLowerInvariant() ?? string.Empty;
            messageLower.Should().ContainAny("không thể", "chuyển", "hoàn thành"); // Invalid transition message
        }

        #endregion

        #region StartCookingWithQuantityAsync Tests - UTCID01-UTCID05

        /// <summary>
        /// UTCID01: Status = Pending, Ingredients = Sufficient, Quantity = == totalQuantity
        /// Expected: Return = TRUE, Log = "Status updated"
        /// </summary>
        [Fact]
        public async Task StartCookingWithQuantityAsync_UTCID01_FullQuantity_SufficientIngredients_ReturnsTrue()
        {
            // Arrange
            var orderDetail = CreateTestOrderDetail(1, 1, "Pending");
            orderDetail.Quantity = 5;

            var request = new StartCookingWithQuantityRequest
            {
                OrderDetailId = 1,
                Quantity = 5,
                UserId = 1
            };

            _mockOrderDetailRepository
                .Setup(repo => repo.GetByIdWithMenuItemAsync(1))
                .ReturnsAsync(orderDetail);

            // Mock empty recipes để không check shortage
            _mockMenuRepository
                .Setup(repo => repo.GetRecipeByMenuItem(1))
                .ReturnsAsync(new List<Recipe>());

            // Mock GetByIdWithOrderDetailsAsync cho UpdateKitchenOrderStatusAsync
            _mockOrderRepository
                .Setup(repo => repo.GetByIdWithOrderDetailsAsync(1))
                .ReturnsAsync(new Order
                {
                    OrderId = 1,
                    Status = "Pending",
                    OrderDetails = new List<OrderDetail> { orderDetail }
                });

            _mockOrderRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);

            _mockOrderDetailRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<OrderDetail>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(uow => uow.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.StartCookingWithQuantityAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue(); // Return TRUE
            result.Message.Should().ContainAny("bắt đầu nấu", "updated"); // Log "Status updated"
            orderDetail.Status.Should().Be("Cooking");
            orderDetail.StartedAt.Should().NotBeNull();
        }

        /// <summary>
        /// UTCID02: Status = Pending, Ingredients = Sufficient, Quantity = <= 0
        /// Expected: Return = FALSE, Log = "Số lượng nấu... không hợp"
        /// </summary>
        [Fact]
        public async Task StartCookingWithQuantityAsync_UTCID02_QuantityLessThanOrEqualZero_ReturnsFalse()
        {
            // Arrange
            var orderDetail = CreateTestOrderDetail(1, 1, "Pending");
            orderDetail.Quantity = 5;

            var request = new StartCookingWithQuantityRequest
            {
                OrderDetailId = 1,
                Quantity = 0, // <= 0
                UserId = 1
            };

            _mockOrderDetailRepository
                .Setup(repo => repo.GetByIdWithMenuItemAsync(1))
                .ReturnsAsync(orderDetail);

            // Mock sufficient ingredients
            _mockMenuRepository
                .Setup(repo => repo.GetRecipeByMenuItem(1))
                .ReturnsAsync(new List<Recipe>());

            // Act
            var result = await _service.StartCookingWithQuantityAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse(); // Return FALSE
            result.Message.Should().ContainAny("Số lượng nấu", "không hợp", "phải lớn hơn 0"); // Log message
        }

        /// <summary>
        /// UTCID03: Status = Pending, Ingredients = Sufficient, Quantity = > totalQuantity
        /// Expected: Return = FALSE, Log = "Số lượng nấu... không hợp"
        /// </summary>
        [Fact]
        public async Task StartCookingWithQuantityAsync_UTCID03_QuantityGreaterThanTotal_ReturnsFalse()
        {
            // Arrange
            var orderDetail = CreateTestOrderDetail(1, 1, "Pending");
            orderDetail.Quantity = 5; // totalQuantity = 5

            var request = new StartCookingWithQuantityRequest
            {
                OrderDetailId = 1,
                Quantity = 6, // > totalQuantity
                UserId = 1
            };

            _mockOrderDetailRepository
                .Setup(repo => repo.GetByIdWithMenuItemAsync(1))
                .ReturnsAsync(orderDetail);

            // Mock sufficient ingredients
            _mockMenuRepository
                .Setup(repo => repo.GetRecipeByMenuItem(1))
                .ReturnsAsync(new List<Recipe>());

            // Act
            var result = await _service.StartCookingWithQuantityAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse(); // Return FALSE
            result.Message.Should().ContainAny("Số lượng nấu", "không hợp", "vượt quá"); // Log message
        }

        /// <summary>
        /// UTCID04: Status = Pending, Ingredients = Insufficient, Quantity = == totalQuantity
        /// Expected: Return = FALSE, Log = "thiếu nguyên liệu"
        /// </summary>
        [Fact]
        public async Task StartCookingWithQuantityAsync_UTCID04_FullQuantity_InsufficientIngredients_ReturnsFalse()
        {
            // Arrange
            var orderDetail = CreateTestOrderDetail(1, 1, "Pending");
            orderDetail.Quantity = 5; // totalQuantity = 5

            var request = new StartCookingWithQuantityRequest
            {
                OrderDetailId = 1,
                Quantity = 5, // == totalQuantity
                UserId = 1
            };

            // Mock recipes với insufficient ingredients
            var recipe = new Recipe
            {
                RecipeId = 1,
                MenuItemId = 1,
                IngredientId = 1,
                QuantityNeeded = 2, // Cần 2 đơn vị nguyên liệu cho 1 món
                Ingredient = new Ingredient
                {
                    IngredientId = 1,
                    Name = "Thịt bò",
                    Unit = new Unit { UnitId = 1, UnitName = "kg" }
                }
            };

            // Mock batches với insufficient quantity (chỉ có 5, cần 10 cho 5 món)
            var batches = new List<InventoryBatch>
            {
                new InventoryBatch
                {
                    BatchId = 1,
                    IngredientId = 1,
                    QuantityRemaining = 5, // Chỉ có 5, cần 10
                    QuantityReserved = 0
                }
            };

            _mockOrderDetailRepository
                .Setup(repo => repo.GetByIdWithMenuItemAsync(1))
                .ReturnsAsync(orderDetail);

            _mockMenuRepository
                .Setup(repo => repo.GetRecipeByMenuItem(1))
                .ReturnsAsync(new List<Recipe> { recipe });

            _mockInventoryRepository
                .Setup(repo => repo.GetAllBatchesByIngredientAsync(1))
                .ReturnsAsync(batches);

            // Act
            var result = await _service.StartCookingWithQuantityAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse(); // Return FALSE
            var messageLower = result.Message?.ToLowerInvariant() ?? string.Empty;
            messageLower.Should().ContainAny("không đủ", "thiếu", "nguyên liệu"); // Log message "thiếu nguyên liệu"
        }

        /// <summary>
        /// UTCID05: Status = Pending, Ingredients = Sufficient, Quantity = < totalQuantity, >0
        /// Expected: Return = TRUE, Log = "Status updated"
        /// </summary>
        [Fact]
        public async Task StartCookingWithQuantityAsync_UTCID05_PartialQuantity_SufficientIngredients_ReturnsTrue()
        {
            // Arrange
            var orderDetail = CreateTestOrderDetail(1, 1, "Pending");
            orderDetail.Quantity = 10; // totalQuantity = 10

            var request = new StartCookingWithQuantityRequest
            {
                OrderDetailId = 1,
                Quantity = 3, // < totalQuantity, >0
                UserId = 1
            };

            _mockOrderDetailRepository
                .Setup(repo => repo.GetByIdWithMenuItemAsync(1))
                .ReturnsAsync(orderDetail);

            // Mock empty recipes để không check shortage
            _mockMenuRepository
                .Setup(repo => repo.GetRecipeByMenuItem(1))
                .ReturnsAsync(new List<Recipe>());

            _mockOrderDetailRepository
                .Setup(repo => repo.AddAsync(It.IsAny<OrderDetail>()))
                .Returns(Task.CompletedTask);

            _mockOrderDetailRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<OrderDetail>()))
                .Returns(Task.CompletedTask);

            // Mock GetByIdWithOrderDetailsAsync cho UpdateKitchenOrderStatusAsync
            _mockOrderRepository
                .Setup(repo => repo.GetByIdWithOrderDetailsAsync(1))
                .ReturnsAsync(new Order
                {
                    OrderId = 1,
                    Status = "Pending",
                    OrderDetails = new List<OrderDetail> { orderDetail }
                });

            _mockOrderRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);

            _mockInventoryService
                .Setup(service => service.ReserveBatchesForOrderDetailAsync(It.IsAny<int>()))
                .ReturnsAsync((true, "Success"));

            _mockUnitOfWork
                .Setup(uow => uow.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.StartCookingWithQuantityAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue(); // Return TRUE
            result.Message.Should().ContainAny("bắt đầu nấu", "updated"); // Log "Status updated"
            // Verify that a new OrderDetail was created for the split
            _mockOrderDetailRepository.Verify(repo => repo.AddAsync(It.IsAny<OrderDetail>()), Times.Once);
        }

        #endregion
    }
}

