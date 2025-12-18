using AutoMapper;
using BusinessAccessLayer.DTOs.Inventory;
using BusinessAccessLayer.DTOs.Manager;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using DomainAccessLayer.Models;
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

        public async Task<IEnumerable<InventoryBatchDTO>> GetBatchesByWarehouseAsync(int warehouseId)
        {
            var batches = await _unitOfWork.Warehouse.GetBatchesByWarehouseIdAsync(warehouseId);
            return _mapper.Map<IEnumerable<InventoryBatchDTO>>(batches);
        }

        public async Task<bool> UpdateWarehouseAsync(int id, WarehouseDTO dto)
        {
            var warehouse = await _unitOfWork.Warehouse.GetByIdAsync(id);
            if (warehouse != null) {
                warehouse.Name = dto.Name;
                var result = await _unitOfWork.Warehouse.UpdateWarehouseAsync(warehouse);
                return result;  
            }
            return false;
        }

        public async Task<bool> CreateWarehouseAsync(WarehouseDTO dto)
        {
            var warehouse = new Warehouse
            {
                Name = dto.Name.Trim()
            };

            var result = await _unitOfWork.Warehouse.AddWarehouseAsync(warehouse);

            return result;
        }

        public async Task<bool> DeleteWarehouseAsync(int id)
        {
            var result = await _unitOfWork.Warehouse.DeleteWarehousesAsync(id);
            return result;
        }
    }
}
    