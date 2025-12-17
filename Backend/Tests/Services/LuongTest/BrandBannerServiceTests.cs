using BusinessLogicLayer.Services;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services.LuongTest
{
    public class BrandBannerServiceTests
    {
        private readonly Mock<IBrandBannerRepository> _mockRepo;
        private readonly BrandBannerService _service;

        public BrandBannerServiceTests()
        {
            _mockRepo = new Mock<IBrandBannerRepository>();
            _service = new BrandBannerService(_mockRepo.Object);
        }

        [Fact]
        public async Task AddAsync_ShouldSetCreatedByAndCallRepositoryMethods()
        {
            // Arrange
            var banner = new BrandBanner
            {
                Title = "Test Banner",
                ImageUrl = "/images/test.png",
                Status = "Active"
            };

            _mockRepo.Setup(r => r.AddAsync(It.IsAny<BrandBanner>()))
                     .Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.SaveChangesAsync())
                     .Returns(Task.CompletedTask);

            // Act
            await _service.AddAsync(banner);

            // Assert
            // 1. Đảm bảo CreatedBy đã được set = 3
            Assert.Equal(3, banner.CreatedBy);

            // 2. Đảm bảo AddAsync được gọi đúng 1 lần với banner đã được set CreatedBy = 3
            _mockRepo.Verify(r => r.AddAsync(
                It.Is<BrandBanner>(b => b == banner && b.CreatedBy == 3)),
                Times.Once);

            // 3. Đảm bảo SaveChangesAsync được gọi đúng 1 lần
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
        [Fact]
        public async Task UpdateAsync_ShouldCallRepositoryUpdateAndSaveChanges()
        {
            // Arrange
            var banner = new BrandBanner
            {
                BannerId = 1,
                Title = "Updated Banner",
                ImageUrl = "/images/updated.png",
                Status = "Active",
                CreatedBy = 5     // giá trị này không bị service sửa
            };

            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<BrandBanner>()))
                     .Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.SaveChangesAsync())
                     .Returns(Task.CompletedTask);

            // Act
            await _service.UpdateAsync(banner);

            // Assert
            // 1. Đảm bảo UpdateAsync được gọi đúng 1 lần với đúng banner
            _mockRepo.Verify(r => r.UpdateAsync(
                It.Is<BrandBanner>(b => b == banner)),
                Times.Once);

            // 2. Đảm bảo SaveChangesAsync được gọi đúng 1 lần
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
    }
