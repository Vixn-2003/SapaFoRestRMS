using BusinessAccessLayer.DTOs.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IManagerCategoryService
    {

        Task<IEnumerable<ManagerCategoryDTO>> GetAllCategory();
    }
}
