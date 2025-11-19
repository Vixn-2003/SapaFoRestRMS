using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
    {
        Task<bool> CreatePurchaseOrderAsync(PurchaseOrder order, List<PurchaseOrderDetail> details);
        Task<bool> PurchaseOrderCompleted(int idConfirm, DateTime timeConfirm, string purchaseOrderId);
        Task<PurchaseOrder?> GetByIdPurchase(string id);
        Task<bool> AddIdNewIngredient(int idDetailOrder, int idIngredient);
        Task<bool> ConfirmOrder(string PurchaseOrderId, int idChecker, DateTime time, string status);
    }
}
