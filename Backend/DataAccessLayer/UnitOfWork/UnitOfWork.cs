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
        private IDbContextTransaction _transaction;

        private IManagerMenuRepository _menuRepository;

        private IManagerComboRepository _comboRepository;


        public UnitOfWork(SapaFoRestRmsContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IManagerMenuRepository MenuItem => _menuRepository ??= new ManagerMenuRepository(_context);
        public IManagerComboRepository Combo => _comboRepository ??= new ManagerComboRepository(_context);

        // Bắt đầu transaction
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            if (_transaction == null)
            {
                _transaction = await _context.Database.BeginTransactionAsync();
            }
            return _transaction;
        }

        // Commit transaction
        public async Task CommitAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
            catch
            {
                await RollbackAsync();
                throw;
            }
        }

        // Rollback transaction
        public async Task RollbackAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        // Lưu thay đổi mà không cần transaction
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        // Giải phóng resource
        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }
    }
}
