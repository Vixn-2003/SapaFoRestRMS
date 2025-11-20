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
    public class WarehouseService : IWarehouseService
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public WarehouseService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<WarehouseDTO>> GetAllWarehouse()
        {
            var warehouse = await _unitOfWork.Warehouse.GetAllAsync();
            return _mapper.Map<IEnumerable<WarehouseDTO>>(warehouse);
        }

        public async Task<WarehouseDTO> GetWarehouseById(int id)
        {
            var warehouse = await _unitOfWork.Warehouse.GetByIdAsync(id);
            return _mapper.Map<WarehouseDTO>(warehouse);
        }

        public async Task<int> GetWarehouseByString(string warehouses)
        {
            var warehouse = await _unitOfWork.Warehouse.GetIdByStringAsync(warehouses);
            return warehouse;
        }
    }
}
