using AutoMapper;
using BusinessAccessLayer.DTOs.ManagementCombo;
using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.Services;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using Moq;
using Xunit;
using static BusinessAccessLayer.DTOs.ManagementCombo.UpdateDtosCombo;

namespace Tests.Services.Huydq
{
    public class ManagerComboServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IManagerComboRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly ManagerComboService _service;

        public ManagerComboServiceTests()
        {
            _uowMock = new Mock<IUnitOfWork>();
            _repoMock = new Mock<IManagerComboRepository>();
            _mapperMock = new Mock<IMapper>();

            _service = new ManagerComboService(
                _uowMock.Object,
                _mapperMock.Object,
                _repoMock.Object
            );
        }

        // ================= GetManagerAllCombo =================

        [Fact]
        public async Task GetManagerAllCombo_ReturnsMappedDtos()
        {
            var combos = new List<Combo>
            {
                new Combo { ComboId = 1, Name = "Combo A" }
            };

            _uowMock.Setup(u => u.Combo.GetManagerAllCombos())
                    .ReturnsAsync(combos);

            _mapperMock.Setup(m => m.Map<IEnumerable<ManagerComboDTO>>(combos))
                       .Returns(new List<ManagerComboDTO>
                       {
                           new ManagerComboDTO { ComboId = 1, Name = "Combo A" }
                       });

            var result = await _service.GetManagerAllCombo();

            Assert.Single(result);
            Assert.Equal("Combo A", result.First().Name);
        }


        [Fact]
        public async Task GetByIdAsync_NotFound_ThrowsKeyNotFound()
        {
            _repoMock.Setup(r => r.GetComboWithItemsAsync(1))
                     .ReturnsAsync((Combo)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetByIdAsync(1));
        }

        // ================= UpdateAsync =================

        [Fact]
        public async Task UpdateAsync_ComboInUse_ThrowsInvalidOperation()
        {
            var combo = new Combo
            {
                ComboId = 1,
                OrderDetails = new List<OrderDetail>
                {
                    new OrderDetail { Status = "Pending" }
                }
            };

            _repoMock.Setup(r => r.GetComboWithItemsAsync(1))
                     .ReturnsAsync(combo);

            var request = new UpdateComboDto
            {
                Name = "Combo mới",
                Items = new List<ComboItemInput>
                {
                    new ComboItemInput { MenuItemId = 1, Quantity = 2 }
                }
            };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UpdateAsync(1, request));

            Assert.Contains("đang được sử dụng", ex.Message);
        }

        [Fact]
        public async Task UpdateAsync_EnableCombo_WithUnavailableMenuItem_ThrowsException()
        {
            var combo = new Combo
            {
                ComboId = 1,
                OrderDetails = new List<OrderDetail>()
            };

            _repoMock.Setup(r => r.GetComboWithItemsAsync(1))
                     .ReturnsAsync(combo);

            _repoMock.Setup(r => r.GetMenuItemsByIdsAsync(It.IsAny<List<int>>()))
                     .ReturnsAsync(new List<MenuItem>
                     {
                         new MenuItem { Name = "Burger", IsAvailable = false }
                     });

            var request = new UpdateComboDto
            {
                Name = "Combo bật",
                IsAvailable = true,
                Items = new List<ComboItemInput>
                {
                    new ComboItemInput { MenuItemId = 1, Quantity = 2 }
                }
            };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UpdateAsync(1, request));

            Assert.Contains("ngừng bán", ex.Message);
        }

        [Fact]
        public async Task UpdateAsync_ValidRequest_CallsUpdateRepo()
        {
            var combo = new Combo
            {
                ComboId = 1,
                OrderDetails = new List<OrderDetail>()
            };

            _repoMock.Setup(r => r.GetComboWithItemsAsync(1))
                     .ReturnsAsync(combo);

            _repoMock.Setup(r => r.GetMenuItemsByIdsAsync(It.IsAny<List<int>>()))
                     .ReturnsAsync(new List<MenuItem>
                     {
                         new MenuItem { IsAvailable = true }
                     });

            _repoMock.Setup(r => r.UpdateComboAsync(
                It.IsAny<Combo>(),
                It.IsAny<List<ComboItem>>()))
                .Returns(Task.CompletedTask);

            var request = new UpdateComboDto
            {
                Name = "Combo OK",
                IsAvailable = true,
                Items = new List<ComboItemInput>
                {
                    new ComboItemInput { MenuItemId = 1, Quantity = 2 }
                }
            };

            await _service.UpdateAsync(1, request);

            _repoMock.Verify(r => r.UpdateComboAsync(
                It.IsAny<Combo>(),
                It.IsAny<List<ComboItem>>()),
                Times.Once);
        }
    }
}
