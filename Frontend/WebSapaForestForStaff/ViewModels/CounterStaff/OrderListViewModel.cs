using System;
using System.Collections.Generic;
using WebSapaForestForStaff.DTOs.CounterStaff;

namespace WebSapaForestForStaff.ViewModels.CounterStaff
{
    /// <summary>
    /// ViewModel cho Order List - UC123
    /// </summary>
    public class OrderListViewModel
    {
        public List<OrderListItemDto> Orders { get; set; } = new();
        public DateOnly SelectedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public string? SelectedStatus { get; set; }
        public string? SearchKeyword { get; set; }
    }
}

