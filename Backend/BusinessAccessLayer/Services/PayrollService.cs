using AutoMapper;
using BusinessAccessLayer.DTOs;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class PayrollService : IPayrollService
    {
        private readonly IPayrollRepository _payrollRepository;
        private readonly IMapper _mapper;

        public PayrollService(IPayrollRepository payrollRepository, IMapper mapper)
        {
            _payrollRepository = payrollRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PayrollDTO>> GetAllAsync()
        {
            var entities = await _payrollRepository.GetAllAsync();
            return entities.Select(MapToDTO);
        }

        public async Task<PayrollDTO?> GetByIdAsync(int id)
        {
            var entity = await _payrollRepository.GetByIdAsync(id);
            return entity == null ? null : MapToDTO(entity);
        }

        public async Task AddAsync(PayrollDTO dto)
        {
            ValidatePayrollDTO(dto);

            var exists = await _payrollRepository
                .AnyAsync(x => x.StaffId == dto.StaffId && x.MonthYear == dto.MonthYear);

            if (exists)
                throw new InvalidOperationException("Bảng lương đã tồn tại cho nhân viên trong tháng này.");

            var entity = new Payroll
            {
                StaffId = dto.StaffId,
                MonthYear = dto.MonthYear,
                BaseSalary = dto.BaseSalary,
                TotalWorkDays = dto.TotalWorkDays,
                TotalBonus = dto.TotalBonus,
                TotalPenalty = dto.TotalPenalty,
                NetSalary = dto.BaseSalary + dto.TotalBonus - dto.TotalPenalty,
                Status = string.IsNullOrEmpty(dto.Status) ? PayrollStatus.Draft : dto.Status
            };

            await _payrollRepository.AddAsync(entity);
            await _payrollRepository.SaveChangesAsync();
        }

        public async Task UpdateAsync(PayrollDTO dto)
        {
            ValidatePayrollDTO(dto);

            var entity = await _payrollRepository.GetByIdAsync(dto.PayrollId);
            if (entity == null)
                throw new KeyNotFoundException("Bảng lương không tồn tại.");

            // Chỉ cho phép sửa khi trạng thái là Draft hoặc Rejected
            if (entity.Status != PayrollStatus.Draft && entity.Status != PayrollStatus.Rejected)
                throw new InvalidOperationException("Chỉ được sửa bảng lương ở trạng thái mới tạo hoặc bị từ chối.");

            var exists = await _payrollRepository.AnyAsync(x =>
                x.StaffId == dto.StaffId &&
                x.MonthYear == dto.MonthYear &&
                x.PayrollId != dto.PayrollId
            );
            if (exists)
                throw new InvalidOperationException("Đã có bảng lương cho nhân viên này trong tháng đã chọn.");

            entity.StaffId = dto.StaffId;
            entity.MonthYear = dto.MonthYear;
            entity.BaseSalary = dto.BaseSalary;
            entity.TotalWorkDays = dto.TotalWorkDays;
            entity.TotalBonus = dto.TotalBonus;
            entity.TotalPenalty = dto.TotalPenalty;
            entity.NetSalary = dto.BaseSalary + dto.TotalBonus - dto.TotalPenalty;
            entity.Status = string.IsNullOrEmpty(dto.Status) ? entity.Status : dto.Status;

            await _payrollRepository.UpdateAsync(entity);
            await _payrollRepository.SaveChangesAsync();
        }




        private void ValidatePayrollDTO(PayrollDTO dto)
        {
            if (dto.StaffId <= 0)
                throw new ArgumentException("StaffId phải là số nguyên dương.");

            if (string.IsNullOrEmpty(dto.MonthYear) || dto.MonthYear.Length != 6)
                throw new ArgumentException("MonthYear phải có định dạng 'yyyyMM'.");

            if (dto.BaseSalary < 0)
                throw new ArgumentException("BaseSalary không được nhỏ hơn 0.");

            if (dto.TotalBonus < 0)
                throw new ArgumentException("TotalBonus không được nhỏ hơn 0.");

            if (dto.TotalPenalty < 0)
                throw new ArgumentException("TotalPenalty không được nhỏ hơn 0.");

            if (dto.TotalWorkDays < 0 || dto.TotalWorkDays > 31)
                throw new ArgumentException("TotalWorkDays phải trong khoảng từ 0 đến 31.");
        }



        public async Task DeleteAsync(int id)
        {
            await _payrollRepository.DeleteAsync(id);
            await _payrollRepository.SaveChangesAsync();
        }

        public async Task<(IEnumerable<PayrollDTO> Data, int TotalCount)> SearchFilterSortPagedAsync(
     int pageNumber,
     int pageSize,
     string? staffName = null,
     string? sortBy = null,
     bool descending = false,
     decimal? minBaseSalary = null,
     decimal? maxBaseSalary = null,
     int? minWorkDays = null,
     int? maxWorkDays = null,
     decimal? minBonus = null,
     decimal? maxBonus = null,
     decimal? minPenalty = null,
     decimal? maxPenalty = null,
     decimal? minNetSalary = null,
     decimal? maxNetSalary = null,
     string? monthYear = null)
        {
            var (entities, totalCount) = await _payrollRepository.SearchFilterPagedAsync(
                pageNumber,
                pageSize,
                staffName,
                sortBy,
                descending,
                minBaseSalary,
                maxBaseSalary,
                minWorkDays,
                maxWorkDays,
                minBonus,
                maxBonus,
                minPenalty,
                maxPenalty,
                minNetSalary,
                maxNetSalary,
                monthYear);

            var dtos = entities.Select(MapToDTO);
            return (dtos, totalCount);
        }


        private PayrollDTO MapToDTO(Payroll entity)
        {
            return new PayrollDTO
            {
                PayrollId = entity.PayrollId,
                StaffId = entity.StaffId,
                StaffName = entity.Staff?.User.FullName ?? string.Empty,
                MonthYear = entity.MonthYear,
                BaseSalary = entity.BaseSalary,
                TotalWorkDays = entity.TotalWorkDays,
                TotalBonus = entity.TotalBonus,
                TotalPenalty = entity.TotalPenalty,
                NetSalary = entity.NetSalary,
                Status = entity.Status
            };
        }
    }
}
