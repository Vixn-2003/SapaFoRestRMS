using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DomainAccessLayer.Models;

namespace DataAccessLayer.Repositories.Interfaces;

/// <summary>
/// Interface cho Shift Repository
/// </summary>
public interface IShiftRepository : IRepository<Shift>
{
    public interface IShiftRepository
    {
        Task<IEnumerable<Shift>> GetAllAsync();
        Task<Shift?> GetByIdAsync(int id);
        Task AddAsync(Shift shift);
        void Update(Shift shift);
        void Delete(Shift shift);
        Task<bool> SaveChangesAsync();
    /// <summary>
    /// Lấy ca làm việc đang mở (chưa kết thúc)
    /// </summary>
    Task<Shift?> GetCurrentOpenShiftAsync(int staffId, CancellationToken ct = default);

    /// <summary>
    /// Lấy ca làm việc theo ngày và staffId
    /// </summary>
    Task<IEnumerable<Shift>> GetShiftsByDateAndStaffAsync(DateOnly date, int staffId, CancellationToken ct = default);

    /// <summary>
    /// Lấy ca làm việc kèm thông tin staff
    /// </summary>
    Task<Shift?> GetShiftWithDetailsAsync(int shiftId, CancellationToken ct = default);

    /// <summary>
    /// Lấy tất cả ca làm việc đang mở (chưa kết ca)
    /// </summary>
    Task<IEnumerable<Shift>> GetAllOpenShiftsAsync(CancellationToken ct = default);

    /// <summary>
    /// Lấy lịch sử ca làm việc của staff
    /// </summary>
    Task<IEnumerable<Shift>> GetShiftHistoryAsync(int staffId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra xem staff có ca nào đang mở không
    /// </summary>
    Task<bool> HasOpenShiftAsync(int staffId, CancellationToken ct = default);

    /// <summary>
    /// Lấy doanh thu theo ca
    /// </summary>
    Task<decimal> GetShiftRevenueAsync(int shiftId, CancellationToken ct = default);

        Task<bool> IsConflictAsync(int departmentId, DateTime date, TimeSpan start, TimeSpan end, int? excludeId = null);
    /// <summary>
    /// Lấy số lượng đơn hàng theo ca
    /// </summary>
    Task<int> GetShiftOrderCountAsync(int shiftId, CancellationToken ct = default);
    }

}
