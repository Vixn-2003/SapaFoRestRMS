using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services.LuongTest
{
    public class EventServiceTests
    {
        private readonly Mock<IEventRepository> _eventRepoMock;
        private readonly Mock<ICloudinaryService> _cloudinaryMock;
        private readonly EventService _service;

        public EventServiceTests()
        {
            _eventRepoMock = new Mock<IEventRepository>();
            _cloudinaryMock = new Mock<ICloudinaryService>();

            _service = new EventService(_eventRepoMock.Object, _cloudinaryMock.Object);
        }

        // CASE 1: StartDate > EndDate -> ném ArgumentException
        [Fact]
        public async Task AddEventAsync_ShouldThrowArgumentException_WhenStartDateGreaterThanEndDate()
        {
            // Arrange
            var dto = new EventCreateDto
            {
                Title = "Sự kiện A",
                Description = "Mô tả",
                StartDate = new DateOnly(2025, 1, 10),
                EndDate = new DateOnly(2025, 1, 5),
                Location = "Hà Nội",
                Image = null
            };

            // Act + Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.AddEventAsync(dto));

            Assert.Equal("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc", ex.Message);

            // Đảm bảo không gọi repo / cloudinary
            _eventRepoMock.Verify(r => r.AddAsync(It.IsAny<Event>()), Times.Never);
            _cloudinaryMock.Verify(c => c.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
        }

        // CASE 2: Thêm event KHÔNG có ảnh
        [Fact]
        public async Task AddEventAsync_ShouldAddEventWithoutImage_WhenImageIsNull()
        {
            // Arrange
            var dto = new EventCreateDto
            {
                Title = "Sự kiện không ảnh",
                Description = "Mô tả",
                StartDate = new DateOnly(2025, 1, 1),
                EndDate = new DateOnly(2025, 1, 5),
                Location = "HCM",
                Image = null
            };

            Event? savedEvent = null;

            _eventRepoMock
                .Setup(r => r.AddAsync(It.IsAny<Event>()))
                .Returns<Event>(e =>
                {
                    savedEvent = e;
                    return Task.CompletedTask;
                });

            // Act
            var result = await _service.AddEventAsync(dto);

            // Assert DTO trả về
            Assert.Equal(dto.Title, result.Title);
            Assert.Equal(dto.Description, result.Description);
            Assert.Equal(dto.Location, result.Location);
            Assert.Equal(dto.StartDate, result.StartDate);
            Assert.Equal(dto.EndDate, result.EndDate);
            Assert.Null(result.ImageUrl);

            // Assert event truyền vào repository
            Assert.NotNull(savedEvent);
            Assert.Equal(dto.Title, savedEvent!.Title);
            Assert.Null(savedEvent.ImageUrl);

            // Không upload ảnh
            _cloudinaryMock.Verify(c => c.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
            _eventRepoMock.Verify(r => r.AddAsync(It.IsAny<Event>()), Times.Once);
        }

        // CASE 3: Thêm event CÓ ảnh
        [Fact]
        public async Task AddEventAsync_ShouldUploadImageAndSetImageUrl_WhenImageProvided()
        {
            // Arrange
            var dto = new EventCreateDto
            {
                Title = "Sự kiện có ảnh",
                Description = "Mô tả",
                StartDate = new DateOnly(2025, 2, 1),
                EndDate = new DateOnly(2025, 2, 5),
                Location = "Đà Nẵng",
                Image = CreateFakeFormFile("banner.png")
            };

            var expectedUrl = "https://cloudinary.com/events/banner.png";

            _cloudinaryMock
                .Setup(c => c.UploadImageAsync(dto.Image!, "events"))
                .ReturnsAsync(expectedUrl);

            Event? savedEvent = null;
            _eventRepoMock
                .Setup(r => r.AddAsync(It.IsAny<Event>()))
                .Returns<Event>(e =>
                {
                    savedEvent = e;
                    return Task.CompletedTask;
                });

            // Act
            var result = await _service.AddEventAsync(dto);

            // Assert DTO trả về
            Assert.Equal(expectedUrl, result.ImageUrl);

            // Assert event truyền vào repository
            Assert.NotNull(savedEvent);
            Assert.Equal(expectedUrl, savedEvent!.ImageUrl);

            // Đảm bảo gọi upload ảnh đúng 1 lần với folder "events"
            _cloudinaryMock.Verify(
                c => c.UploadImageAsync(dto.Image!, "events"),
                Times.Once);

            _eventRepoMock.Verify(r => r.AddAsync(It.IsAny<Event>()), Times.Once);
        }

        // Hàm tạo IFormFile fake để test upload
        private IFormFile CreateFakeFormFile(string fileName)
        {
            var ms = new MemoryStream(Encoding.UTF8.GetBytes("fake image content"));
            return new FormFile(ms, 0, ms.Length, "file", fileName);
        }
        [Fact]
        public async Task UpdateEventAsync_ShouldReturnNull_WhenEventNotFound()
        {
            // Arrange
            int id = 1;
            var dto = new EventUpdateDto
            {
                Title = "Sự kiện A",
                Description = "Mô tả",
                StartDate = new DateOnly(2025, 1, 1),
                EndDate = new DateOnly(2025, 1, 5),
                Location = "Địa điểm A",
                Image = null
            };

            _eventRepoMock.Setup(r => r.GetByIdAsync(id))
                          .ReturnsAsync((Event?)null);

            // Act
            var result = await _service.UpdateEventAsync(id, dto);

            // Assert
            Assert.Null(result);

            _cloudinaryMock.Verify(c => c.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
            _cloudinaryMock.Verify(c => c.DeleteImageAsync(It.IsAny<string>()), Times.Never);
            _eventRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Event>()), Times.Never);
        }
        [Fact]
        public async Task UpdateEventAsync_ShouldThrowArgumentException_WhenStartDateGreaterThanEndDate()
        {
            // Arrange
            int id = 1;

            var existing = new Event
            {
                EventId = id,
                Title = "Old",
                StartDate = new DateOnly(2025, 1, 1),
                EndDate = new DateOnly(2025, 1, 5),
                Location = "Old",
                ImageUrl = "old-url"
            };

            _eventRepoMock.Setup(r => r.GetByIdAsync(id))
                          .ReturnsAsync(existing);

            var dto = new EventUpdateDto
            {
                Title = "Sự kiện A",
                Description = "Mô tả",
                StartDate = new DateOnly(2025, 1, 10),
                EndDate = new DateOnly(2025, 1, 5),
                Location = "Địa điểm A",
                Image = null
            };

            // Act + Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateEventAsync(id, dto));

            Assert.Equal("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc", ex.Message);

            _cloudinaryMock.Verify(c => c.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
            _cloudinaryMock.Verify(c => c.DeleteImageAsync(It.IsAny<string>()), Times.Never);
            _eventRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Event>()), Times.Never);
        }
        [Fact]
        public async Task UpdateEventAsync_ShouldUpdateFieldsWithoutChangingImage_WhenImageIsNull()
        {
            // Arrange
            int id = 1;

            var existing = new Event
            {
                EventId = id,
                Title = "Old Title",
                Description = "Old desc",
                StartDate = new DateOnly(2025, 1, 1),
                EndDate = new DateOnly(2025, 1, 5),
                Location = "Old location",
                ImageUrl = "old-url"
            };

            _eventRepoMock.Setup(r => r.GetByIdAsync(id))
                          .ReturnsAsync(existing);

            _eventRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Event>()))
                          .Returns(Task.CompletedTask);

            var dto = new EventUpdateDto
            {
                Title = "New Title",
                Description = "New desc",
                StartDate = new DateOnly(2025, 2, 1),
                EndDate = new DateOnly(2025, 2, 5),
                Location = "New location",
                Image = null
            };

            // Act
            var result = await _service.UpdateEventAsync(id, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Title, result!.Title);
            Assert.Equal(dto.Description, result.Description);
            Assert.Equal(dto.Location, result.Location);
            Assert.Equal(dto.StartDate, result.StartDate);
            Assert.Equal(dto.EndDate, result.EndDate);
            Assert.Equal("old-url", result.ImageUrl);  // không đổi ảnh

            _cloudinaryMock.Verify(c => c.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
            _cloudinaryMock.Verify(c => c.DeleteImageAsync(It.IsAny<string>()), Times.Never);
            _eventRepoMock.Verify(r => r.UpdateAsync(existing), Times.Once);
        }
        [Fact]
        public async Task UpdateEventAsync_ShouldReplaceImage_WhenNewImageProvidedAndOldImageExists()
        {
            // Arrange
            int id = 1;

            var existing = new Event
            {
                EventId = id,
                Title = "Old Title",
                Description = "Old desc",
                StartDate = new DateOnly(2025, 1, 1),
                EndDate = new DateOnly(2025, 1, 5),
                Location = "Old location",
                ImageUrl = "old-url"
            };

            _eventRepoMock.Setup(r => r.GetByIdAsync(id))
                          .ReturnsAsync(existing);

            var newImage = CreateFakeFormFile("new-banner.png");
            var newUrl = "https://cloudinary.com/events/new-banner.png";

            _cloudinaryMock.Setup(c => c.UploadImageAsync(newImage, "events"))
                           .ReturnsAsync(newUrl);

            _eventRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Event>()))
                          .Returns(Task.CompletedTask);

            var dto = new EventUpdateDto
            {
                Title = "New Title",
                Description = "New desc",
                StartDate = new DateOnly(2025, 2, 1),
                EndDate = new DateOnly(2025, 2, 5),
                Location = "New location",
                Image = newImage
            };

            // Act
            var result = await _service.UpdateEventAsync(id, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newUrl, result!.ImageUrl);

            Assert.Equal(newUrl, existing.ImageUrl); // đã được gán lại

            _cloudinaryMock.Verify(c => c.DeleteImageAsync("old-url"), Times.Once);
            _cloudinaryMock.Verify(c => c.UploadImageAsync(newImage, "events"), Times.Once);
            _eventRepoMock.Verify(r => r.UpdateAsync(existing), Times.Once);
        }
    }
}
