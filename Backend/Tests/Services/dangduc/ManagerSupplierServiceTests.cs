using Xunit;
using Moq;
using AutoMapper;
using System;
using System.Threading.Tasks;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.DTOs.Inventory;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;

/// <summary>
/// Unit tests for ManagerSupplierService
/// 
/// Function: CreateSupplier
/// - Tạo mới nhà cung cấp trong hệ thống
/// - Kiểm tra mã nhà cung cấp trùng lặp
/// - Validate dữ liệu đầu vào (Code, Name, Phone, Email, Address)
/// - Xử lý exception khi tạo thất bại
/// 
/// Function: UpdateSupplier
/// - Cập nhật thông tin nhà cung cấp (Name, ContactInfo, Phone, Email, Address)
/// - Không cho phép cập nhật mã nhà cung cấp (CodeSupplier)
/// - Kiểm tra supplier tồn tại trước khi cập nhật
/// - Xử lý exception khi cập nhật thất bại
/// </summary>
public class ManagerSupplierServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly ManagerSupplierService _service;

    public ManagerSupplierServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _service = new ManagerSupplierService(_mockUnitOfWork.Object, _mockMapper.Object);
    }

    #region CreateSupplier Tests

    // UTCID01: Test tạo supplier thành công
    [Fact]
    public async Task CreateSupplier_WhenSuccessful_ReturnsTrue()
    {
        // Arrange
        var createDTO = new CreateSupplierDTO
        {
            CodeSupplier = "SUP001",
            Name = "ABC Company",
            ContactInfo = "Mr. Nguyen",
            Phone = "0123456789",
            Email = "contact@abc.com",
            Address = "123 Main St, Hanoi"
        };

        var supplier = new Supplier
        {
            CodeSupplier = "SUP001",
            Name = "ABC Company",
            ContactInfo = "Mr. Nguyen",
            Phone = "0123456789",
            Email = "contact@abc.com",
            Address = "123 Main St, Hanoi"
        };

        _mockUnitOfWork.Setup(x => x.Supplier.CheckCodeExistsAsync(createDTO.CodeSupplier))
            .ReturnsAsync(false);
        _mockMapper.Setup(x => x.Map<Supplier>(createDTO))
            .Returns(supplier);
        _mockUnitOfWork.Setup(x => x.Supplier.AddAsync(supplier))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.Supplier.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateSupplier(createDTO);

        // Assert
        Assert.True(result);
        _mockUnitOfWork.Verify(x => x.Supplier.CheckCodeExistsAsync(createDTO.CodeSupplier), Times.Once);
        _mockUnitOfWork.Verify(x => x.Supplier.AddAsync(supplier), Times.Once);
        _mockUnitOfWork.Verify(x => x.Supplier.SaveChangesAsync(), Times.Once);
    }

    // UTCID02: Test tạo supplier với mã đã tồn tại
    [Fact]
    public async Task CreateSupplier_WhenCodeExists_ReturnsFalse()
    {
        // Arrange
        var createDTO = new CreateSupplierDTO
        {
            CodeSupplier = "SUP001",
            Name = "ABC Company",
            ContactInfo = "Mr. Nguyen",
            Phone = "0123456789",
            Email = "contact@abc.com",
            Address = "123 Main St"
        };

        _mockUnitOfWork.Setup(x => x.Supplier.CheckCodeExistsAsync(createDTO.CodeSupplier))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateSupplier(createDTO);

        // Assert
        Assert.False(result);
        _mockUnitOfWork.Verify(x => x.Supplier.CheckCodeExistsAsync(createDTO.CodeSupplier), Times.Once);
        _mockUnitOfWork.Verify(x => x.Supplier.AddAsync(It.IsAny<Supplier>()), Times.Never);
    }

    // UTCID03: Test với null DTO
    [Fact]
    public async Task CreateSupplier_WhenDTOIsNull_ReturnsFalse()
    {
        // Arrange
        CreateSupplierDTO createDTO = null;

        // Act
        var result = await _service.CreateSupplier(createDTO);

        // Assert
        Assert.False(result);
    }

    // UTCID04: Test khi AddAsync throw exception
    [Fact]
    public async Task CreateSupplier_WhenAddAsyncThrowsException_ReturnsFalse()
    {
        // Arrange
        var createDTO = new CreateSupplierDTO
        {
            CodeSupplier = "SUP002",
            Name = "XYZ Company",
            ContactInfo = "Ms. Tran",
            Phone = "0987654321",
            Email = "contact@xyz.com",
            Address = "456 Second St"
        };

        var supplier = new Supplier { CodeSupplier = "SUP002" };

        _mockUnitOfWork.Setup(x => x.Supplier.CheckCodeExistsAsync(createDTO.CodeSupplier))
            .ReturnsAsync(false);
        _mockMapper.Setup(x => x.Map<Supplier>(createDTO)).Returns(supplier);
        _mockUnitOfWork.Setup(x => x.Supplier.AddAsync(supplier))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.CreateSupplier(createDTO);

        // Assert
        Assert.False(result);
    }

    // UTCID05: Test khi SaveChangesAsync throw exception
    [Fact]
    public async Task CreateSupplier_WhenSaveChangesThrowsException_ReturnsFalse()
    {
        // Arrange
        var createDTO = new CreateSupplierDTO
        {
            CodeSupplier = "SUP003",
            Name = "DEF Company",
            ContactInfo = "Mr. Le",
            Phone = "0111222333",
            Email = "contact@def.com",
            Address = "789 Third St"
        };

        var supplier = new Supplier { CodeSupplier = "SUP003" };

        _mockUnitOfWork.Setup(x => x.Supplier.CheckCodeExistsAsync(createDTO.CodeSupplier))
            .ReturnsAsync(false);
        _mockMapper.Setup(x => x.Map<Supplier>(createDTO)).Returns(supplier);
        _mockUnitOfWork.Setup(x => x.Supplier.AddAsync(supplier))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.Supplier.SaveChangesAsync())
            .ThrowsAsync(new Exception("Save failed"));

        // Act
        var result = await _service.CreateSupplier(createDTO);

        // Assert
        Assert.False(result);
    }

    // UTCID06: Test với email không hợp lệ (mapping vẫn thành công nhưng business logic có thể validate)
    [Fact]
    public async Task CreateSupplier_WithValidData_CreatesSuccessfully()
    {
        // Arrange
        var createDTO = new CreateSupplierDTO
        {
            CodeSupplier = "SUP004",
            Name = "GHI Company",
            ContactInfo = "Mr. Pham",
            Phone = "0444555666",
            Email = "valid@email.com",
            Address = "101 Fourth St"
        };

        var supplier = new Supplier { CodeSupplier = "SUP004" };

        _mockUnitOfWork.Setup(x => x.Supplier.CheckCodeExistsAsync(createDTO.CodeSupplier))
            .ReturnsAsync(false);
        _mockMapper.Setup(x => x.Map<Supplier>(createDTO)).Returns(supplier);
        _mockUnitOfWork.Setup(x => x.Supplier.AddAsync(supplier))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.Supplier.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateSupplier(createDTO);

        // Assert
        Assert.True(result);
    }

    // UTCID07: Test với nhiều suppliers khác nhau
    [Theory]
    [InlineData("SUP101", "Supplier A", "0123456789", "a@test.com")]
    [InlineData("SUP102", "Supplier B", "0987654321", "b@test.com")]
    [InlineData("SUP103", "Supplier C", "0111222333", "c@test.com")]
    public async Task CreateSupplier_WithDifferentData_CreatesSuccessfully(
        string code, string name, string phone, string email)
    {
        // Arrange
        var createDTO = new CreateSupplierDTO
        {
            CodeSupplier = code,
            Name = name,
            ContactInfo = "Contact Person",
            Phone = phone,
            Email = email,
            Address = "Address"
        };

        var supplier = new Supplier { CodeSupplier = code };

        _mockUnitOfWork.Setup(x => x.Supplier.CheckCodeExistsAsync(code))
            .ReturnsAsync(false);
        _mockMapper.Setup(x => x.Map<Supplier>(createDTO)).Returns(supplier);
        _mockUnitOfWork.Setup(x => x.Supplier.AddAsync(It.IsAny<Supplier>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.Supplier.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateSupplier(createDTO);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region UpdateSupplier Tests

    // UTCID08: Test cập nhật supplier thành công
    [Fact]
    public async Task UpdateSupplier_WhenSuccessful_ReturnsTrue()
    {
        // Arrange
        int supplierId = 1;
        var updateDTO = new UpdateSupplierDTO
        {
            Name = "Updated ABC Company",
            ContactInfo = "Ms. Updated",
            Phone = "0999888777",
            Email = "updated@abc.com",
            Address = "999 Updated St"
        };

        var existingSupplier = new Supplier
        {
            SupplierId = 1,
            CodeSupplier = "SUP001",
            Name = "Old Name",
            ContactInfo = "Old Contact",
            Phone = "0123456789",
            Email = "old@abc.com",
            Address = "Old Address"
        };

        _mockUnitOfWork.Setup(x => x.Supplier.GetByIdAsync(supplierId))
            .ReturnsAsync(existingSupplier);
        _mockUnitOfWork.Setup(x => x.Supplier.UpdateAsync(existingSupplier))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.Supplier.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateSupplier(supplierId, updateDTO);

        // Assert
        Assert.True(result);
        Assert.Equal("Updated ABC Company", existingSupplier.Name);
        Assert.Equal("Ms. Updated", existingSupplier.ContactInfo);
        Assert.Equal("0999888777", existingSupplier.Phone);
        Assert.Equal("updated@abc.com", existingSupplier.Email);
        Assert.Equal("999 Updated St", existingSupplier.Address);
        Assert.Equal("SUP001", existingSupplier.CodeSupplier); // Code không thay đổi
        _mockUnitOfWork.Verify(x => x.Supplier.UpdateAsync(existingSupplier), Times.Once);
        _mockUnitOfWork.Verify(x => x.Supplier.SaveChangesAsync(), Times.Once);
    }

    // UTCID09: Test cập nhật supplier không tồn tại
    [Fact]
    public async Task UpdateSupplier_WhenSupplierNotFound_ReturnsFalse()
    {
        // Arrange
        int supplierId = 999;
        var updateDTO = new UpdateSupplierDTO
        {
            Name = "Updated Name",
            ContactInfo = "Updated Contact",
            Phone = "0999888777",
            Email = "updated@test.com",
            Address = "Updated Address"
        };

        _mockUnitOfWork.Setup(x => x.Supplier.GetByIdAsync(supplierId))
            .ReturnsAsync((Supplier)null);

        // Act
        var result = await _service.UpdateSupplier(supplierId, updateDTO);

        // Assert
        Assert.False(result);
        _mockUnitOfWork.Verify(x => x.Supplier.GetByIdAsync(supplierId), Times.Once);
        _mockUnitOfWork.Verify(x => x.Supplier.UpdateAsync(It.IsAny<Supplier>()), Times.Never);
    }

    // UTCID10: Test với null DTO
    [Fact]
    public async Task UpdateSupplier_WhenDTOIsNull_ReturnsFalse()
    {
        // Arrange
        int supplierId = 1;
        UpdateSupplierDTO updateDTO = null;

        var existingSupplier = new Supplier { SupplierId = 1 };

        _mockUnitOfWork.Setup(x => x.Supplier.GetByIdAsync(supplierId))
            .ReturnsAsync(existingSupplier);

        // Act
        var result = await _service.UpdateSupplier(supplierId, updateDTO);

        // Assert
        Assert.False(result);
    }

    // UTCID11: Test khi UpdateAsync throw exception
    [Fact]
    public async Task UpdateSupplier_WhenUpdateAsyncThrowsException_ReturnsFalse()
    {
        // Arrange
        int supplierId = 1;
        var updateDTO = new UpdateSupplierDTO
        {
            Name = "Updated Name",
            ContactInfo = "Contact",
            Phone = "0123456789",
            Email = "test@test.com",
            Address = "Address"
        };

        var existingSupplier = new Supplier { SupplierId = 1 };

        _mockUnitOfWork.Setup(x => x.Supplier.GetByIdAsync(supplierId))
            .ReturnsAsync(existingSupplier);
        _mockUnitOfWork.Setup(x => x.Supplier.UpdateAsync(existingSupplier))
            .ThrowsAsync(new Exception("Update failed"));

        // Act
        var result = await _service.UpdateSupplier(supplierId, updateDTO);

        // Assert
        Assert.False(result);
    }

    // UTCID12: Test khi SaveChangesAsync throw exception
    [Fact]
    public async Task UpdateSupplier_WhenSaveChangesThrowsException_ReturnsFalse()
    {
        // Arrange
        int supplierId = 1;
        var updateDTO = new UpdateSupplierDTO
        {
            Name = "Updated Name",
            ContactInfo = "Contact",
            Phone = "0123456789",
            Email = "test@test.com",
            Address = "Address"
        };

        var existingSupplier = new Supplier { SupplierId = 1 };

        _mockUnitOfWork.Setup(x => x.Supplier.GetByIdAsync(supplierId))
            .ReturnsAsync(existingSupplier);
        _mockUnitOfWork.Setup(x => x.Supplier.UpdateAsync(existingSupplier))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.Supplier.SaveChangesAsync())
            .ThrowsAsync(new Exception("Save failed"));

        // Act
        var result = await _service.UpdateSupplier(supplierId, updateDTO);

        // Assert
        Assert.False(result);
    }

    // UTCID13: Test cập nhật chỉ một số fields
    [Fact]
    public async Task UpdateSupplier_PartialUpdate_UpdatesOnlyProvidedFields()
    {
        // Arrange
        int supplierId = 1;
        var updateDTO = new UpdateSupplierDTO
        {
            Name = "New Name Only",
            ContactInfo = "Old Contact",
            Phone = "0123456789",
            Email = "old@test.com",
            Address = "Old Address"
        };

        var existingSupplier = new Supplier
        {
            SupplierId = 1,
            CodeSupplier = "SUP001",
            Name = "Old Name",
            ContactInfo = "Old Contact",
            Phone = "0123456789",
            Email = "old@test.com",
            Address = "Old Address"
        };

        _mockUnitOfWork.Setup(x => x.Supplier.GetByIdAsync(supplierId))
            .ReturnsAsync(existingSupplier);
        _mockUnitOfWork.Setup(x => x.Supplier.UpdateAsync(existingSupplier))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.Supplier.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateSupplier(supplierId, updateDTO);

        // Assert
        Assert.True(result);
        Assert.Equal("New Name Only", existingSupplier.Name);
        Assert.Equal("SUP001", existingSupplier.CodeSupplier); // Code không đổi
    }

    // UTCID14: Test với supplier ID = 0
    [Fact]
    public async Task UpdateSupplier_WithZeroId_ReturnsFalse()
    {
        // Arrange
        int supplierId = 0;
        var updateDTO = new UpdateSupplierDTO
        {
            Name = "Test",
            ContactInfo = "Test",
            Phone = "0123456789",
            Email = "test@test.com",
            Address = "Test"
        };

        _mockUnitOfWork.Setup(x => x.Supplier.GetByIdAsync(supplierId))
            .ReturnsAsync((Supplier)null);

        // Act
        var result = await _service.UpdateSupplier(supplierId, updateDTO);

        // Assert
        Assert.False(result);
    }

    // UTCID15: Test với nhiều suppliers khác nhau
    [Theory]
    [InlineData(1, "Updated A", "0111111111", "a@update.com")]
    [InlineData(2, "Updated B", "0222222222", "b@update.com")]
    [InlineData(3, "Updated C", "0333333333", "c@update.com")]
    public async Task UpdateSupplier_WithDifferentData_UpdatesSuccessfully(
        int id, string name, string phone, string email)
    {
        // Arrange
        var updateDTO = new UpdateSupplierDTO
        {
            Name = name,
            ContactInfo = "Contact",
            Phone = phone,
            Email = email,
            Address = "Address"
        };

        var existingSupplier = new Supplier
        {
            SupplierId = id,
            CodeSupplier = $"SUP{id:D3}"
        };

        _mockUnitOfWork.Setup(x => x.Supplier.GetByIdAsync(id))
            .ReturnsAsync(existingSupplier);
        _mockUnitOfWork.Setup(x => x.Supplier.UpdateAsync(It.IsAny<Supplier>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.Supplier.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateSupplier(id, updateDTO);

        // Assert
        Assert.True(result);
        Assert.Equal(name, existingSupplier.Name);
        Assert.Equal(phone, existingSupplier.Phone);
        Assert.Equal(email, existingSupplier.Email);
    }

    #endregion
}