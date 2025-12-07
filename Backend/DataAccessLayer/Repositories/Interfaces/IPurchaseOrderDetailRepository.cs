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

        Task<IEnumerable<PurchaseOrderDetail>> GetByIngredientIdAsync(int ingredientId);
        Task<IEnumerable<PurchaseOrderDetail>> GetByIngredientIdWithDetailsAsync(int ingredientId);
    }
}
