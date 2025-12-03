using BusinessAccessLayer.DTOs.ShiftAssignment;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class ShiftAssignmentService : IShiftAssignmentService
    {
        private readonly IShiftAssignmentRepository _repo;
        private readonly SapaFoRestRmsContext _context;

        public ShiftAssignmentService(IShiftAssignmentRepository repo, SapaFoRestRmsContext context)
        {
            _repo = repo;
            _context = context;
        }

        public async Task<IEnumerable<ShiftAssignmentViewDTO>> GetAllAsync()
        {
            var data = await _repo.GetAllAsync();
            return data.Select(x => new ShiftAssignmentViewDTO
            {
                Id = x.Id,
                ShiftId = x.ShiftId,
                ShiftCode = x.Shift.Code,
                ShiftDate = x.Shift.Date,
                StartTime = x.Shift.StartTime,
                EndTime = x.Shift.EndTime,
                StaffId = x.StaffId,
                StaffName = x.Staff.User.FullName,
                DepartmentId = x.Shift.DepartmentId,
                DepartmentName = x.Shift.Department.Name
            });
        }

        public async Task<ShiftAssignmentViewDTO?> GetByIdAsync(int id)
        {
            var x = await _repo.GetByIdAsync(id);
            if (x == null) return null;
            return new ShiftAssignmentViewDTO
            {
                Id = x.Id,
                ShiftId = x.ShiftId,
                ShiftCode = x.Shift.Code,
                ShiftDate = x.Shift.Date,
                StartTime = x.Shift.StartTime,
                EndTime = x.Shift.EndTime,
                StaffId = x.StaffId,
                StaffName = x.Staff.User.FullName,
                DepartmentId = x.Shift.DepartmentId,
                DepartmentName = x.Shift.Department.Name
            };
        }

        public async Task<ShiftAssignmentViewDTO> CreateAsync(CreateShiftAssignmentDTO dto)
        {
            var shift = await _context.Shifts.FindAsync(dto.ShiftId);
            if (shift == null) throw new Exception("Shift không tồn tại");

            var staff = await _context.Staffs.Include(s => s.User).FirstOrDefaultAsync(s => s.StaffId == dto.StaffId);
            if (staff == null) throw new Exception("Staff không tồn tại");

            bool conflict = await _repo.IsConflictAsync(staff.StaffId, shift.Date, shift.StartTime, shift.EndTime);
            if (conflict) throw new Exception("Nhân viên đã được phân công ca khác trùng giờ");

            var assignment = new ShiftAssignment
            {
                ShiftId = dto.ShiftId,
                StaffId = dto.StaffId
            };

            await _repo.AddAsync(assignment);
            await _repo.SaveChangesAsync();

            return await GetByIdAsync(assignment.Id);
        }

        public async Task<ShiftAssignmentViewDTO> UpdateAsync(int id, UpdateShiftAssignmentDTO dto)
        {
            var assignment = await _repo.GetByIdAsync(id);
            if (assignment == null) throw new Exception("Không tìm thấy phân công");

            var shift = await _context.Shifts.FindAsync(dto.ShiftId);
            if (shift == null) throw new Exception("Shift không tồn tại");

            var staff = await _context.Staffs.Include(s => s.User).FirstOrDefaultAsync(s => s.StaffId == dto.StaffId);
            if (staff == null) throw new Exception("Staff không tồn tại");

            bool conflict = await _repo.IsConflictAsync(staff.StaffId, shift.Date, shift.StartTime, shift.EndTime, assignment.Id);
            if (conflict) throw new Exception("Nhân viên đã được phân công ca khác trùng giờ");

            assignment.ShiftId = dto.ShiftId;
            assignment.StaffId = dto.StaffId;

            _repo.Update(assignment);
            await _repo.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var assignment = await _repo.GetByIdAsync(id);
            if (assignment == null) return false;

            _repo.Delete(assignment);
            return await _repo.SaveChangesAsync();
        }
    }

}
