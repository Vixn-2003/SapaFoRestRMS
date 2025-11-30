using BusinessAccessLayer.DTOs.Inventory;
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IAuditService
    {
        Task<bool> CreateAuditAsync(AuditInventory auditRecord);
        Task<int> CountAuditAsync(string count);
        Task<IEnumerable<AuditInventoryResponseDTO>> GetAllAuditsAsync();
        Task<bool> ConfirmAuditAsync(string id, AuditInventoryResponseDTO request);
        Task<string?> CheckExitsAuditStatus(int batchId);
        Task<AuditInventoryResponseDTO> GetAuditByIdAsync(string id);
    }
}
