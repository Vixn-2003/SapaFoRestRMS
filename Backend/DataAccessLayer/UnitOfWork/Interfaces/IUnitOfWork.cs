﻿using DataAccessLayer.Repositories.Interfaces;
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
        IMenuRepository MenuItem { get; }

        IComboRepository Combo { get; }

        Task<IDbContextTransaction> BeginTransactionAsync();

        Task<int> SaveChangesAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
