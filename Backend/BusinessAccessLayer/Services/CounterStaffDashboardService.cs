using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.CounterStaff;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;

namespace BusinessAccessLayer.Services
{
    /// <summary>
    /// Service implementation cho Counter Staff Dashboard - UC122
    /// </summary>
    public class CounterStaffDashboardService : ICounterStaffDashboardService
    {
        private readonly ICounterStaffDashboardRepository _dashboardRepository;

        public CounterStaffDashboardService(ICounterStaffDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<CounterStaffDashboardDto> GetDashboardDataAsync(CancellationToken ct = default)
        {
            // Lấy các KPI
            var todayReservations = await _dashboardRepository.GetTodayReservationCountAsync();
            var todayRevenue = await _dashboardRepository.GetTodayRevenueAsync();
            var activeOrders = await _dashboardRepository.GetActiveOrdersCountAsync();
            var pendingPayments = await _dashboardRepository.GetPendingPaymentOrdersAsync();
            var activeTables = await _dashboardRepository.GetActiveTablesCountAsync();
            var completedTransactions = await _dashboardRepository.GetTransactionCountAsync();

            // Lấy chart data
            var hourlyRevenue = await _dashboardRepository.GetHourlyRevenueChartAsync();
            var hourlyOrders = await _dashboardRepository.GetHourlyOrdersChartAsync();

            // Build dashboard DTO
            var dashboard = new CounterStaffDashboardDto
            {
                TodayReservations = todayReservations,
                TodayRevenue = todayRevenue,
                ActiveOrders = activeOrders,
                PendingPayments = pendingPayments,
                ActiveTables = activeTables,
                CompletedTransactions = completedTransactions,
                RevenueChart = hourlyRevenue.Select(x => new HourlyRevenuePoint
                {
                    Hour = x.Key,
                    Revenue = x.Value
                }).OrderBy(x => x.Hour).ToList(),
                OrderChart = hourlyOrders.Select(x => new HourlyOrderPoint
                {
                    Hour = x.Key,
                    OrderCount = x.Value
                }).OrderBy(x => x.Hour).ToList()
            };

            return dashboard;
        }
    }
}

