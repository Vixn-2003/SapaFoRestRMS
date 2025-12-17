using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IShiftCounterRepository : IRepository<Shift>
    {
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

        /// <summary>
        /// Lấy số lượng đơn hàng theo ca
        /// </summary>
        Task<int> GetShiftOrderCountAsync(int shiftId, CancellationToken ct = default);

        /// <summary>
        /// Thêm record vào ShiftHistory
        /// </summary>
        Task AddHistoryAsync(ShiftHistory history, CancellationToken ct = default);

        /// <summary>
        /// Lấy danh sách history của một shift
        /// </summary>
        Task<List<ShiftHistory>> GetShiftHistoriesAsync(int shiftId, CancellationToken ct = default);
    }
}
