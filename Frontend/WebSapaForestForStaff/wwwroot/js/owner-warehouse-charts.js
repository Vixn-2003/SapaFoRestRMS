/**
 * Owner Warehouse Alert Charts Module
 * Renders all charts for Warehouse Alert View using Chart.js
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
        if (typeof warehouseData === 'undefined') {
            console.error('Warehouse data not found');
            return;
        }

        renderAlertCategoryChart();
        renderStockLevelChart();
        renderExpiryTimelineChart();
    }

    /**
     * Alert Category Donut Chart
     */
    function renderAlertCategoryChart() {
        const ctx = document.getElementById('alertCategoryChart');
        if (!ctx) return;

        const distribution = warehouseData.categoryDistribution || {};
        
        new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['Tồn Kho Thấp', 'Sắp Hết Hạn', 'Đã Hết Hạn'],
                datasets: [{
                    data: [
                        distribution.lowStockCount || 0,
                        distribution.nearExpiryCount || 0,
                        distribution.expiredCount || 0
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
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                const label = context.label || '';
                                const value = context.parsed || 0;
                                return label + ': ' + value + ' mục';
                            }
                        }
                    }
                }
            }
        });
    }

    /**
     * Stock Level Bar Chart (Current vs Reorder Level)
     */
    function renderStockLevelChart() {
        const ctx = document.getElementById('stockLevelChart');
        if (!ctx) return;

        const data = warehouseData.stockLevelChart || [];
        
        new Chart(ctx, {
            type: 'bar',
            data: {
                labels: data.map(d => d.ingredientName),
                datasets: [
                    {
                        label: 'Số Lượng Hiện Tại',
                        data: data.map(d => d.currentQuantity),
                        backgroundColor: 'rgba(255, 99, 132, 0.8)',
                        borderColor: 'rgb(255, 99, 132)',
                        borderWidth: 1
                    },
                    {
                        label: 'Ngưỡng Đặt Hàng',
                        data: data.map(d => d.reorderLevel),
                        backgroundColor: 'rgba(54, 162, 235, 0.8)',
                        borderColor: 'rgb(54, 162, 235)',
                        borderWidth: 1
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'top'
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                return context.dataset.label + ': ' + context.parsed.y.toFixed(2);
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });
    }

    /**
     * Expiry Timeline Bar Chart
     */
    function renderExpiryTimelineChart() {
        const ctx = document.getElementById('expiryTimelineChart');
        if (!ctx) return;

        const data = warehouseData.expiryTimeline || [];
        
        // Sort by days until expiry
        const sortedData = [...data].sort((a, b) => a.daysUntilExpiry - b.daysUntilExpiry);
        
        new Chart(ctx, {
            type: 'bar',
            data: {
                labels: sortedData.map(d => d.ingredientName),
                datasets: [{
                    label: 'Số Ngày Còn Lại',
                    data: sortedData.map(d => d.daysUntilExpiry),
                    backgroundColor: sortedData.map(d => {
                        if (d.daysUntilExpiry <= 3) return 'rgba(220, 53, 69, 0.8)';
                        if (d.daysUntilExpiry <= 7) return 'rgba(255, 193, 7, 0.8)';
                        return 'rgba(40, 167, 69, 0.8)';
                    }),
                    borderColor: sortedData.map(d => {
                        if (d.daysUntilExpiry <= 3) return 'rgb(220, 53, 69)';
                        if (d.daysUntilExpiry <= 7) return 'rgb(255, 193, 7)';
                        return 'rgb(40, 167, 69)';
                    }),
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                const item = sortedData[context.dataIndex];
                                return [
                                    'Còn lại: ' + item.daysUntilExpiry + ' ngày',
                                    'Số lượng: ' + item.quantity.toFixed(2),
                                    'Hết hạn: ' + formatDate(item.expiryDate)
                                ];
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        title: {
                            display: true,
                            text: 'Số Ngày Còn Lại'
                        }
                    }
                }
            }
        });
    }

    /**
     * Format date helper
     */
    function formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString('vi-VN');
    }

})();

