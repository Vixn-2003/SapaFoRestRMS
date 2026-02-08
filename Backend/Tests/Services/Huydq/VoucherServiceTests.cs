using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services.Huydq
{
    public class VoucherServiceTests
    {
        private readonly Mock<IVoucherRepository> _voucherRepoMock;
        private readonly VoucherService _voucherService;

        public VoucherServiceTests()
        {
            _voucherRepoMock = new Mock<IVoucherRepository>();
            _voucherService = new VoucherService(_voucherRepoMock.Object);
        }
        [Fact]
        public async Task GetAllAsync_ReturnsVoucherDtos()
        {
            // Arrange
            var vouchers = new List<Voucher>
            {
                new Voucher
                {
                    VoucherId = 1,
                    Code = "SALE10",
                    DiscountType = "Phần trăm",
                    DiscountValue = 10,
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddDays(5),
                    IsDelete = false
                }
            };

            _voucherRepoMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(vouchers);

            // Act
            var result = await _voucherService.GetAllAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal("SALE10", result.First().Code);
        }
        [Fact]
        public async Task GetByIdAsync_WhenFound_ReturnsVoucherDto()
        {
            // Arrange
            var voucher = new Voucher
            {
                VoucherId = 1,
                Code = "SALE20",
                DiscountType = "Phần trăm",
                DiscountValue = 20,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(3)
            };

            _voucherRepoMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(voucher);

            // Act
            var result = await _voucherService.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("SALE20", result!.Code);
        }
        [Fact]
        public async Task CreateAsync_ValidVoucher_ReturnsVoucherDto()
        {
            // Arrange
            var dto = new VoucherCreateDto
            {
                Code = "NEW50",
                DiscountType = "Phần trăm",
                DiscountValue = 50,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(10),
                MinOrderValue = 100000,
                MaxDiscount = 50000
            };

            _voucherRepoMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Voucher>());

            _voucherRepoMock
                .Setup(r => r.AddAsync(It.IsAny<Voucher>()))
                .Returns(Task.CompletedTask);

            _voucherRepoMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _voucherService.CreateAsync(dto);

            // Assert
            Assert.Equal("NEW50", result.Code);
            Assert.Equal("Đang sử dụng", result.Status);
        }
        [Fact]
        public async Task CreateAsync_DiscountPercentOver100_ThrowsException()
        {
            // Arrange
            var dto = new VoucherCreateDto
            {
                Code = "ERROR",
                DiscountType = "Phần trăm",
                DiscountValue = 150,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1)
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _voucherService.CreateAsync(dto));

            Assert.Equal("Giá trị phần trăm phải từ 1 đến 100.", ex.Message);
        }
        [Fact]
        public async Task DeleteAsync_WhenFound_ReturnsTrue()
        {
            // Arrange
            var voucher = new Voucher { VoucherId = 1 };

            _voucherRepoMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(voucher);

            _voucherRepoMock
                .Setup(r => r.UpdateAsync(voucher))
                .Returns(Task.CompletedTask);

            _voucherRepoMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _voucherService.DeleteAsync(1);

            // Assert
            Assert.True(result);
            Assert.True(voucher.IsDelete);
        }
    }
}
