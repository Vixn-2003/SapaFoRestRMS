using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IPurchaseOrderDetailRepository
    {
        Task<IEnumerable<PurchaseOrderDetail>> GetPurchaseOrderDetails(string purchaseOrderId);
        Task<bool> AddIdNewIngredient(int idDetailOrder, int idIngredient);
    }
}
