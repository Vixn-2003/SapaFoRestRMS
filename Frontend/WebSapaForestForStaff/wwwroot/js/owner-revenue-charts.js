/**
 * Owner Revenue Charts Module
 * Renders all charts for Revenue View using Chart.js
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
        if (typeof revenueData === 'undefined') {
            console.error('Revenue data not found');
            return;
        }

        renderRevenueTrendChart();
        renderPaymentMethodChart();
    }

    /**
     * Revenue Trend Area Chart
     */
    function renderRevenueTrendChart() {
        const ctx = document.getElementById('revenueTrendChart');
        if (!ctx) return;

        const data = revenueData.trendData || [];
        
        new Chart(ctx, {
            type: 'line',
            data: {
                labels: data.map(d => d.date),
                datasets: [{
                    label: 'Doanh Thu (đ)',
                    data: data.map(d => d.revenue),
                    borderColor: 'rgb(75, 192, 192)',
                    backgroundColor: 'rgba(75, 192, 192, 0.3)',
                    tension: 0.4,
                    fill: true,
                    pointRadius: 4,
                    pointHoverRadius: 6
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
                                const item = data[context.dataIndex];
                                return [
                                    'Doanh thu: ' + formatCurrency(context.parsed.y),
                                    'Số đơn: ' + item.orderCount
                                ];
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
     * Payment Method Pie Chart
     */
    function renderPaymentMethodChart() {
        const ctx = document.getElementById('paymentMethodChart');
        if (!ctx) return;

        const breakdown = revenueData.paymentBreakdown || {};
        
        new Chart(ctx, {
            type: 'pie',
            data: {
                labels: ['Tiền Mặt', 'QR Code'],
                datasets: [{
                    data: [
                        breakdown.cashAmount || 0,
                        breakdown.qrAmount || 0
                    ],
                    backgroundColor: [
                        'rgba(40, 167, 69, 0.8)',
                        'rgba(23, 162, 184, 0.8)'
                    ],
                    borderColor: [
                        'rgb(40, 167, 69)',
                        'rgb(23, 162, 184)'
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
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                const label = context.label || '';
                                const value = context.parsed || 0;
                                const count = context.dataIndex === 0 ? breakdown.cashCount :
                                             breakdown.qrCount;
                                return [
                                    label + ': ' + formatCurrency(value),
                                    'Số giao dịch: ' + count
                                ];
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

