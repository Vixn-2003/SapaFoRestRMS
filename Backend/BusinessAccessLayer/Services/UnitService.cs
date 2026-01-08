using AutoMapper;
using BusinessAccessLayer.DTOs.Inventory;
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
    public class UnitService : IUnitService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UnitService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<UnitDTO>> GetAllUnits()
        {
            var result = await _unitOfWork.UnitRepository.GetAllUnits();
            return _mapper.Map<IEnumerable<UnitDTO>>(result);
        }

        public async Task<int> getIdUnitByString(string unitName)
        {
            var result = await _unitOfWork.UnitRepository.GetIdUnitByString(unitName);
            return result;
        }


        public async Task<UnitDTO> CreateAsync(UnitDTO dto)
        {
            if (await _unitOfWork.UnitRepository.ExistsByNameAsync(dto.UnitName))
                throw new InvalidOperationException("Unit name already exists");

            // 🔁 Mapping DTO → Entity
            var unit = new Unit
            {
                UnitName = dto.UnitName,
                UnitType = dto.UnitType
            };

            await _unitOfWork.UnitRepository.AddAsync(unit);

            // 🔁 Mapping Entity → DTO
            return new UnitDTO
            {
                UnitId = unit.UnitId,
                UnitName = unit.UnitName,
                UnitType = unit.UnitType
            };
        }

        // ============ UPDATE ============
        public async Task UpdateAsync(int id, UnitDTO dto)
        {
            var unit = await _unitOfWork.UnitRepository.GetByIdAsync(id);
            if (unit == null)
                throw new InvalidOperationException("Không tìm thấy đơn vị tính");

            if (await _unitOfWork.UnitRepository.ExistsByNameAsync(dto.UnitName, id))
                throw new InvalidOperationException("Đơn vị tính đã tồn tại");

            // 🔁 Mapping DTO → Entity
            unit.UnitName = dto.UnitName;
            unit.UnitType = dto.UnitType;

            await _unitOfWork.UnitRepository.UpdateAsync(unit);
        }
    }
}
