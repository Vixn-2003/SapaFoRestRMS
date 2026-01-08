using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.CounterStaff;

namespace BusinessAccessLayer.Services.Interfaces
{
    /// <summary>
    /// Service interface cho Counter Transaction History - UC124
    /// </summary>
    public interface ICounterTransactionService
    {
        /// <summary>
        /// Lấy danh sách transaction history với filter và phân trang
        /// </summary>
        Task<TransactionHistoryListDto> GetTransactionHistoryAsync(TransactionFilterDto filter, CancellationToken ct = default);

        /// <summary>
        /// Export transactions to Excel file
        /// </summary>
        Task<byte[]> ExportTransactionsToExcelAsync(TransactionFilterDto filter, CancellationToken ct = default);
    }
}

