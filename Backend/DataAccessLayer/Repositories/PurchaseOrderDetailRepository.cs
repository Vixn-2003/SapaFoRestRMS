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
    public class PurchaseOrderDetailRepository : IPurchaseOrderDetailRepository
    {
        private readonly SapaFoRestRmsContext _context;

        public PurchaseOrderDetailRepository(SapaFoRestRmsContext context)
        {
            _context = context;
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


        public async Task<IEnumerable<PurchaseOrderDetail>> GetPurchaseOrderDetails(string purchaseOrderId)
        {
            return await _context.PurchaseOrderDetails
                .Where(d => d.PurchaseOrderId == purchaseOrderId)
                .Include(d => d.Ingredient)        
                .ToListAsync();
        }


        public async Task<IEnumerable<PurchaseOrderDetail>> GetByIngredientIdAsync(int ingredientId)
        {
            return await _context.PurchaseOrderDetails.Where(pod => pod.IngredientId == ingredientId)
                .ToListAsync();
        }

        public async Task<IEnumerable<PurchaseOrderDetail>> GetByIngredientIdWithDetailsAsync(int ingredientId)
        {
            return await _context.PurchaseOrderDetails.Include(pod => pod.PurchaseOrder)
                    .ThenInclude(po => po.Supplier)
                .Where(pod => pod.IngredientId == ingredientId &&
                             pod.PurchaseOrder.Status == "Completed") // Chỉ lấy đơn đã xác nhận
                .OrderByDescending(pod => pod.PurchaseOrder.OrderDate)
                .ToListAsync();
        }
    }
}
