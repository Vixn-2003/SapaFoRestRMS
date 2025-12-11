using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainAccessLayer.Models;

namespace DataAccessLayer.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface cho Counter Transaction History - UC124
    /// </summary>
    public interface ICounterTransactionRepository
    {
        /// <summary>
        /// Lấy danh sách transactions theo filter (có phân trang)
        /// </summary>
        Task<(IEnumerable<Transaction> Transactions, int TotalCount)> GetTransactionHistoryAsync(
            DateOnly? fromDate,
            DateOnly? toDate,
            string? paymentMethod,
            string? status,
            int pageNumber,
            int pageSize);

        /// <summary>
        /// Export transactions to list (không phân trang, dùng cho Excel)
        /// </summary>
        Task<IEnumerable<Transaction>> ExportTransactionsToListAsync(
            DateOnly? fromDate,
            DateOnly? toDate,
            string? paymentMethod,
            string? status);
    }
}

