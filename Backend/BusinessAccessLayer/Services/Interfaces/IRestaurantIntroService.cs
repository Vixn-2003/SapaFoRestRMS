using BusinessAccessLayer.DTOs.RestaurantIntroDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IRestaurantIntroService
    {
        Task<IEnumerable<RestaurantIntroDto>> GetAllAsync();
        Task<IEnumerable<RestaurantIntroDto>> GetActiveAsync();
        Task<RestaurantIntroDto?> GetByIdAsync(int id);
        Task<RestaurantIntroDto> CreateAsync(CreateRestaurantIntroDto dto);
        Task<bool> UpdateAsync(int id, UpdateRestaurantIntroDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
