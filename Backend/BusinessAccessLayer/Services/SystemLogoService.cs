using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class SystemLogoService : ISystemLogoService
    {
        private readonly ISystemLogoRepository _logoRepository;
        private readonly SapaFoRestRmsContext _context;

        public SystemLogoService(ISystemLogoRepository logoRepository, SapaFoRestRmsContext context)
        {
            _logoRepository = logoRepository;
            _context = context;
        }
        public IEnumerable<SystemLogoDto> GetAllLogos()
        {
            var logos = _logoRepository.GetAll();
            var result = new List<SystemLogoDto>();

            foreach (var logo in logos)
            {
                result.Add(new SystemLogoDto
                {
                    LogoId = logo.LogoId,
                    LogoName = logo.LogoName,
                    LogoUrl = logo.LogoUrl,
                    Description = logo.Description,
                    IsActive = logo.IsActive,
                    CreatedBy = logo.CreatedBy,
                    CreatedByName = logo.CreatedByNavigation?.FullName ?? logo.CreatedByNavigation?.FullName // fallback
                });
            }

            return result;
        }


        public IEnumerable<SystemLogoDto> GetActiveLogos()
        {
            var logos = _logoRepository.GetActiveLogos();
            var result = new List<SystemLogoDto>();

            foreach (var logo in logos)
            {
                result.Add(new SystemLogoDto
                {
                    LogoId = logo.LogoId,
                    LogoName = logo.LogoName,
                    LogoUrl = logo.LogoUrl,
                    Description = logo.Description,
                    IsActive = logo.IsActive,
                    CreatedBy = logo.CreatedBy,
                    CreatedByName = logo.CreatedByNavigation?.FullName ?? logo.CreatedByNavigation?.FullName
                });
            }

            return result;
        }

        public async Task<SystemLogoDto?> GetByIdAsync(int id)
        {
            var logo = await _logoRepository.GetByIdAsync(id);
            if (logo == null) return null;

            return new SystemLogoDto
            {
                LogoId = logo.LogoId,
                LogoName = logo.LogoName,
                LogoUrl = logo.LogoUrl,
                Description = logo.Description,
                IsActive = logo.IsActive,
                CreatedBy = logo.CreatedBy,
                CreatedByName = logo.CreatedByNavigation?.FullName ?? logo.CreatedByNavigation?.FullName
            };
        }


        public async Task<SystemLogoDto> AddLogoAsync(SystemLogoDto dto, int userId)
        {
            var logo = new SystemLogo
            {
                LogoName = dto.LogoName,
                LogoUrl = dto.LogoUrl,
                Description = dto.Description,
                IsActive = dto.IsActive,
                CreatedBy = userId,
                CreatedDate = DateTime.Now
            };

            await _logoRepository.AddAsync(logo);
            await _context.SaveChangesAsync();

            dto.LogoId = logo.LogoId; // trả về id vừa tạo
            return dto;
        }

        public async Task<bool> UpdateLogoAsync(SystemLogoDto dto, int userId)
        {
            var logo = await _logoRepository.GetByIdAsync(dto.LogoId);
            if (logo == null)
                return false;

            logo.LogoName = dto.LogoName;
            logo.Description = dto.Description;
            logo.IsActive = dto.IsActive;
            logo.UpdatedDate = DateTime.Now;
            logo.CreatedBy = userId;

            // Nếu có file upload mới thì cập nhật URL
            if (!string.IsNullOrEmpty(dto.LogoUrl))
            {
                logo.LogoUrl = dto.LogoUrl;
            }

            _logoRepository.Update(logo);
            await _context.SaveChangesAsync();
            return true;
        }



        public async Task<bool> DeleteLogoAsync(int id)
        {
            var logo = await _logoRepository.GetByIdAsync(id);
            if (logo == null) return false;

            _logoRepository.Delete(logo);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
