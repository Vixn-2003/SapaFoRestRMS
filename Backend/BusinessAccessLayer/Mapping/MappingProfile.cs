using AutoMapper;
using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.DTOs.UserManagement;
using BusinessAccessLayer.DTOs.Users;
using BusinessAccessLayer.DTOs.Positions;
using BusinessAccessLayer.DTOs.Payment;
using DomainAccessLayer.Models;
using Role = DomainAccessLayer.Models.Role;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Mapping
{
    public class MappingProfile : Profile
    {

        public MappingProfile()
        {

            CreateMap<MenuItem, ManagerMenuDTO>();
            CreateMap<ManagerMenuDTO, MenuItem>();

            CreateMap<Combo, ManagerComboDTO>();
            CreateMap<ComboItem, ManagerComboItemDTO>().ForMember(dest => dest.ManagerMenuItem,
                      opt => opt.MapFrom(src => src.MenuItem)); ;

            CreateMap<MenuCategory, ManagerCategoryDTO>();
            CreateMap<ManagerCategoryDTO, MenuCategory>();
            CreateMap<Recipe, RecipeDTO>();
            CreateMap<RecipeDTO, Recipe>();

            CreateMap<Ingredient, InventoryIngredientDTO>()
             .ForMember(dest => dest.Batches,
               opt => opt.MapFrom(src => src.InventoryBatches));

            CreateMap<InventoryBatch, InventoryBatchDTO>();
            CreateMap<InventoryBatchDTO, InventoryBatch>();

            CreateMap<StockTransaction, StockTransactionDTO>();

            // User -> StaffProfileDto
            CreateMap<User, StaffProfileDto>()
                .ForMember(d => d.RoleName, m => m.MapFrom(s => s.Role.RoleName))
                .ForMember(d => d.PositionNames, m => m.MapFrom(s => s.Staff.SelectMany(st => st.Positions.Select(p => p.PositionName)).Distinct().ToList()))
                .ForMember(d => d.DateOfBirth, m => m.Ignore())
                .ForMember(d => d.Gender, m => m.Ignore());

            CreateMap<Supplier, SupplierDTO>();
            CreateMap<SupplierDTO, Supplier>();
            CreateMap<PurchaseOrder, PurchaseOrderDTO>();
            CreateMap<PurchaseOrderDTO, PurchaseOrder>();
            CreateMap<PurchaseOrderDetailDTO, PurchaseOrderDetail>();
            CreateMap<PurchaseOrderDetail, PurchaseOrderDetailDTO>();
            CreateMap<Ingredient, IngredientDTO>();
            CreateMap<IngredientDTO, Ingredient>();


            //BatchIngredient 
            CreateMap<InventoryBatch, BatchIngredientDTO>()
                .ForMember(dest => dest.IngredientName, opt => opt.MapFrom(src => src.Ingredient.Name))
                .ForMember(dest => dest.IngredientUnit, opt => opt.MapFrom(src => src.Ingredient.Unit))
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse.Name))
                .ForMember(dest => dest.PurchaseOrderId, opt => opt.MapFrom(src => src.PurchaseOrderDetail.PurchaseOrderId))
                .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(src => src.PurchaseOrderDetail.PurchaseOrder.OrderDate))
                .ForMember(dest => dest.OrderStatus, opt => opt.MapFrom(src => src.PurchaseOrderDetail.PurchaseOrder.Status))
                .ForMember(dest => dest.SupplierId, opt => opt.MapFrom(src => src.PurchaseOrderDetail.PurchaseOrder.Supplier.SupplierId))
                .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src => src.PurchaseOrderDetail.PurchaseOrder.Supplier.Name))
                .ForMember(dest => dest.SupplierCode, opt => opt.MapFrom(src => src.PurchaseOrderDetail.PurchaseOrder.Supplier.CodeSupplier))
                .ForMember(dest => dest.SupplierPhone, opt => opt.MapFrom(src => src.PurchaseOrderDetail.PurchaseOrder.Supplier.Phone))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
            CreateMap<WarehouseDTO, Warehouse>();
            CreateMap<Warehouse, WarehouseDTO>();
            CreateMap<StockTransactionDTO, StockTransaction>();
            CreateMap<AuditInventoryRequestDTO, AuditInventory>();
            CreateMap<AuditInventory, AuditInventoryRequestDTO>();
            CreateMap<AuditInventoryResponseDTO, AuditInventory>()
            .ForMember(d => d.AuditStatus, opt => opt.MapFrom(src => src.AuditStatus));

            CreateMap<AuditInventory, AuditInventoryResponseDTO>();

            CreateMap<InventoryBatch, PurchaseOrderDetailWithSupplierDTO>()
               .ForMember(dest => dest.IngredientName,
                   opt => opt.MapFrom(src => src.Ingredient.Name))
               .ForMember(dest => dest.IngredientCode,
                   opt => opt.MapFrom(src => src.Ingredient.IngredientCode))
               .ForMember(dest => dest.WarehouseName,
                   opt => opt.MapFrom(src => src.Warehouse.Name))
               .ForMember(dest => dest.Unit,
                   opt => opt.MapFrom(src => src.Ingredient.Unit.UnitName))
               .ForMember(dest => dest.PurchaseOrderId,
                   opt => opt.MapFrom(src =>
                       src.PurchaseOrderDetail != null && src.PurchaseOrderDetail.PurchaseOrder != null
                           ? src.PurchaseOrderDetail.PurchaseOrder.PurchaseOrderId
                           : null));


            CreateMap<BestSellerDto, MenuItem>();
            CreateMap<MenuItem, BestSellerDto>();


            CreateMap<MenuItem, MenuItemStatisticsDto>()
        .ForMember(dest => dest.CategoryId,
            opt => opt.MapFrom(src => src.CategoryId))
        .ForMember(dest => dest.Description,
            opt => opt.MapFrom(src => src.Description ?? ""))
        .ForMember(dest => dest.CourseType,
            opt => opt.MapFrom(src => src.CourseType))
        .ForMember(dest => dest.ImageUrl,
            opt => opt.MapFrom(src => src.ImageUrl ?? ""))
        .ForMember(dest => dest.IsAvailable,
            opt => opt.MapFrom(src => src.IsAvailable ?? false))
        .ForMember(dest => dest.IsAds,
            opt => opt.MapFrom(src => src.IsAds ?? false))
        .ForMember(dest => dest.TimeCook,
            opt => opt.MapFrom(src => src.TimeCook))
        .ForMember(dest => dest.BillingType,
            opt => opt.MapFrom(src => src.BillingType))
        .ForMember(dest => dest.Recipes,  // 🔥 QUAN TRỌNG
            opt => opt.MapFrom(src => src.Recipes))
        // Statistics fields
        .ForMember(dest => dest.ServedToday, opt => opt.Ignore())
        .ForMember(dest => dest.ServedYesterday, opt => opt.Ignore())
        .ForMember(dest => dest.Average7Days, opt => opt.Ignore())
        .ForMember(dest => dest.Average30Days, opt => opt.Ignore())
        .ForMember(dest => dest.Average90Days, opt => opt.Ignore())
        .ForMember(dest => dest.CompareWithYesterday, opt => opt.Ignore())
        .ForMember(dest => dest.CompareWith7Days, opt => opt.Ignore())
        .ForMember(dest => dest.CompareWith30Days, opt => opt.Ignore());


            CreateMap<MenuItem, MenuItemDto>()
                .ForMember(dest => dest.CategoryName,
                    opt => opt.MapFrom(src => src.Category != null ? src.Category.CategoryName : string.Empty))
                .ForMember(dest => dest.IsAvailable,
                    opt => opt.MapFrom(src => src.IsAvailable ?? false))
                .ForMember(dest => dest.ImageUrl,
                    opt => opt.MapFrom(src => src.ImageUrl ?? string.Empty))
                .ForMember(dest => dest.Description,
                    opt => opt.MapFrom(src => src.Description ?? string.Empty));


            // User mappings
            CreateMap<User, UserDto>()
                .ForMember(d => d.RoleName, m => m.Ignore()); // RoleName is loaded separately in service

            // Position mappings
            CreateMap<Position, PositionDto>();
            CreateMap<PositionCreateRequest, Position>();
            CreateMap<PositionUpdateRequest, Position>();

            // SalaryChangeRequest mappings
            CreateMap<SalaryChangeRequest, SalaryChangeRequestDto>()
                .ForMember(d => d.PositionName, m => m.MapFrom(s => s.Position != null ? s.Position.PositionName : ""))
                .ForMember(d => d.RequestedByName, m => m.MapFrom(s => s.RequestedByUser != null ? s.RequestedByUser.FullName : ""))
                .ForMember(d => d.ApprovedByName, m => m.MapFrom(s => s.ApprovedByUser != null ? s.ApprovedByUser.FullName : null));

            // Role mappings
            CreateMap<Role, RoleDto>()
                .ForMember(d => d.Description, m => m.MapFrom(s => string.Empty)); // Role model doesn't have Description

            // Payment mappings
            CreateMap<Order, OrderDto>()
                .ForMember(d => d.OrderCode, m => m.MapFrom(s => $"ORD-{s.OrderId:D6}"))
                .ForMember(d => d.Subtotal, m => m.Ignore())
                .ForMember(d => d.VatAmount, m => m.Ignore())
                .ForMember(d => d.ServiceFee, m => m.Ignore())
                .ForMember(d => d.DiscountAmount, m => m.Ignore())
                .ForMember(d => d.CustomerName, m => m.Ignore())
                .ForMember(d => d.TableNumber, m => m.Ignore())
                .ForMember(d => d.StaffName, m => m.Ignore())
                .ForMember(d => d.OrderItems, m => m.MapFrom(s => s.OrderDetails));

            CreateMap<OrderDetail, OrderItemDto>()
                .ForMember(d => d.MenuItemId, m => m.MapFrom(s => s.MenuItemId))
                .ForMember(d => d.MenuItemName, m => m.MapFrom(s => s.MenuItem != null ? s.MenuItem.Name : (s.Combo != null ? s.Combo.Name : "")))
                .ForMember(d => d.ComboId, m => m.MapFrom(s => s.ComboId))
                .ForMember(d => d.ComboName, m => m.MapFrom(s => s.Combo != null ? s.Combo.Name : null))
                .ForMember(d => d.TotalPrice, m => m.MapFrom(s => s.UnitPrice * s.Quantity))
                // NEW: Map BillingType and Kitchen Status
                .ForMember(d => d.BillingType, m => m.MapFrom(s => s.MenuItem != null ? (int?)s.MenuItem.BillingType : null))
                .ForMember(d => d.KitchenStatus, m => m.MapFrom(s => s.Status))
                .ForMember(d => d.QuantityUsed, m => m.MapFrom(s => s.QuantityUsed ?? s.Quantity))
                .ForMember(d => d.CanCancel, m => m.MapFrom(s => 
                    s.MenuItem != null &&
                    s.MenuItem.BillingType == DomainAccessLayer.Enums.ItemBillingType.KitchenPrepared &&
                    (s.Status == "Pending" || s.Status == "Confirmed" || s.Status == null)));


            CreateMap<Transaction, TransactionDto>();
            CreateMap<Unit, UnitDTO>()
    .ForMember(dest => dest.IngredientCount,
        opt => opt.MapFrom(src => src.Ingredients != null ? src.Ingredients.Count : 0));



            CreateMap<Supplier, SupplierListDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.SupplierId))
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.CodeSupplier))
            .ForMember(dest => dest.Contact, opt => opt.MapFrom(src => src.ContactInfo))
            // Giả lập IsActive = true, bạn cần thêm trường này vào model Supplier nếu muốn quản lý
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))

            // Các trường thống kê KHÔNG CÒN thuộc tính OnTimeRate
            .ForMember(dest => dest.TotalOrders, opt => opt.Ignore())
            .ForMember(dest => dest.TotalValue, opt => opt.Ignore())
            .ForMember(dest => dest.LastOrder, opt => opt.Ignore());

            CreateMap<PurchaseOrder, OrderHistoryDto>()
        .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.PurchaseOrderId))
        .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.OrderDate))

        // Bỏ qua Total và Items vì chúng được Service tính toán sau
        .ForMember(dest => dest.Total, opt => opt.Ignore())
        .ForMember(dest => dest.Items, opt => opt.Ignore());

            CreateMap<Supplier, TopSupplierDto>()
    .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
    .ForMember(dest => dest.Value, opt => opt.Ignore())
    .ForMember(dest => dest.Orders, opt => opt.Ignore());

            CreateMap<CreateSupplierDTO, Supplier>()
       .ForMember(dest => dest.SupplierId, opt => opt.Ignore())
       .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
       .ForMember(dest => dest.PurchaseOrders, opt => opt.Ignore());

            CreateMap<UpdateSupplierDTO, Supplier>()
                .ForMember(dest => dest.SupplierId, opt => opt.Ignore())
                .ForMember(dest => dest.CodeSupplier, opt => opt.Ignore()) // Không cho phép sửa mã
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.PurchaseOrders, opt => opt.Ignore());

            CreateMap<StockTransaction, StockTransactionInventoryDTO>()
    // ===== Transaction =====
    .ForMember(dest => dest.BatchId, opt => opt.MapFrom(src => src.BatchId))

    // ===== Batch =====
    .ForMember(dest => dest.QuantityRemaining, opt => opt.MapFrom(src => src.Batch.QuantityRemaining))
    .ForMember(dest => dest.ExpiryDate, opt => opt.MapFrom(src => src.Batch.ExpiryDate))
    .ForMember(dest => dest.BatchCreatedAt, opt => opt.MapFrom(src => src.Batch.CreatedAt))
    

    // ===== Ingredient =====
    .ForMember(dest => dest.IngredientId, opt => opt.MapFrom(src => src.Batch.IngredientId))
    .ForMember(dest => dest.IngredientCode, opt => opt.MapFrom(src => src.Batch.Ingredient.IngredientCode))
    .ForMember(dest => dest.IngredientName, opt => opt.MapFrom(src => src.Batch.Ingredient.Name))

    // ===== Unit =====
    .ForMember(dest => dest.UnitId, opt => opt.MapFrom(src => src.Batch.Ingredient.UnitId))
    .ForMember(dest => dest.UnitName, opt => opt.MapFrom(src => src.Batch.Ingredient.Unit.UnitName))
    .ForMember(dest => dest.UnitType, opt => opt.MapFrom(src => src.Batch.Ingredient.Unit.UnitType.ToString()))

    // ===== Warehouse =====
    .ForMember(dest => dest.WarehouseId, opt => opt.MapFrom(src => src.Batch.WarehouseId))
    .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Batch.Warehouse.Name))

    // ===== PurchaseOrderDetail =====
    .ForMember(dest => dest.PurchaseOrderDetailId, opt => opt.MapFrom(src => src.Batch.PurchaseOrderDetailId))
    .ForMember(dest => dest.PurchaseOrderId, opt => opt.MapFrom(src => src.Batch.PurchaseOrderDetail.PurchaseOrderId))

    // ===== PurchaseOrder =====
    .ForMember(dest => dest.PurchaseOrderStatus,
               opt => opt.MapFrom(src => src.Batch.PurchaseOrderDetail.PurchaseOrder.Status))
    .ForMember(dest => dest.PurchaseOrderDate,
               opt => opt.MapFrom(src => src.Batch.PurchaseOrderDetail.PurchaseOrder.OrderDate))
    .ForMember(dest => dest.SupplierName,
               opt => opt.MapFrom(src => src.Batch.PurchaseOrderDetail.PurchaseOrder.Supplier.Name))
     .ForMember(dest => dest.SupplierCode,
        opt => opt.MapFrom(src => src.Batch.PurchaseOrderDetail.PurchaseOrder.Supplier.CodeSupplier));


            CreateMap<PurchaseOrderDetail, PurchaseOrderDetailWithSupplierDTO>()
                    .ForMember(dest => dest.SupplierId,
                        opt => opt.MapFrom(src => src.PurchaseOrder.SupplierId))
                    .ForMember(dest => dest.OrderDate,
                        opt => opt.MapFrom(src => src.PurchaseOrder.OrderDate))
                    .ForMember(dest => dest.Status,
                        opt => opt.MapFrom(src => src.PurchaseOrder.Status))
                    .ForMember(dest => dest.SupplierName,
                        opt => opt.MapFrom(src => src.PurchaseOrder.Supplier.Name))
                    .ForMember(dest => dest.SupplierCode,
                        opt => opt.MapFrom(src => src.PurchaseOrder.Supplier.CodeSupplier))
                    .ForMember(dest => dest.SupplierPhone,
                        opt => opt.MapFrom(src => src.PurchaseOrder.Supplier.Phone))
                    .ForMember(dest => dest.SupplierEmail,
                        opt => opt.MapFrom(src => src.PurchaseOrder.Supplier.Email));
        }

    }
}
