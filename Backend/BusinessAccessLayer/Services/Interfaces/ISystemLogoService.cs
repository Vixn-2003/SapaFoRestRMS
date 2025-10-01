using BusinessAccessLayer.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface ISystemLogoService
    {
        IEnumerable<SystemLogoDto> GetAllLogos();
        IEnumerable<SystemLogoDto> GetActiveLogos();
        Task<SystemLogoDto?> GetByIdAsync(int id);
        Task<SystemLogoDto> AddLogoAsync(SystemLogoDto dto, int userId);
        Task<bool> UpdateLogoAsync(SystemLogoDto dto, int userId);
        Task<bool> DeleteLogoAsync(int id);
    }
}
