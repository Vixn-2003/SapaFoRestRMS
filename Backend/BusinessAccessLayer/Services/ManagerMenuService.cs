using AutoMapper;
using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
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
        public async Task<IEnumerable<ManagerMenuDTO>> GetManagerAllMenu()
        {
            var menu = await _unitOfWork.MenuItem.GetManagerAllMenus();
            return _mapper.Map<IEnumerable<ManagerMenuDTO>>(menu);
        }

        public async Task<ManagerMenuDTO> ManagerMenuById(int id)
        {
            var menu = await _unitOfWork.MenuItem.ManagerMenuByIds(id);
            return _mapper.Map<ManagerMenuDTO>(menu);
        }
    }
}
