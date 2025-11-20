using BusinessAccessLayer.DTOs.Manager;
using DomainAccessLayer.Models;
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

        Task<bool> UpdateManagerMenu(ManagerMenuDTO formUpdateMenuDTO);

        Task<IEnumerable<RecipeDTO>> GetRecipeByMenuItem(int id);


        Task<bool> DeleteRecipeByMenuItemId(int menuItemId);

        Task<bool> AddRecipe(RecipeDTO dto);


    }
}
