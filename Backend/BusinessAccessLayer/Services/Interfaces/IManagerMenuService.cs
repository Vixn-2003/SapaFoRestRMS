using BusinessAccessLayer.DTOs.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IManagerMenuService
    {
        Task<IEnumerable<ManagerMenuDTO>> GetManagerAllMenu();
        Task<ManagerMenuDTO> ManagerMenuById(int id);
       
    }
}
