using AutoMapper;
using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class MenuService : IMenuService
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public MenuService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<IEnumerable<MenuDTO>> GetAllMenu()
        {
            var menu = await _unitOfWork.MenuItem.GetAllMenus();
            return _mapper.Map<IEnumerable<MenuDTO>>(menu);
        }
    }
}
