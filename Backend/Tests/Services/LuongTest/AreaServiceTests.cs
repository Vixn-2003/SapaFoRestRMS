using Moq;
using Xunit;
using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using System.Threading.Tasks;

namespace Tests.Services.LuongTest
{
    public class AreaServiceTests
    {
        private readonly Mock<IAreaRepository> _mockRepo;  // Use IAreaRepository instead of IRepository<Area>
        private readonly AreaService _service;

        public AreaServiceTests()
        {
            _mockRepo = new Mock<IAreaRepository>(); // Mock IAreaRepository
            _service = new AreaService(_mockRepo.Object);
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnError_WhenAreaNameIsEmpty()
        {
            // Arrange
            var dto = new AreaDto { AreaName = "", Floor = 1, Description = "Test description" };

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Tên khu vực không được để trống.", result.Message);
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnError_WhenFloorIsLessThanOrEqualToZero()
        {
            // Arrange
            var dto = new AreaDto { AreaName = "Test Area", Floor = 0, Description = "Test description" };

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Số tầng phải lớn hơn 0.", result.Message);
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnError_WhenAreaAlreadyExists()
        {
            // Arrange
            var dto = new AreaDto { AreaName = "Test Area", Floor = 1, Description = "Test description" };
            _mockRepo.Setup(r => r.ExistsAsync(dto.AreaName, dto.Floor, It.IsAny<int?>()))
             .ReturnsAsync(true); // Simulate non-existing area
                                  // Simulate existing area

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Khu vực đã tồn tại trong tầng này.", result.Message);
        }

        public async Task CreateAsync_ShouldReturnSuccess_WhenAreaIsValid()
        {
            // Arrange
            var dto = new AreaDto { AreaName = "New Area", Floor = 2, Description = "Test description" };

            // Setup mock to simulate non-existing area
            _mockRepo.Setup(r => r.ExistsAsync(dto.AreaName, dto.Floor, It.IsAny<int?>())).ReturnsAsync(false);

            // Setup mock to simulate adding area
            _mockRepo.Setup(r => r.AddAsync(It.IsAny<Area>())).Returns(Task.CompletedTask); // Simulate adding area

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Thêm khu vực thành công.", result.Message);
        }


        [Fact]
        public async Task UpdateAsync_ShouldReturnError_WhenAreaNotFound()
        {
            // Arrange
            var dto = new AreaDto { AreaId = 1, AreaName = "Test Area", Floor = 1, Description = "Test description" };
            _mockRepo.Setup(r => r.GetByIdAsync(dto.AreaId)).ReturnsAsync((Area?)null); // Simulate that the area doesn't exist

            // Act
            var result = await _service.UpdateAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Không tìm thấy khu vực.", result.Message);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnError_WhenAreaNameIsEmpty()
        {
            // Arrange
            var dto = new AreaDto { AreaId = 1, AreaName = "", Floor = 1, Description = "Test description" };
            _mockRepo.Setup(r => r.GetByIdAsync(dto.AreaId)).ReturnsAsync(new Area()); // Simulate that the area exists

            // Act
            var result = await _service.UpdateAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Tên khu vực không được để trống.", result.Message);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnError_WhenFloorIsLessThanOrEqualToZero()
        {
            // Arrange
            var dto = new AreaDto { AreaId = 1, AreaName = "Test Area", Floor = 0, Description = "Test description" };
            _mockRepo.Setup(r => r.GetByIdAsync(dto.AreaId)).ReturnsAsync(new Area()); // Simulate that the area exists

            // Act
            var result = await _service.UpdateAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Số tầng phải lớn hơn 0.", result.Message);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnError_WhenAreaAlreadyExists()
        {
            // Arrange
            var dto = new AreaDto { AreaId = 1, AreaName = "Test Area", Floor = 1, Description = "Test description" };
            _mockRepo.Setup(r => r.GetByIdAsync(dto.AreaId)).ReturnsAsync(new Area()); // Simulate that the area exists
            _mockRepo.Setup(r => r.ExistsAsync(dto.AreaName, dto.Floor, dto.AreaId)).ReturnsAsync(true); // Simulate existing area

            // Act
            var result = await _service.UpdateAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Khu vực đã tồn tại trong tầng này.", result.Message);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnSuccess_WhenAreaIsValid()
        {
            // Arrange
            var dto = new AreaDto { AreaId = 1, AreaName = "Updated Area", Floor = 2, Description = "Updated description" };
            _mockRepo.Setup(r => r.GetByIdAsync(dto.AreaId)).ReturnsAsync(new Area { AreaId = 1 }); // Simulate that the area exists
            _mockRepo.Setup(r => r.ExistsAsync(dto.AreaName, dto.Floor, dto.AreaId)).ReturnsAsync(false); // Simulate no conflict

            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Area>())).Returns(Task.CompletedTask); // Simulate updating area

            // Act
            var result = await _service.UpdateAsync(dto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Cập nhật khu vực thành công.", result.Message);
        }

    }
}
