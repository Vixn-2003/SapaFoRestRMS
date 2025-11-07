using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.DTOs.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IManagerSupplierService
    {
        Task<IEnumerable<SupplierDTO>> GetManagerAllSupplier();

        Task<SupplierDTO> ManagerSupplierById(int id);

        Task<bool> UpdateSupplier(SupplierDTO updateSupplier);

        Task<bool> DeleteSupplierByMenuItemId(int idSupplier);

        Task<bool> AddRecipe(SupplierDTO dto);
    }
}
