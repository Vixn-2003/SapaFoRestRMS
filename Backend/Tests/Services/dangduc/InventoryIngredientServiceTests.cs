using AutoMapper;
using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.Services;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services.dangduc
{
    public class InventoryIngredientServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly InventoryIngredientService _service;

        public InventoryIngredientServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _service = new InventoryIngredientService(_mockUnitOfWork.Object, _mockMapper.Object);
        }

        #region UpdateBatchWarehouse Tests

        [Fact]
        public async Task UpdateBatchWarehouse_WhenSuccessful_ReturnsTrue()
        {
            // Arrange
            int idBatch = 1;
            int idWarehouse = 2;
            bool isActive = true;

            _mockUnitOfWork
                .Setup(x => x.InventoryIngredient.UpdateBatchWarehouse(idBatch, idWarehouse, isActive))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateBatchWarehouse(idBatch, idWarehouse, isActive);

            // Assert
            Assert.True(result);
            _mockUnitOfWork.Verify(
                x => x.InventoryIngredient.UpdateBatchWarehouse(idBatch, idWarehouse, isActive),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateBatchWarehouse_WhenFailed_ReturnsFalse()
        {
            // Arrange
            int idBatch = 1;
            int idWarehouse = 2;
            bool isActive = false;

            _mockUnitOfWork
                .Setup(x => x.InventoryIngredient.UpdateBatchWarehouse(idBatch, idWarehouse, isActive))
                .ReturnsAsync(false);

            // Act
            var result = await _service.UpdateBatchWarehouse(idBatch, idWarehouse, isActive);

            // Assert
            Assert.False(result);
            _mockUnitOfWork.Verify(
                x => x.InventoryIngredient.UpdateBatchWarehouse(idBatch, idWarehouse, isActive),
                Times.Once
            );
        }

        [Theory]
        [InlineData(1, 1, true)]
        [InlineData(5, 3, false)]
        [InlineData(10, 2, true)]
        public async Task UpdateBatchWarehouse_WithVariousInputs_CallsRepositoryCorrectly(
            int idBatch, int idWarehouse, bool isActive)
        {
            // Arrange
            _mockUnitOfWork
                .Setup(x => x.InventoryIngredient.UpdateBatchWarehouse(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateBatchWarehouse(idBatch, idWarehouse, isActive);

            // Assert
            Assert.True(result);
            _mockUnitOfWork.Verify(
                x => x.InventoryIngredient.UpdateBatchWarehouse(idBatch, idWarehouse, isActive),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateBatchWarehouse_WhenRepositoryThrowsException_PropagatesException()
        {
            // Arrange
            int idBatch = 1;
            int idWarehouse = 2;
            bool isActive = true;

            _mockUnitOfWork
                .Setup(x => x.InventoryIngredient.UpdateBatchWarehouse(idBatch, idWarehouse, isActive))
                .ThrowsAsync(new System.Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<System.Exception>(
                () => _service.UpdateBatchWarehouse(idBatch, idWarehouse, isActive)
            );
        }

        #endregion

        #region AddNewIngredient Tests

        [Fact]
        public async Task AddNewIngredient_WhenSuccessful_ReturnsIngredientId()
        {
            // Arrange
            var ingredientDTO = new IngredientDTO
            {
                Name = "Tomato",
                UnitId = 1
            };

            var ingredient = new Ingredient
            {
                Name = "Tomato",
                UnitId = 1
            };

            _mockMapper
                .Setup(x => x.Map<Ingredient>(ingredientDTO))
                .Returns(ingredient);

            _mockUnitOfWork
                .Setup(x => x.InventoryIngredient.AddNewIngredient(ingredient))
                .ReturnsAsync(10);

            // Act
            var result = await _service.AddNewIngredient(ingredientDTO);

            // Assert
            Assert.Equal(10, result);
            _mockMapper.Verify(x => x.Map<Ingredient>(ingredientDTO), Times.Once);
            _mockUnitOfWork.Verify(x => x.InventoryIngredient.AddNewIngredient(ingredient), Times.Once);
        }

        [Fact]
        public async Task AddNewIngredient_WithNullDTO_ThrowsException()
        {
            // Arrange
            IngredientDTO ingredientDTO = null;

            // Act & Assert
            await Assert.ThrowsAsync<System.NullReferenceException>(
                () => _service.AddNewIngredient(ingredientDTO)
            );
        }

        [Fact]
        public async Task AddNewIngredient_WhenRepositoryFails_ReturnsZero()
        {
            // Arrange
            var ingredientDTO = new IngredientDTO
            {
                Name = "Onion",
                UnitId = 2
            };

            var ingredient = new Ingredient
            {
                Name = "Onion",
                UnitId = 2
            };

            _mockMapper
                .Setup(x => x.Map<Ingredient>(ingredientDTO))
                .Returns(ingredient);

            _mockUnitOfWork
                .Setup(x => x.InventoryIngredient.AddNewIngredient(ingredient))
                .ReturnsAsync(0);

            // Act
            var result = await _service.AddNewIngredient(ingredientDTO);

            // Assert
            Assert.Equal(0, result);
        }

        [Theory]
        [InlineData("Carrot", 1)]
        [InlineData("Potato", 2)]
        [InlineData("Chicken", 3)]
        public async Task AddNewIngredient_WithDifferentIngredients_CallsRepositoryWithCorrectData(
            string name, int unitId)
        {
            // Arrange
            var ingredientDTO = new IngredientDTO { Name = name, UnitId = unitId };
            var ingredient = new Ingredient { Name = name, UnitId = unitId };

            _mockMapper.Setup(x => x.Map<Ingredient>(ingredientDTO)).Returns(ingredient);
            _mockUnitOfWork.Setup(x => x.InventoryIngredient.AddNewIngredient(It.IsAny<Ingredient>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.AddNewIngredient(ingredientDTO);

            // Assert
            _mockUnitOfWork.Verify(x => x.InventoryIngredient.AddNewIngredient(
                It.Is<Ingredient>(i => i.Name == name && i.UnitId == unitId)), Times.Once);
        }

        #endregion

        #region AddNewBatch Tests

        [Fact]
        public async Task AddNewBatch_WhenSuccessful_ReturnsBatchId()
        {
            // Arrange
            var batchDTO = new InventoryBatchDTO
            {
                IngredientId = 1,
                QuantityRemaining = 100,
                ExpiryDate = DateOnly.FromDateTime(System.DateTime.Now.AddMonths(6)),
                WarehouseId = 1
            };

            var batch = new InventoryBatch
            {
                IngredientId = 1,
                QuantityRemaining = 100,
                ExpiryDate = DateOnly.FromDateTime(System.DateTime.Now.AddMonths(6)),
                WarehouseId = 1
            };

            _mockMapper
                .Setup(x => x.Map<InventoryBatch>(batchDTO))
                .Returns(batch);

            _mockUnitOfWork
                .Setup(x => x.InventoryIngredient.AddNewBatch(batch))
                .ReturnsAsync(20);

            // Act
            var result = await _service.AddNewBatch(batchDTO);

            // Assert
            Assert.Equal(20, result);
            _mockMapper.Verify(x => x.Map<InventoryBatch>(batchDTO), Times.Once);
            _mockUnitOfWork.Verify(x => x.InventoryIngredient.AddNewBatch(batch), Times.Once);
        }

        [Fact]
        public async Task AddNewBatch_WithNullDTO_ThrowsException()
        {
            // Arrange
            InventoryBatchDTO batchDTO = null;

            // Act & Assert
            await Assert.ThrowsAsync<System.NullReferenceException>(
                () => _service.AddNewBatch(batchDTO)
            );
        }

        [Fact]
        public async Task AddNewBatch_WhenRepositoryFails_ReturnsZero()
        {
            // Arrange
            var batchDTO = new InventoryBatchDTO
            {
                IngredientId = 1,
                QuantityRemaining = 50
            };

            var batch = new InventoryBatch
            {
                IngredientId = 1,
                QuantityRemaining = 50
            };

            _mockMapper.Setup(x => x.Map<InventoryBatch>(batchDTO)).Returns(batch);
            _mockUnitOfWork.Setup(x => x.InventoryIngredient.AddNewBatch(batch)).ReturnsAsync(0);

            // Act
            var result = await _service.AddNewBatch(batchDTO);

            // Assert
            Assert.Equal(0, result);
        }

        [Theory]
        [InlineData(1, 100, 1)]
        [InlineData(2, 200, 2)]
        [InlineData(3, 150, 3)]
        public async Task AddNewBatch_WithDifferentBatches_CallsRepositoryWithCorrectData(
            int ingredientId, decimal quantity, int warehouseId)
        {
            // Arrange
            var batchDTO = new InventoryBatchDTO
            {
                IngredientId = ingredientId,
                QuantityRemaining = quantity,
                WarehouseId = warehouseId
            };
            var batch = new InventoryBatch
            {
                IngredientId = ingredientId,
                QuantityRemaining = quantity,
                WarehouseId = warehouseId
            };

            _mockMapper.Setup(x => x.Map<InventoryBatch>(batchDTO)).Returns(batch);
            _mockUnitOfWork.Setup(x => x.InventoryIngredient.AddNewBatch(It.IsAny<InventoryBatch>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.AddNewBatch(batchDTO);

            // Assert
            _mockUnitOfWork.Verify(x => x.InventoryIngredient.AddNewBatch(
                It.Is<InventoryBatch>(b =>
                    b.IngredientId == ingredientId &&
                    b.QuantityRemaining == quantity &&
                    b.WarehouseId == warehouseId)), Times.Once);
        }

        #endregion

        #region UpdateIngredient Tests

        [Fact]
        public async Task UpdateIngredient_WhenSuccessful_ReturnsSuccessTrue()
        {
            // Arrange
            int idIngredient = 1;
            string nameIngredient = "Updated Tomato";
            int unit = 2;

            _mockUnitOfWork
                .Setup(x => x.InventoryIngredient.UpdateInforIngredient(idIngredient, nameIngredient, unit))
                .ReturnsAsync((true, "Cập nhật thành công"));

            // Act
            var result = await _service.UpdateIngredient(idIngredient, nameIngredient, unit);

            // Assert
            Assert.True(result.success);
            Assert.Equal("Cập nhật thành công", result.message);
            _mockUnitOfWork.Verify(
                x => x.InventoryIngredient.UpdateInforIngredient(idIngredient, nameIngredient, unit),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateIngredient_WhenFailed_ReturnsSuccessFalse()
        {
            // Arrange
            int idIngredient = 999;
            string nameIngredient = "NonExistent";
            int unit = 1;

            _mockUnitOfWork
                .Setup(x => x.InventoryIngredient.UpdateInforIngredient(idIngredient, nameIngredient, unit))
                .ReturnsAsync((false, "Không tìm thấy nguyên liệu"));

            // Act
            var result = await _service.UpdateIngredient(idIngredient, nameIngredient, unit);

            // Assert
            Assert.False(result.success);
            Assert.Equal("Không tìm thấy nguyên liệu", result.message);
        }

        [Theory]
        [InlineData(1, "Tomato Fresh", 1)]
        [InlineData(2, "Onion Organic", 2)]
        [InlineData(3, "Chicken Breast", 3)]
        public async Task UpdateIngredient_WithVariousInputs_CallsRepositoryCorrectly(
            int id, string name, int unit)
        {
            // Arrange
            _mockUnitOfWork
                .Setup(x => x.InventoryIngredient.UpdateInforIngredient(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync((true, "Success"));

            // Act
            var result = await _service.UpdateIngredient(id, name, unit);

            // Assert
            Assert.True(result.success);
            _mockUnitOfWork.Verify(
                x => x.InventoryIngredient.UpdateInforIngredient(id, name, unit),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateIngredient_WithEmptyName_StillCallsRepository()
        {
            // Arrange
            int idIngredient = 1;
            string nameIngredient = "";
            int unit = 1;

            _mockUnitOfWork
                .Setup(x => x.InventoryIngredient.UpdateInforIngredient(idIngredient, nameIngredient, unit))
                .ReturnsAsync((false, "Tên nguyên liệu không được để trống"));

            // Act
            var result = await _service.UpdateIngredient(idIngredient, nameIngredient, unit);

            // Assert
            Assert.False(result.success);
            Assert.Contains("trống", result.message);
        }

        [Fact]
        public async Task UpdateIngredient_WhenRepositoryThrowsException_PropagatesException()
        {
            // Arrange
            int idIngredient = 1;
            string nameIngredient = "Tomato";
            int unit = 1;

            _mockUnitOfWork
                .Setup(x => x.InventoryIngredient.UpdateInforIngredient(idIngredient, nameIngredient, unit))
                .ThrowsAsync(new System.Exception("Database connection error"));

            // Act & Assert
            await Assert.ThrowsAsync<System.Exception>(
                () => _service.UpdateIngredient(idIngredient, nameIngredient, unit)
            );
        }

        #endregion
    }
}
