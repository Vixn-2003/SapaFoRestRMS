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
    public class PurchaseOrderRepository : IPurchaseOrderRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public PurchaseOrderRepository(SapaFoRestRmsContext context)
        {
            _context = context;
        }

        public Task AddAsync(PurchaseOrder entity)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> CreatePurchaseOrderAsync(PurchaseOrder order, List<PurchaseOrderDetail> details)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                order.Status = "Processing";

                order.OrderDate ??= DateTime.Now;

                _context.PurchaseOrders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var d in details)
                {
                    d.PurchaseOrderId = order.PurchaseOrderId;
                }
                _context.PurchaseOrderDetails.AddRange(details);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"❌ Lỗi khi thêm đơn hàng: {ex.Message}");
                return false;
            }
        }

        public Task DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<PurchaseOrder>> GetAllAsync()
        {
            return await _context.PurchaseOrders.Include(p => p.Creator)
                .Include(p => p.Confirmer)
                .Include(x => x.Supplier)
                .Include(x => x.PurchaseOrderDetails)
                .ToListAsync();
        }

        public async Task<PurchaseOrder?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
            
        }

        public async Task<PurchaseOrder?> GetByIdPurchase(string id)
        {
            return await _context.PurchaseOrders.Include( p => p.Creator).Include(p => p.Confirmer)
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseOrderDetails)                   
                .FirstOrDefaultAsync(p => p.PurchaseOrderId == id);
        }

        public async Task<bool> PurchaseOrderCompleted(int idConfirm, DateTime timeConfirm, string purchaseOrderId)
        {
            try
            {
                var purchaseOrder = await _context.PurchaseOrders.FindAsync(purchaseOrderId);

                if (purchaseOrder == null)
                    return false;

                var confirmer = await _context.Users.FindAsync(idConfirm);
                if (confirmer == null)
                    return false;

                purchaseOrder.IdConfirm = idConfirm;
                purchaseOrder.TimeConfirm = timeConfirm;

                purchaseOrder.Status = "Completed";  

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi khi xác thực đơn hàng: {ex.Message}");
                return false;
            }
        }

        public Task SaveChangesAsync()
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(PurchaseOrder entity)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> AddIdNewIngredient(int idDetailOrder, int idIngredient)
        {
            // Lấy detail theo ID
            var detailOrder = await _context.PurchaseOrderDetails
                .FirstOrDefaultAsync(p => p.PurchaseOrderDetailId == idDetailOrder);

            if (detailOrder == null)
                return false;

            // Gán IngredientId mới
            detailOrder.IngredientId = idIngredient;

            // Lưu thay đổi
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ConfirmOrder(string purchaseOrderId, int idChecker, DateTime time, string status)
        {
            try
            {
                // 1. Tìm đơn hàng theo ID
                var order = await _context.PurchaseOrders
                    .FirstOrDefaultAsync(p => p.PurchaseOrderId == purchaseOrderId);

                if (order == null)
                    return false;

                order.IdConfirm = idChecker;
                order.TimeConfirm = time;
                order.Status = status;

                // 4. Lưu thay đổi
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi khi xác nhận đơn hàng: {ex.Message}");
                return false;
            }
        }

    }
}
