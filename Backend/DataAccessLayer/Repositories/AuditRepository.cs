using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class AuditRepository : IAuditRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public AuditRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public async Task<bool> AddAsync(AuditInventory audit)
        {
            try
            {
                await _context.AuditInventories.AddAsync(audit);
                var result = await _context.SaveChangesAsync();
                if (result == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error adding audit to database: {ex.Message}");
                return false;
                
            }
        }

        public async Task<string?> CheckExitsAuditStatusRe(int batchId)
        {
            var audit = await _context.AuditInventories
                .Where(x => x.BatchId == batchId && x.AuditStatus == "processing")
                .FirstOrDefaultAsync();

            return audit?.AuditId;  // nếu null → trả null, nếu có → trả AuditId
        }


        public async Task<bool> ConfirmAuditReAsync(string id, AuditInventory request)
        {
            try
            {

                var audit = await _context.AuditInventories
                    .FirstOrDefaultAsync(x => x.AuditId == id);

                if (audit == null)
                {
                    Console.WriteLine("❌ Audit not found");
                    return false;
                }

                audit.AuditStatus = request.AuditStatus;
                audit.ConfirmerId = request.ConfirmerId;
                audit.ConfirmerName = request.ConfirmerName;
                audit.ConfirmerPhone = request.ConfirmerPhone;
                audit.ConfirmerPosition = request.ConfirmerPosition;
                audit.ConfirmedAt = request.ConfirmedAt;

                var result = await _context.SaveChangesAsync();

                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing audit to database: {ex.Message}");
                return false;
            }
        }


        public async Task<int> CountAsync(string count)
        {
            return await _context.AuditInventories
                .Where(a => a.AuditId.StartsWith(count))
                .CountAsync();
        }

        public  async Task<IEnumerable<AuditInventory>> GetAllAsync()
        {
            return await _context.AuditInventories.ToListAsync();
        }

        public async Task<AuditInventory?> GetAuditByIdReAsync(string id)
        {
            return await _context.AuditInventories
                .FirstOrDefaultAsync(a => a.AuditId == id);
        }

        public async Task<int> GetBatchIdByIdReAsync(string id)
        {
            var audit = await _context.AuditInventories
              .FirstOrDefaultAsync(a => a.AuditId == id);
            if (audit == null)
            {
                return 0;
            }
            else
            {
                return audit.BatchId;
            }

        }
    }
}
