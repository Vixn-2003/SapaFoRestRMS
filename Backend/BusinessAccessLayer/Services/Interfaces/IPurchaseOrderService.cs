using Azure.Core;
using BusinessAccessLayer.DTOs.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IPurchaseOrderService
    {
        Task<IEnumerable<PurchaseOrderDTO>> GetAll();
        Task<PurchaseOrderDTO> GetPurchaseOrderById( string id);
        Task<bool> CreateImportOrderAsync(ImportOrder importOrder, List<ImportDetail> importDetails);
        Task<bool> AddIdNewIngredient(int idDetailOrder,int idIngredient);
        Task<bool> ConfirmOrder(string PurchaseOrderId, int idChecker, DateTime time, string status);
    }
}
