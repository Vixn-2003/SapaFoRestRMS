using AutoMapper;
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
        private readonly IMapper _mapper;

        public SystemLogoService(ISystemLogoRepository logoRepository, SapaFoRestRmsContext context, IMapper mapper)
        {
            _logoRepository = logoRepository;
            _context = context;
            _mapper = mapper;
        }

        public IEnumerable<SystemLogoDto> GetActiveLogos()
        {
            var logos = _logoRepository.GetActiveLogos();
            return _mapper.Map<IEnumerable<SystemLogoDto>>(logos);
        }

        public async Task<SystemLogoDto?> GetByIdAsync(int id)
        {
            var logo = await _logoRepository.GetByIdAsync(id);
            return _mapper.Map<SystemLogoDto>(logo);
        }

        public async Task<SystemLogoDto> AddLogoAsync(SystemLogoDto dto, int userId)
        {
            var logo = _mapper.Map<SystemLogo>(dto);
            logo.CreatedBy = userId;
            logo.CreatedDate = DateTime.Now;
            logo.IsActive = true;

            await _logoRepository.AddAsync(logo);
            await _context.SaveChangesAsync();

            return _mapper.Map<SystemLogoDto>(logo);
        }

        public async Task<bool> UpdateLogoAsync(SystemLogoDto dto, int userId)
        {
            var logo = await _logoRepository.GetByIdAsync(dto.LogoId);
            if (logo == null) return false;

            logo.LogoName = dto.LogoName;
            logo.LogoUrl = dto.LogoUrl;
            logo.Description = dto.Description;
            logo.IsActive = dto.IsActive;
            logo.UpdatedDate = DateTime.Now;
            logo.CreatedBy = userId;

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
