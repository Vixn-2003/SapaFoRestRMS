﻿using AutoMapper;
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
    public class ComboService : IComboService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ComboService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<IEnumerable<ComboDTO>> GetAllCombo()
        {
            var combo = await _unitOfWork.Combo.GetAllCombos();
            return _mapper.Map<IEnumerable<ComboDTO>>(combo);
        }

    }
}
