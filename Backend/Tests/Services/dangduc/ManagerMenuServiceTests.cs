using Xunit;
using Moq;
using AutoMapper;
using System;
using System.Threading.Tasks;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.DTOs.Manager;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using DomainAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Unit tests for ManagerMenuService
/// 
/// Function: AddRecipe
/// - Thêm mới công thức (recipe) cho món ăn
/// - Mapping từ RecipeDTO sang Recipe entity
/// - Xử lý trường hợp thêm thành công và thất bại
/// 
/// Function: UpdateManagerMenu
/// - Cập nhật thông tin món ăn (tên, giá, mô tả, category, availability, etc.)
/// - Validate dữ liệu đầu vào (null check)
/// - Xử lý các loại exception: AutoMapperMappingException, DbUpdateException
/// 
/// Function: CreateManagerMenu
/// - Tạo mới món ăn trong hệ thống
/// - Trả về MenuItemId của món ăn vừa tạo
/// - Xử lý exception khi tạo thất bại
/// </summary>
public class ManagerMenuServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly ManagerMenuService _service;

    public ManagerMenuServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _service = new ManagerMenuService(_mockUnitOfWork.Object, _mockMapper.Object);
    }

    #region AddRecipe Tests

    // UTCID01: Test thêm recipe thành công
    [Fact]
    public async Task AddRecipe_WhenSuccessful_ReturnsTrue()
    {
        // Arrange
        var recipeDTO = new RecipeDTO
        {
            MenuItemId = 1,
            IngredientId = 10,
            QuantityNeeded = 2.5m
        };

        var recipe = new Recipe
        {
            MenuItemId = 1,
            IngredientId = 10,
            QuantityNeeded = 2.5m
        };

        _mockMapper.Setup(x => x.Map<Recipe>(recipeDTO))
            .Returns(recipe);
        _mockUnitOfWork.Setup(x => x.MenuItem.AddRecipe(recipe))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AddRecipe(recipeDTO);

        // Assert
        Assert.True(result);
        _mockMapper.Verify(x => x.Map<Recipe>(recipeDTO), Times.Once);
        _mockUnitOfWork.Verify(x => x.MenuItem.AddRecipe(recipe), Times.Once);
    }

    // UTCID02: Test thêm recipe thất bại
    [Fact]
    public async Task AddRecipe_WhenFailed_ReturnsFalse()
    {
        // Arrange
        var recipeDTO = new RecipeDTO
        {
            MenuItemId = 999,
            IngredientId = 10,
            QuantityNeeded = 2.5m
        };

        var recipe = new Recipe
        {
            MenuItemId = 999,
            IngredientId = 10,
            QuantityNeeded = 2.5m
        };

        _mockMapper.Setup(x => x.Map<Recipe>(recipeDTO)).Returns(recipe);
        _mockUnitOfWork.Setup(x => x.MenuItem.AddRecipe(recipe))
            .ReturnsAsync(false);

        // Act
        var result = await _service.AddRecipe(recipeDTO);

        // Assert
        Assert.False(result);
    }

    // UTCID03: Test với null RecipeDTO
    [Fact]
    public async Task AddRecipe_WhenDTOIsNull_ThrowsException()
    {
        // Arrange
        RecipeDTO recipeDTO = null;

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(
            () => _service.AddRecipe(recipeDTO)
        );
    }

    // UTCID04: Test với QuantityNeeded = 0
    [Fact]
    public async Task AddRecipe_WhenQuantityIsZero_ReturnsTrue()
    {
        // Arrange
        var recipeDTO = new RecipeDTO
        {
            MenuItemId = 1,
            IngredientId = 10,
            QuantityNeeded = 0
        };

        var recipe = new Recipe
        {
            MenuItemId = 1,
            IngredientId = 10,
            QuantityNeeded = 0
        };

        _mockMapper.Setup(x => x.Map<Recipe>(recipeDTO)).Returns(recipe);
        _mockUnitOfWork.Setup(x => x.MenuItem.AddRecipe(recipe))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AddRecipe(recipeDTO);

        // Assert
        Assert.True(result);
    }

    // UTCID05: Test với repository throw exception
    [Fact]
    public async Task AddRecipe_WhenRepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var recipeDTO = new RecipeDTO
        {
            MenuItemId = 1,
            IngredientId = 10,
            QuantityNeeded = 2.5m
        };

        var recipe = new Recipe
        {
            MenuItemId = 1,
            IngredientId = 10,
            QuantityNeeded = 2.5m
        };

        _mockMapper.Setup(x => x.Map<Recipe>(recipeDTO)).Returns(recipe);
        _mockUnitOfWork.Setup(x => x.MenuItem.AddRecipe(recipe))
            .ThrowsAsync(new Exception("Database connection error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _service.AddRecipe(recipeDTO)
        );
    }

    // UTCID06: Test với nhiều recipes khác nhau
    [Theory]
    [InlineData(1, 10, 2.5)]
    [InlineData(2, 20, 1.0)]
    [InlineData(3, 30, 0.5)]
    public async Task AddRecipe_WithDifferentRecipes_CallsRepositoryCorrectly(
        int menuItemId, int ingredientId, decimal quantity)
    {
        // Arrange
        var recipeDTO = new RecipeDTO
        {
            MenuItemId = menuItemId,
            IngredientId = ingredientId,
            QuantityNeeded = quantity
        };

        var recipe = new Recipe
        {
            MenuItemId = menuItemId,
            IngredientId = ingredientId,
            QuantityNeeded = quantity
        };

        _mockMapper.Setup(x => x.Map<Recipe>(recipeDTO)).Returns(recipe);
        _mockUnitOfWork.Setup(x => x.MenuItem.AddRecipe(It.IsAny<Recipe>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AddRecipe(recipeDTO);

        // Assert
        Assert.True(result);
        _mockUnitOfWork.Verify(x => x.MenuItem.AddRecipe(
            It.Is<Recipe>(r =>
                r.MenuItemId == menuItemId &&
                r.IngredientId == ingredientId &&
                r.QuantityNeeded == quantity)), Times.Once);
    }

    #endregion

    #region UpdateManagerMenu Tests

    // UTCID07: Test cập nhật menu thành công
    [Fact]
    public async Task UpdateManagerMenu_WhenSuccessful_ReturnsTrue()
    {
        // Arrange
        var menuDTO = new ManagerMenuDTO
        {
            MenuItemId = 1,
            Name = "Updated Dish",
            CategoryId = 2,
            Price = 150000,
            IsAvailable = true,
            CourseType = "Main",
            Description = "Updated description",
            TimeCook = 30,
            BillingType = ItemBillingType.KitchenPrepared
        };

        var menuItem = new MenuItem
        {
            MenuItemId = 1,
            Name = "Updated Dish",
            CategoryId = 2,
            Price = 150000,
            IsAvailable = true
        };

        _mockMapper.Setup(x => x.Map<MenuItem>(menuDTO))
            .Returns(menuItem);
        _mockUnitOfWork.Setup(x => x.MenuItem.ManagerUpdateMenu(menuItem))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateManagerMenu(menuDTO);

        // Assert
        Assert.True(result);
        _mockMapper.Verify(x => x.Map<MenuItem>(menuDTO), Times.Once);
        _mockUnitOfWork.Verify(x => x.MenuItem.ManagerUpdateMenu(menuItem), Times.Once);
    }

    // UTCID08: Test cập nhật menu thất bại
    [Fact]
    public async Task UpdateManagerMenu_WhenFailed_ReturnsFalse()
    {
        // Arrange
        var menuDTO = new ManagerMenuDTO
        {
            MenuItemId = 999,
            Name = "NonExistent Dish",
            Price = 100000
        };

        var menuItem = new MenuItem
        {
            MenuItemId = 999,
            Name = "NonExistent Dish"
        };

        _mockMapper.Setup(x => x.Map<MenuItem>(menuDTO)).Returns(menuItem);
        _mockUnitOfWork.Setup(x => x.MenuItem.ManagerUpdateMenu(menuItem))
            .ReturnsAsync(false);

        // Act
        var result = await _service.UpdateManagerMenu(menuDTO);

        // Assert
        Assert.False(result);
    }

    // UTCID09: Test với null DTO
    [Fact]
    public async Task UpdateManagerMenu_WhenDTOIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        ManagerMenuDTO menuDTO = null;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.UpdateManagerMenu(menuDTO)
        );
        Assert.Contains("Dữ liệu cập nhật không được để trống", exception.Message);
    }

    // UTCID10: Test với AutoMapperMappingException
    [Fact]
    public async Task UpdateManagerMenu_WhenMappingFails_ReturnsFalse()
    {
        // Arrange
        var menuDTO = new ManagerMenuDTO
        {
            MenuItemId = 1,
            Name = "Test Dish"
        };

        _mockMapper.Setup(x => x.Map<MenuItem>(menuDTO))
            .Throws(new AutoMapperMappingException("Mapping error"));

        // Act
        var result = await _service.UpdateManagerMenu(menuDTO);

        // Assert
        Assert.False(result);
    }

    // UTCID11: Test với DbUpdateException
    [Fact]
    public async Task UpdateManagerMenu_WhenDatabaseUpdateFails_ReturnsFalse()
    {
        // Arrange
        var menuDTO = new ManagerMenuDTO
        {
            MenuItemId = 1,
            Name = "Test Dish"
        };

        var menuItem = new MenuItem { MenuItemId = 1 };

        _mockMapper.Setup(x => x.Map<MenuItem>(menuDTO)).Returns(menuItem);
        _mockUnitOfWork.Setup(x => x.MenuItem.ManagerUpdateMenu(menuItem))
            .ThrowsAsync(new DbUpdateException("Database constraint violation"));

        // Act
        var result = await _service.UpdateManagerMenu(menuDTO);

        // Assert
        Assert.False(result);
    }

    // UTCID12: Test với general exception
    [Fact]
    public async Task UpdateManagerMenu_WhenUnexpectedErrorOccurs_ReturnsFalse()
    {
        // Arrange
        var menuDTO = new ManagerMenuDTO
        {
            MenuItemId = 1,
            Name = "Test Dish"
        };

        var menuItem = new MenuItem { MenuItemId = 1 };

        _mockMapper.Setup(x => x.Map<MenuItem>(menuDTO)).Returns(menuItem);
        _mockUnitOfWork.Setup(x => x.MenuItem.ManagerUpdateMenu(menuItem))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _service.UpdateManagerMenu(menuDTO);

        // Assert
        Assert.False(result);
    }

    // UTCID13: Test cập nhật với giá = 0
    [Fact]
    public async Task UpdateManagerMenu_WhenPriceIsZero_ReturnsTrue()
    {
        // Arrange
        var menuDTO = new ManagerMenuDTO
        {
            MenuItemId = 1,
            Name = "Free Dish",
            Price = 0,
            IsAvailable = true
        };

        var menuItem = new MenuItem
        {
            MenuItemId = 1,
            Name = "Free Dish",
            Price = 0
        };

        _mockMapper.Setup(x => x.Map<MenuItem>(menuDTO)).Returns(menuItem);
        _mockUnitOfWork.Setup(x => x.MenuItem.ManagerUpdateMenu(menuItem))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateManagerMenu(menuDTO);

        // Assert
        Assert.True(result);
    }

    // UTCID14: Test cập nhật IsAvailable = false
    [Fact]
    public async Task UpdateManagerMenu_WhenSetUnavailable_ReturnsTrue()
    {
        // Arrange
        var menuDTO = new ManagerMenuDTO
        {
            MenuItemId = 1,
            Name = "Unavailable Dish",
            Price = 100000,
            IsAvailable = false
        };

        var menuItem = new MenuItem
        {
            MenuItemId = 1,
            Name = "Unavailable Dish",
            IsAvailable = false
        };

        _mockMapper.Setup(x => x.Map<MenuItem>(menuDTO)).Returns(menuItem);
        _mockUnitOfWork.Setup(x => x.MenuItem.ManagerUpdateMenu(menuItem))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateManagerMenu(menuDTO);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region CreateManagerMenu Tests

    // UTCID15: Test tạo menu thành công
    [Fact]
    public async Task CreateManagerMenu_WhenSuccessful_ReturnsMenuItemId()
    {
        // Arrange
        var menuDTO = new ManagerMenuDTO
        {
            Name = "New Dish",
            CategoryId = 1,
            Price = 120000,
            IsAvailable = true,
            CourseType = "Main",
            Description = "Delicious new dish",
            ImageUrl = "http://example.com/image.jpg",
            TimeCook = 25,
            BillingType = ItemBillingType.KitchenPrepared,
            IsAds = false
        };

        _mockUnitOfWork.Setup(x => x.MenuItem.CreateManagerMenuRe(It.IsAny<MenuItem>()))
            .Callback<MenuItem>(m => m.MenuItemId = 100)
            .Returns(Task.FromResult(0));
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateManagerMenu(menuDTO);

        // Assert
        Assert.Equal(100, result);
        _mockUnitOfWork.Verify(x => x.MenuItem.CreateManagerMenuRe(
            It.Is<MenuItem>(m =>
                m.Name == "New Dish" &&
                m.Price == 120000 &&
                m.IsAvailable == true)), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    // UTCID16: Test với null DTO
    [Fact]
    public async Task CreateManagerMenu_WhenDTOIsNull_ThrowsException()
    {
        // Arrange
        ManagerMenuDTO menuDTO = null;

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(
            () => _service.CreateManagerMenu(menuDTO)
        );
    }

    // UTCID17: Test khi CreateManagerMenuRe throw exception
    [Fact]
    public async Task CreateManagerMenu_WhenRepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var menuDTO = new ManagerMenuDTO
        {
            Name = "New Dish",
            Price = 120000
        };

        _mockUnitOfWork.Setup(x => x.MenuItem.CreateManagerMenuRe(It.IsAny<MenuItem>()))
            .Returns((Task<int>)Task.FromException(new Exception("Database error")));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _service.CreateManagerMenu(menuDTO)
        );
        Assert.Contains("Database error", exception.Message);
    }

    // UTCID18: Test khi SaveChangesAsync thất bại
    [Fact]
    public async Task CreateManagerMenu_WhenSaveChangesFails_ThrowsException()
    {
        // Arrange
        var menuDTO = new ManagerMenuDTO
        {
            Name = "New Dish",
            Price = 120000
        };

        _mockUnitOfWork.Setup(x => x.MenuItem.CreateManagerMenuRe(It.IsAny<MenuItem>()))
            .Returns(Task.FromResult(0));
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
            .ThrowsAsync(new DbUpdateException("Save failed"));

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(
            () => _service.CreateManagerMenu(menuDTO)
        );
    }

    // UTCID19: Test tạo menu với Price = 0
    [Fact]
    public async Task CreateManagerMenu_WhenPriceIsZero_ReturnsMenuItemId()
    {
        // Arrange
        var menuDTO = new ManagerMenuDTO
        {
            Name = "Free Item",
            CategoryId = 1,
            Price = 0,
            IsAvailable = true,
            CourseType = "Appetizer"
        };

        _mockUnitOfWork.Setup(x => x.MenuItem.CreateManagerMenuRe(It.IsAny<MenuItem>()))
            .Callback<MenuItem>(m => m.MenuItemId = 200)
            .Returns(Task.FromResult(0));
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateManagerMenu(menuDTO);

        // Assert
        Assert.Equal(200, result);
    }

    // UTCID20: Test với tất cả fields đầy đủ
    [Fact]
    public async Task CreateManagerMenu_WithAllFields_CreatesSuccessfully()
    {
        // Arrange
        var menuDTO = new ManagerMenuDTO
        {
            Name = "Complete Dish",
            CategoryId = 2,
            Price = 250000,
            IsAvailable = true,
            CourseType = "Dessert",
            Description = "A complete description",
            ImageUrl = "http://example.com/complete.jpg",
            TimeCook = 45,
            BatchSize = 10,
            BillingType = ItemBillingType.KitchenPrepared,
            IsAds = true
        };

        _mockUnitOfWork.Setup(x => x.MenuItem.CreateManagerMenuRe(It.IsAny<MenuItem>()))
            .Callback<MenuItem>(m => m.MenuItemId = 300)
            .Returns(Task.FromResult(0));
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateManagerMenu(menuDTO);

        // Assert
        Assert.Equal(300, result);
        _mockUnitOfWork.Verify(x => x.MenuItem.CreateManagerMenuRe(
            It.Is<MenuItem>(m =>
                m.Name == "Complete Dish" &&
                m.CategoryId == 2 &&
                m.Price == 250000 &&
                m.Description == "A complete description" &&
                m.TimeCook == 45 &&
                m.IsAds == true)), Times.Once);
    }

    // UTCID21: Test với CategoryId null
    [Fact]
    public async Task CreateManagerMenu_WhenCategoryIdIsNull_ReturnsMenuItemId()
    {
        // Arrange
        var menuDTO = new ManagerMenuDTO
        {
            Name = "No Category Dish",
            CategoryId = null,
            Price = 100000,
            IsAvailable = true,
            CourseType = "Main"
        };

        _mockUnitOfWork.Setup(x => x.MenuItem.CreateManagerMenuRe(It.IsAny<MenuItem>()))
            .Callback<MenuItem>(m => m.MenuItemId = 400)
            .Returns(Task.FromResult(0));
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.CreateManagerMenu(menuDTO);

        // Assert
        Assert.Equal(400, result);
        _mockUnitOfWork.Verify(x => x.MenuItem.CreateManagerMenuRe(
            It.Is<MenuItem>(m => m.CategoryId == null)), Times.Once);
    }

    #endregion
}