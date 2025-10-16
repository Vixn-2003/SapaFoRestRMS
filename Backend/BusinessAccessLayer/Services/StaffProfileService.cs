using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.UserManagement;
using AutoMapper;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.UnitOfWork.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BusinessAccessLayer.Services
{
    public class StaffProfileService : IStaffProfileService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public StaffProfileService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<List<StaffProfileDto>> GetAllAsync(CancellationToken ct = default)
        {
            var staffUsers = await _uow.StaffProfiles.GetAllWithDetailsAsync(ct);

            return staffUsers.Select(u => _mapper.Map<StaffProfileDto>(u)).ToList();
        }

        public async Task<StaffProfileDto?> GetAsync(int userId, CancellationToken ct = default)
        {
            var user = await _uow.StaffProfiles.GetWithDetailsAsync(userId, ct);
            if (user == null) return null;

            return _mapper.Map<StaffProfileDto>(user);
        }

        public async Task UpdateAsync(int userId, StaffProfileUpdateDto update, CancellationToken ct = default)
        {
            var user = await _uow.StaffProfiles.GetWithDetailsAsync(userId, ct);
            if (user == null) throw new KeyNotFoundException("User not found");

            user.FullName = update.FullName;
            user.Email = update.Email;
            user.Phone = update.Phone;
            user.Status = update.Status;

            if (update.PositionIds != null)
            {
                var positions = await _uow.Positions.GetByIdsAsync(update.PositionIds, ct);

                var staff = user.Staff.FirstOrDefault();
                if (staff == null)
                {
                    staff = new DomainAccessLayer.Models.Staff
                    {
                        UserId = user.UserId,
                        HireDate = DateOnly.FromDateTime(System.DateTime.UtcNow),
                        SalaryBase = 0,
                        Status = 0
                    };
                    // tracked by context behind UnitOfWork; save after update
                }

                staff.Positions.Clear();
                foreach (var pos in positions)
                {
                    staff.Positions.Add(pos);
                }
            }
            await _uow.SaveChangesAsync();
        }
    }
}


