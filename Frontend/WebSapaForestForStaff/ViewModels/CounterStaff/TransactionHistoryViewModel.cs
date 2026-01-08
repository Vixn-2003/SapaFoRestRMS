using System;
using WebSapaForestForStaff.DTOs.CounterStaff;

namespace WebSapaForestForStaff.ViewModels.CounterStaff
{
    /// <summary>
    /// ViewModel cho Transaction History - UC124
    /// </summary>
    public class TransactionHistoryViewModel
    {
        public TransactionHistoryListDto TransactionList { get; set; } = new();
        public TransactionFilterDto Filter { get; set; } = new()
        {
            FromDate = DateOnly.FromDateTime(DateTime.Today),
            ToDate = DateOnly.FromDateTime(DateTime.Today),
            PageNumber = 1,
            PageSize = 20
        };
    }
}

