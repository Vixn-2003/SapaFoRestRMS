/**
 * Admin Dashboard Charts
 * Sử dụng Chart.js để render các biểu đồ
 * Data được truyền từ Razor View (KHÔNG gọi API từ JS)
 */

document.addEventListener('DOMContentLoaded', function () {
    console.log('Admin Dashboard Charts Initializing...');

    // Check if data is available
    if (typeof userRolesData === 'undefined' || typeof revenueData === 'undefined') {
        console.error('Dashboard data is not available');
        return;
    }

    // Initialize all charts
    initUserRolesChart();
    initRevenueChart();
    initOrdersChart();
    initWarehouseAlertsChart();
});

/**
 * 1. User Roles Distribution - Pie Chart
 */
function initUserRolesChart() {
    const ctx = document.getElementById('userRolesChart');
    if (!ctx) return;

    // Prepare data from userRolesData
    const roleLabels = [];
    const roleCounts = [];
    const roleColors = {
        'Admin': '#dc3545',      // Red
        'Owner': '#6f42c1',      // Purple
        'Manager': '#007bff',    // Blue
        'Staff': '#17a2b8',      // Cyan
        'Cashier': '#28a745',    // Green
        'Waiter': '#ffc107',     // Yellow
        'Kitchen': '#fd7e14',    // Orange
        'Customer': '#6c757d'    // Gray
    };

    // Build data arrays
    if (userRolesData.roleDistribution) {
        for (const [roleName, count] of Object.entries(userRolesData.roleDistribution)) {
            if (count > 0) { // Only show roles with users
                roleLabels.push(roleName);
                roleCounts.push(count);
            }
        }
    }

    // Prepare colors
    const backgroundColors = roleLabels.map(role => roleColors[role] || '#6c757d');

    new Chart(ctx, {
        type: 'pie',
        data: {
            labels: roleLabels,
            datasets: [{
                label: 'Số lượng',
                data: roleCounts,
                backgroundColor: backgroundColors,
                borderWidth: 2,
                borderColor: '#ffffff'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'right',
                    labels: {
                        boxWidth: 12,
                        padding: 10,
                        font: {
                            size: 11
                        }
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            const label = context.label || '';
                            const value = context.parsed || 0;
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const percentage = ((value / total) * 100).toFixed(1);
                            return `${label}: ${value} (${percentage}%)`;
                        }
                    }
                }
            }
        }
    });
}

/**
 * 2. Revenue Last 7 Days - Line Chart
 */
function initRevenueChart() {
    const ctx = document.getElementById('revenueChart');
    if (!ctx) return;

    // Prepare data from revenueData
    const labels = revenueData.map(item => item.dateLabel);
    const revenues = revenueData.map(item => item.revenue);

    new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: 'Doanh Thu (VNĐ)',
                data: revenues,
                borderColor: '#28a745',
                backgroundColor: 'rgba(40, 167, 69, 0.1)',
                borderWidth: 3,
                fill: true,
                tension: 0.4,
                pointRadius: 5,
                pointBackgroundColor: '#28a745',
                pointBorderColor: '#ffffff',
                pointBorderWidth: 2,
                pointHoverRadius: 7
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                intersect: false,
                mode: 'index'
            },
            plugins: {
                legend: {
                    display: true,
                    position: 'top'
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            let label = context.dataset.label || '';
                            if (label) {
                                label += ': ';
                            }
                            label += new Intl.NumberFormat('vi-VN').format(context.parsed.y) + ' VNĐ';
                            return label;
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback: function (value) {
                            if (value >= 1000000) {
                                return (value / 1000000).toFixed(1) + 'M';
                            } else if (value >= 1000) {
                                return (value / 1000).toFixed(0) + 'K';
                            }
                            return value;
                        }
                    }
                },
                x: {
                    grid: {
                        display: false
                    }
                }
            }
        }
    });
}

/**
 * 3. Orders Last 7 Days - Bar Chart
 */
function initOrdersChart() {
    const ctx = document.getElementById('ordersChart');
    if (!ctx) return;

    // Prepare data from ordersData
    const labels = ordersData.map(item => item.dateLabel);
    const orderCounts = ordersData.map(item => item.orderCount);

    new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Số Đơn Hàng',
                data: orderCounts,
                backgroundColor: '#007bff',
                borderColor: '#0056b3',
                borderWidth: 1,
                borderRadius: 5,
                barPercentage: 0.7
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
                        label: function (context) {
                            return `Số đơn: ${context.parsed.y}`;
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        stepSize: 1,
                        precision: 0
                    }
                },
                x: {
                    grid: {
                        display: false
                    }
                }
            }
        }
    });
}

/**
 * 4. Warehouse Alerts Distribution - Donut Chart
 */
function initWarehouseAlertsChart() {
    const ctx = document.getElementById('warehouseAlertsChart');
    if (!ctx) return;

    // Prepare data from warehouseAlertsData
    const labels = ['Hết Hạn', 'Sắp Hết Hạn', 'Sắp Hết'];
    const counts = [
        warehouseAlertsData.expiredIngredientsCount || 0,
        warehouseAlertsData.nearExpiryCount || 0,
        warehouseAlertsData.lowStockCount || 0
    ];

    // Check if there's any data
    const totalAlerts = counts.reduce((a, b) => a + b, 0);
    if (totalAlerts === 0) {
        // Show "No Alerts" message
        const container = ctx.parentElement;
        container.innerHTML = '<div class="text-center text-muted py-5"><i class="fas fa-check-circle fa-3x mb-3"></i><p>Không có cảnh báo nào</p></div>';
        return;
    }

    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: labels,
            datasets: [{
                label: 'Số lượng',
                data: counts,
                backgroundColor: [
                    '#dc3545',  // Red - Expired
                    '#ffc107',  // Yellow - Near Expiry
                    '#fd7e14'   // Orange - Low Stock
                ],
                borderWidth: 2,
                borderColor: '#ffffff'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        boxWidth: 12,
                        padding: 10,
                        font: {
                            size: 11
                        }
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            const label = context.label || '';
                            const value = context.parsed || 0;
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const percentage = ((value / total) * 100).toFixed(1);
                            return `${label}: ${value} (${percentage}%)`;
                        }
                    }
                }
            }
        }
    });
}

/**
 * Helper function to format numbers
 */
function formatNumber(num) {
    return new Intl.NumberFormat('vi-VN').format(num);
}

/**
 * Helper function to format currency
 */
function formatCurrency(num) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(num);
}

console.log('Admin Dashboard Charts Loaded Successfully');

