using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.DTOs.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IWarehouseService
    {
        Task<IEnumerable<WarehouseDTO>> GetAllWarehouse();
        Task<WarehouseDTO> GetWarehouseById(int id);
        
    }
}
