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

        Task<IDbContextTransaction> BeginTransactionAsync();

        Task<int> SaveChangesAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
