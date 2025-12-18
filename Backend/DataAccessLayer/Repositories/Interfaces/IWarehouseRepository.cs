using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IWarehouseRepository : IRepository<Warehouse>
    {
        Task<int> GetIdByStringAsync(string warehouse);
        Task<bool> UpdateWarehouseAsync(Warehouse warehouse);
        Task<bool> AddWarehouseAsync(Warehouse warehouse);
        Task<bool> DeleteWarehousesAsync(int id);

        Task<IEnumerable<InventoryBatch>> GetBatchesByWarehouseIdAsync(int warehouseId);
    }
}
