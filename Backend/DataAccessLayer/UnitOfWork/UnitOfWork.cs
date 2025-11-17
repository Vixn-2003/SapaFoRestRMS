using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading.Tasks;

namespace DataAccessLayer.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly SapaFoRestRmsContext _context;


        private IManagerMenuRepository _menuRepository;
        private IManagerCategoryRepository _categoryRepository;

        private IManagerComboRepository _comboRepository;

        private IInventoryIngredientRepository _inventoryRepository;

        public IManagerMenuRepository MenuItem => _menuRepository ??= new ManagerMenuRepository(_context);
        public IManagerCategoryRepository MenuCategory => _categoryRepository ??= new ManagerCategoryRepository(_context);
        public IManagerComboRepository Combo => _comboRepository ??= new ManagerComboRepository(_context);
        public IInventoryIngredientRepository InventoryIngredient => _inventoryRepository ??= new InventoryIngredientRepository(_context);


        private IDbContextTransaction _transaction;


        private IUserRepository _users;

        public IUserRepository Users => _users ??= new UserRepository(_context);

        private IStaffProfileRepository _staffProfiles;

        public IStaffProfileRepository StaffProfiles => _staffProfiles ??= new StaffProfileRepository(_context);

        private IPositionRepository _positions;

        public IPositionRepository Positions => _positions ??= new PositionRepository(_context);

        private IPaymentRepository _payments;

        public IPaymentRepository Payments => _payments ??= new PaymentRepository(_context);

        private IAuditLogRepository _auditLogs;

        public IAuditLogRepository AuditLogs => _auditLogs ??= new AuditLogRepository(_context);

        private IOrderLockRepository _orderLocks;

        public IOrderLockRepository OrderLocks => _orderLocks ??= new OrderLockRepository(_context);

        public UnitOfWork(SapaFoRestRmsContext context)
        {
            _context = context;
        }
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
            return _transaction;
        }

        public async Task CommitAsync()
        {
            try
            {
                await _transaction.CommitAsync();
            }
            catch
            {
                await _transaction.RollbackAsync();
                throw;
            }
        }

        public async Task RollbackAsync()
        {
            await _transaction.RollbackAsync();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _transaction?.Dispose();
                    _context.Dispose();
                }
            }
            disposed = true;

        }
        // Giải phóng resource
        public void Dispose()
        {

            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
