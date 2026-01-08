/**
 * Owner Dashboard Charts Module
 * Renders all charts for Owner Dashboard using Chart.js
 * Data is provided by Razor ViewModel (no fetch API)
 */

(function() {
    'use strict';

    // Wait for DOM to be ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initCharts);
    } else {
        initCharts();
    }

    function initCharts() {
        if (typeof dashboardData === 'undefined') {
            console.error('Dashboard data not found');
            return;
        }

        renderRevenueTrendChart();
        renderAlertsDonutChart();
        renderTopSellingChart();
        renderBranchComparisonChart();
    }

    /**
     * Revenue Trend Line Chart
     */
    function renderRevenueTrendChart() {
        const ctx = document.getElementById('revenueTrendChart');
        if (!ctx) return;

        const data = dashboardData.revenueTrend || [];
        
        new Chart(ctx, {
            type: 'line',
            data: {
                labels: data.map(d => d.date),
                datasets: [{
                    label: 'Doanh Thu (đ)',
                    data: data.map(d => d.revenue),
                    borderColor: 'rgb(75, 192, 192)',
                    backgroundColor: 'rgba(75, 192, 192, 0.2)',
                    tension: 0.4,
                    fill: true
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: true,
                        position: 'top'
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                return 'Doanh thu: ' + formatCurrency(context.parsed.y);
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function(value) {
                                return formatCurrency(value);
                            }
                        }
                    }
                }
            }
        });
    }

    /**
     * Alerts Donut Chart
     */
    function renderAlertsDonutChart() {
        const ctx = document.getElementById('alertsDonutChart');
        if (!ctx) return;

        const alerts = dashboardData.alertsSummary || {};
        
        new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['Tồn Kho Thấp', 'Sắp Hết Hạn', 'Đã Hết Hạn'],
                datasets: [{
                    data: [
                        alerts.lowStockCount || 0,
                        alerts.nearExpiryCount || 0,
                        alerts.expiredCount || 0
                    ],
                    backgroundColor: [
                        'rgba(255, 193, 7, 0.8)',
                        'rgba(255, 99, 132, 0.8)',
                        'rgba(108, 117, 125, 0.8)'
                    ],
                    borderColor: [
                        'rgb(255, 193, 7)',
                        'rgb(255, 99, 132)',
                        'rgb(108, 117, 125)'
                    ],
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom'
                    }
                }
            }
        });
    }

    /**
     * Top Selling Items Horizontal Bar Chart
     */
    function renderTopSellingChart() {
        const ctx = document.getElementById('topSellingChart');
        if (!ctx) return;

        const data = dashboardData.topSellingItems || [];
        
        new Chart(ctx, {
            type: 'bar',
            data: {
                labels: data.map(d => d.itemName),
                datasets: [{
                    label: 'Số Lượng Bán',
                    data: data.map(d => d.quantitySold),
                    backgroundColor: 'rgba(54, 162, 235, 0.8)',
                    borderColor: 'rgb(54, 162, 235)',
                    borderWidth: 1
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        callbacks: {
                            afterLabel: function(context) {
                                const item = data[context.dataIndex];
                                return 'Doanh thu: ' + formatCurrency(item.revenue);
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        beginAtZero: true
                    }
                }
            }
        });
    }

    /**
     * Branch Comparison Bar Chart
     */
    function renderBranchComparisonChart() {
        const ctx = document.getElementById('branchComparisonChart');
        if (!ctx) return;

        const data = dashboardData.branchComparison || [];
        
        new Chart(ctx, {
            type: 'bar',
            data: {
                labels: data.map(d => d.branchName),
                datasets: [{
                    label: 'Doanh Thu (đ)',
                    data: data.map(d => d.revenue),
                    backgroundColor: 'rgba(153, 102, 255, 0.8)',
                    borderColor: 'rgb(153, 102, 255)',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: true
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                return 'Doanh thu: ' + formatCurrency(context.parsed.y);
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function(value) {
                                return formatCurrency(value);
                            }
                        }
                    }
                }
            }
        });
    }

    /**
     * Format currency helper
     */
    function formatCurrency(value) {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND',
            minimumFractionDigits: 0
        }).format(value);
    }

})();

