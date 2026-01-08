using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface ISupplierRepository
    {
        // Lấy tất cả Suppliers (base)
        Task<List<Supplier>> GetAllSuppliersAsync();

        // Lấy PurchaseOrders của một Supplier (có Include chi tiết)
        // Trả về List<PurchaseOrder>
        Task<List<PurchaseOrder>> GetSupplierPurchaseOrdersAsync(int supplierId);

        // Lấy IQueryable để Service tiếp tục GroupBy và tính toán trên DB
        // Trả về IQueryable<PurchaseOrderDetail>
        Task<IQueryable<PurchaseOrderDetail>> GetSupplierOrderDetailsQuery(int supplierId);
        Task<bool> DeleteSoftAsync(int supplierId);


    }
}
