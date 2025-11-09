using AutoMapper;
using BusinessAccessLayer.DTOs.Users;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IMapper _mapper;

        public RoleService(IRoleRepository roleRepository, IMapper mapper)
        {
            _roleRepository = roleRepository;
            _mapper = mapper;
        }

        public async Task<List<RoleDto>> GetAllAsync(CancellationToken ct = default)
        {
            var roles = await _roleRepository.GetAllOrderedAsync(ct);
            return _mapper.Map<List<RoleDto>>(roles);
        }

        public async Task<RoleDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null)
            {
                return null;
            }

            return _mapper.Map<RoleDto>(role);
        }
    }
}

