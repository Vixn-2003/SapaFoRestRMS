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
    }
}
