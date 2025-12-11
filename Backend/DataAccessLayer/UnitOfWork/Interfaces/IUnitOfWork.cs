using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DataAccessLayer.UnitOfWork.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IManagerMenuRepository MenuItem { get; }
        IManagerCategoryRepository MenuCategory { get; }
        IInventoryIngredientRepository InventoryIngredient { get; }

        IPurchaseOrderDetailRepository PurchaseOrderDetail { get; }
        IPurchaseOrderRepository PurchaseOrder { get; }
        IStockTransactionRepository StockTransaction { get; }
        IManagerSupplierRepository Supplier { get; }
        ISupplierRepository SupplierManager { get; }
        IAuditRepository AuditRepository { get; }
        IUnitRepository UnitRepository { get; }
        IWarehouseRepository Warehouse { get; }
        IManagerComboRepository Combo { get; }
        IUserRepository Users { get; }
        IStaffProfileRepository StaffProfiles { get; }
        IPositionRepository Positions { get; }
        IPaymentRepository Payments { get; }
        IAuditLogRepository AuditLogs { get; }
        IOrderLockRepository OrderLocks { get; }
        IOrderRepository Orders { get; }
        IOrderDetailRepository OrderDetails { get; }
        IOrderComboItemRepository OrderComboItems { get; }
        ITableRepository Tables { get; }
        IShiftRepository Shifts { get; }

        IShiftCounterRepository ShiftCounters { get; }
        IReservationRepository Reservations { get; }
        
        ICustomerManagementRepository CustomerManagement { get; }
        
        IStaffManagementRepository StaffManagement { get; }

        Task<IDbContextTransaction> BeginTransactionAsync();

        Task<int> SaveChangesAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
