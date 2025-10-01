using BusinessAccessLayer.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IMenuService
    {
        Task<IEnumerable<MenuDTO>> GetAllMenu();
       
    }
}
