using BusinessAccessLayer.DTOs.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
     public interface IPurchaseOrderDetailService
    {
        Task<IEnumerable<PurchaseOrderDetailWithSupplierDTO>> GetByIngredientIdAsync(int ingredientId);
        Task<IEnumerable<SupplierComparisonDTO>> GetSupplierComparisonAsync(int ingredientId, string compareBy = "price");
    }
}
