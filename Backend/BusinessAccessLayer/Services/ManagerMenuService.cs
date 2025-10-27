using AutoMapper;
using AutoMapper.Configuration.Annotations;
using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class ManagerMenuService : IManagerMenuService
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ManagerMenuService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<bool> AddRecipe(RecipeDTO dto)
        {
            var replaceDto = _mapper.Map<Recipe>(dto);
            var menu = await _unitOfWork.MenuItem.AddRecipe(replaceDto);
            return menu;
        }

        public async Task<bool> DeleteRecipeByMenuItemId(int menuItemId)
        {
            var menu = await _unitOfWork.MenuItem.DeleteRecipeByMenuItemId(menuItemId);
            return menu;
        }

        public async Task<IEnumerable<ManagerMenuDTO>> GetManagerAllMenu()
        {
            var menu = await _unitOfWork.MenuItem.GetManagerAllMenus();
            return _mapper.Map<IEnumerable<ManagerMenuDTO>>(menu);
        }

        public async Task<IEnumerable<RecipeDTO>> GetRecipeByMenuItem(int id)
        {
            var recipe = await _unitOfWork.MenuItem.GetRecipeByMenuItem(id);
            return _mapper.Map<IEnumerable<RecipeDTO>>(recipe);
        }

        public async Task<ManagerMenuDTO> ManagerMenuById(int id)
        {
            var menu = await _unitOfWork.MenuItem.ManagerMenuByIds(id);
            return _mapper.Map<ManagerMenuDTO>(menu);
        }

        public async Task<bool> UpdateManagerMenu(ManagerMenuDTO formUpdateMenuDTO)
        {
            if (formUpdateMenuDTO == null)
                throw new ArgumentNullException(nameof(formUpdateMenuDTO), "Dữ liệu cập nhật không được để trống.");

            try
            {
                var mapping = _mapper.Map<MenuItem>(formUpdateMenuDTO);
                var result = await _unitOfWork.MenuItem.ManagerUpdateMenu(mapping);

                return result; // trả về kết quả thực tế (true/false)
            }
            catch (AutoMapperMappingException mapEx)
            {
                // Lỗi trong quá trình ánh xạ (mapping)
                Console.Error.WriteLine($"[Mapping Error] {mapEx.Message}");
                return false;
            }
            catch (DbUpdateException dbEx)
            {
                // Lỗi khi cập nhật cơ sở dữ liệu
                Console.Error.WriteLine($"[Database Error] {dbEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // Lỗi không xác định
                Console.Error.WriteLine($"[Unexpected Error] {ex.Message}");
                return false;
            }
        }



    }
}
