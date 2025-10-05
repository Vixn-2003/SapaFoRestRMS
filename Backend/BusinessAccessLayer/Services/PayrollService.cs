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
            var entity = new Payroll
            {
                StaffId = dto.StaffId,
                MonthYear = dto.MonthYear,
                BaseSalary = dto.BaseSalary,
                TotalWorkDays = dto.TotalWorkDays,
                TotalBonus = dto.TotalBonus,
                TotalPenalty = dto.TotalPenalty,
                NetSalary = dto.NetSalary,
                Status = dto.Status
            };

            await _payrollRepository.AddAsync(entity);
            await _payrollRepository.SaveChangesAsync();
        }

        public async Task UpdateAsync(PayrollDTO dto)
        {
            var entity = await _payrollRepository.GetByIdAsync(dto.PayrollId);
            if (entity == null) return;

            entity.StaffId = dto.StaffId;
            entity.MonthYear = dto.MonthYear;
            entity.BaseSalary = dto.BaseSalary;
            entity.TotalWorkDays = dto.TotalWorkDays;
            entity.TotalBonus = dto.TotalBonus;
            entity.TotalPenalty = dto.TotalPenalty;
            entity.NetSalary = dto.NetSalary;
            entity.Status = dto.Status;

            await _payrollRepository.UpdateAsync(entity);
            await _payrollRepository.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await _payrollRepository.DeleteAsync(id);
            await _payrollRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<PayrollDTO>> SearchAsync(string staffName)
        {
            var results = await _payrollRepository.SearchAsync(staffName);
            return results.Select(MapToDTO);
        }

        public async Task<IEnumerable<PayrollDTO>> FilterAsync(
            string? sortBy,
            bool descending,
            decimal? minBaseSalary,
            decimal? maxBaseSalary,
            int? minWorkDays,
            int? maxWorkDays,
            decimal? minBonus,
            decimal? maxBonus,
            decimal? minPenalty,
            decimal? maxPenalty,
            decimal? minNetSalary,
            decimal? maxNetSalary,
            string? monthYear)
        {
            var results = await _payrollRepository.FilterAsync(
                sortBy, descending,
                minBaseSalary, maxBaseSalary,
                minWorkDays, maxWorkDays,
                minBonus, maxBonus,
                minPenalty, maxPenalty,
                minNetSalary, maxNetSalary,
                monthYear);

            return results.Select(MapToDTO);
        }

        public async Task<(IEnumerable<PayrollDTO> Data, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? staffName,
            string? sortBy,
            bool descending)
        {
            var (data, total) = await _payrollRepository.GetPagedAsync(pageNumber, pageSize, staffName, sortBy, descending);
            return (data.Select(MapToDTO), total);
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
