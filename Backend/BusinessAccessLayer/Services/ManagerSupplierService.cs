using AutoMapper;
using BusinessAccessLayer.DTOs.Inventory;
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
    public class ManagerSupplierService : IManagerSupplierService
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ManagerSupplierService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public Task<bool> AddRecipe(SupplierDTO dto)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteSupplierByMenuItemId(int idSupplier)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<SupplierDTO>> GetManagerAllSupplier()
        {
            var supplier = await _unitOfWork.Supplier.GetAllAsync();
            return _mapper.Map<IEnumerable<SupplierDTO>>(supplier);
        }

        public Task<SupplierDTO> ManagerSupplierById(int id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateSupplier(SupplierDTO updateSupplier)
        {
            throw new NotImplementedException();
        }
    }
}
