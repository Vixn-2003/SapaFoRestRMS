using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IAuditRepository
    {
        Task<bool> AddAsync(AuditInventory audit);
        Task<int> CountAsync(string count);
        Task<IEnumerable<AuditInventory>> GetAllAsync();

        Task<bool> ConfirmAuditReAsync(string id, AuditInventory request);
        Task<AuditInventory> GetAuditByIdReAsync(string id);
        Task<int> GetBatchIdByIdReAsync(string id);
        Task<string?> CheckExitsAuditStatusRe(int id);
    }
}
