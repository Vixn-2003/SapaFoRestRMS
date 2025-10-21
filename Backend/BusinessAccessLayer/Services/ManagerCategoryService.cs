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
    public class ManagerCategoryService : IManagerCategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ManagerCategoryService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<IEnumerable<ManagerCategoryDTO>> GetAllCategory()
        {
            var category = await _unitOfWork.MenuCategory.GetManagerAllCategory();
            return _mapper.Map<IEnumerable<ManagerCategoryDTO>>(category);
        }
    }
}
