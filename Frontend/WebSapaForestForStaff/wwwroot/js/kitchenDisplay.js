// Kitchen Display System JavaScript
// File: wwwroot/js/kitchenDisplay.js

const API_BASE = 'https://localhost:7096/api'; // Update with your API URL
let signalRConnection = null;
let currentOrders = [];
let currentGroupedItems = [];
let currentViewMode = 'all'; // 'all', 'theo-ban', 'theo-tung-mon'

// Initialize on page load
document.addEventListener('DOMContentLoaded', function () {
    initializeSignalR();
    loadActiveOrders();

    // Auto-refresh every 30 seconds as fallback
    setInterval(() => {
        if (currentViewMode === 'theo-tung-mon') {
            loadGroupedItems();
        } else {
            loadActiveOrders();
        }
    }, 30000);

    // Update timers every minute
    setInterval(updateAllTimers, 60000);
});

// SignalR Setup
function initializeSignalR() {
    signalRConnection = new signalR.HubConnectionBuilder()
        .withUrl(`${API_BASE.replace('/api', '')}/kitchenHub`, {
            skipNegotiation: true,
            transport: signalR.HttpTransportType.WebSockets
        })
        .withAutomaticReconnect()
        .build();

    // Listen for item status changes
    signalRConnection.on('ItemStatusChanged', function (notification) {
        console.log('Item status changed:', notification);
        updateItemStatus(notification);
        
        // Refresh current view
        if (currentViewMode === 'theo-tung-mon') {
            loadGroupedItems();
        } else {
            loadActiveOrders();
        }
    });

    // Listen for new orders
    signalRConnection.on('NewOrderReceived', function (order) {
        console.log('New order received:', order);
        addNewOrder(order);
    });

    // Listen for completed orders
    signalRConnection.on('OrderCompleted', function (orderId) {
        console.log('Order completed:', orderId);
        removeOrder(orderId);
    });

    // Start connection
    signalRConnection.start()
        .then(() => console.log('SignalR connected'))
        .catch(err => console.error('SignalR error:', err));
}

// Load active orders from API
async function loadActiveOrders() {
    try {
        const response = await fetch(`${API_BASE}/KitchenDisplay/active-orders`);
        const result = await response.json();

        if (result.success) {
            currentOrders = result.data;
            renderOrders(currentOrders);
            updateOrderCount(currentOrders.length);
        } else {
            showError('Không thể tải đơn hàng');
        }
    } catch (error) {
        console.error('Error loading orders:', error);
        showError('Lỗi kết nối API');
    }
}

// Render orders to grid
function renderOrders(orders) {
    const grid = document.getElementById('ordersGrid');

    if (!orders || orders.length === 0) {
        grid.innerHTML = `
            <div class="empty-state">
                <i class="mdi mdi-food-off" style="font-size: 48px;"></i>
                <p class="mt-3">Không có đơn hàng nào</p>
            </div>
        `;
        return;
    }

    grid.innerHTML = orders.map(order => createOrderCard(order)).join('');
}

// Create single order card HTML
function createOrderCard(order) {
    const timerClass = getTimerClass(order.priorityLevel);
    const canComplete = order.completedItems === order.totalItems;

    // Hiển thị tên nhân viên hoặc table number
    const displayName = order.staffName || order.tableNumber;

    return `
        <div class="order-card" data-order-id="${order.orderId}">
            <!-- Header -->
            <div class="d-flex justify-content-between align-items-center mb-3">
                <div>
                    <h4 class="mb-0"># ${order.orderNumber}</h4>
                    <small class="text-muted">
                        <i class="mdi mdi-account"></i> ${displayName}
                    </small>
                </div>
                <div class="timer-badge ${timerClass}">
                    ${order.waitingMinutes}p
                </div>
            </div>

            <!-- Progress -->
            <div class="mb-3">
                <div class="d-flex justify-content-between mb-1">
                    <small>Tiến độ</small>
                    <small>${order.completedItems}/${order.totalItems} món</small>
                </div>
                <div class="progress" style="height: 8px;">
                    <div class="progress-bar ${canComplete ? 'bg-success' : 'bg-warning'}" 
                         style="width: ${(order.completedItems / order.totalItems) * 100}%">
                    </div>
                </div>
            </div>

            <!-- Items List -->
            <div class="item-list">
                ${order.items.map(item => createItemRow(item)).join('')}
            </div>

            <!-- Complete Button -->
            <button class="btn-complete" 
                    onclick="completeOrder(${order.orderId})"
                    ${!canComplete ? 'disabled' : ''}>
                <i class="mdi mdi-check-circle"></i> Hoàn thành
            </button>
        </div>
    `;
}

// Create single item row HTML
function createItemRow(item) {
    const statusClass = `status-${item.status.toLowerCase()}`;

    return `
        <div class="item-row" data-item-id="${item.ticketDetailId}">
            <div class="item-name">
                ${item.menuItemName}
                ${item.specialInstructions ?
            `<div class="special-instructions">
                        <i class="mdi mdi-alert"></i> ${item.specialInstructions}
                    </div>` : ''}
            </div>
            <span class="item-quantity">${item.quantity}x</span>
            <span class="item-status ${statusClass}">${getStatusText(item.status)}</span>
        </div>
    `;
}

// Get timer badge class based on exact minutes
function getTimerClass(priority) {
    switch (priority) {
        case 'Critical': return 'timer-critical'; // >15 phút
        case 'Warning': return 'timer-warning';   // 10-15 phút
        default: return 'timer-normal';           // 1-10 phút
    }
}

// Calculate priority based on waiting minutes
function calculatePriority(waitingMinutes) {
    if (waitingMinutes > 15) return 'Critical';
    if (waitingMinutes >= 10) return 'Warning';
    return 'Normal';
}

// Update all timers on the page
function updateAllTimers() {
    currentOrders.forEach(order => {
        const now = new Date();
        const createdAt = new Date(order.createdAt);
        const newWaitingMinutes = Math.floor((now - createdAt) / 60000);

        // Update order object
        order.waitingMinutes = newWaitingMinutes;
        order.priorityLevel = calculatePriority(newWaitingMinutes);

        // Update DOM
        const orderCard = document.querySelector(`[data-order-id="${order.orderId}"]`);
        if (orderCard) {
            const timerBadge = orderCard.querySelector('.timer-badge');
            if (timerBadge) {
                // Update text
                timerBadge.textContent = `${newWaitingMinutes}p`;

                // Update class
                const newClass = getTimerClass(order.priorityLevel);
                timerBadge.className = `timer-badge ${newClass}`;
            }
        }
    });
}

// Get Vietnamese status text
function getStatusText(status) {
    const statusMap = {
        'Pending': 'Chờ',
        'Cooking': 'Đang nấu',
        'Done': 'Xong'
    };
    return statusMap[status] || status;
}

// Complete order (Sous Chef button)
async function completeOrder(orderId) {
    if (!confirm('Xác nhận hoàn thành đơn hàng này?')) {
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/KitchenDisplay/complete-order`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                orderId: orderId,
                sousChefUserId: 1 // TODO: Get from session
            })
        });

        const result = await response.json();

        if (result.success) {
            showSuccess('Đơn hàng đã hoàn thành!');
            removeOrder(orderId);
        } else {
            showError(result.message);
        }
    } catch (error) {
        console.error('Error completing order:', error);
        showError('Không thể hoàn thành đơn hàng');
    }
}

// Update item status from SignalR
function updateItemStatus(notification) {
    const itemElement = document.querySelector(`[data-item-id="${notification.ticketDetailId}"]`);
    if (!itemElement) return;

    const statusSpan = itemElement.querySelector('.item-status');
    if (statusSpan) {
        // Remove old status class
        statusSpan.classList.remove('status-pending', 'status-cooking', 'status-done');
        // Add new status class
        statusSpan.classList.add(`status-${notification.newStatus.toLowerCase()}`);
        statusSpan.textContent = getStatusText(notification.newStatus);
    }

    // Update order progress
    loadActiveOrders();
}

// Add new order from SignalR
function addNewOrder(order) {
    currentOrders.unshift(order);
    renderOrders(currentOrders);
    updateOrderCount(currentOrders.length);

    // Show notification
    showSuccess(`Đơn mới: ${order.orderNumber}`);
}

// Remove order from display
function removeOrder(orderId) {
    const orderCard = document.querySelector(`[data-order-id="${orderId}"]`);
    if (orderCard) {
        orderCard.style.transition = 'all 0.3s';
        orderCard.style.opacity = '0';
        orderCard.style.transform = 'scale(0.9)';

        setTimeout(() => {
            currentOrders = currentOrders.filter(o => o.orderId !== orderId);
            renderOrders(currentOrders);
            updateOrderCount(currentOrders.length);
        }, 300);
    }
}

// Update order count badge
function updateOrderCount(count) {
    const badge = document.getElementById('orderCount');
    if (badge) {
        if (currentViewMode === 'theo-tung-mon') {
            badge.textContent = `${count} món`;
        } else {
            badge.textContent = `${count} đơn`;
        }
    }
}

// Refresh button handler
function refreshOrders() {
    if (currentViewMode === 'theo-tung-mon') {
        loadGroupedItems();
    } else {
        loadActiveOrders();
    }
    showSuccess('Đã làm mới');
}

// Load grouped items by menu item
async function loadGroupedItems() {
    try {
        const response = await fetch(`${API_BASE}/KitchenDisplay/grouped-by-item`);
        const result = await response.json();

        if (result.success) {
            currentGroupedItems = result.data;
            renderGroupedItems(currentGroupedItems);
            updateOrderCount(currentGroupedItems.length);
        } else {
            showError('Không thể tải danh sách món');
        }
    } catch (error) {
        console.error('Error loading grouped items:', error);
        showError('Lỗi kết nối API');
    }
}

// Render grouped items to grid
function renderGroupedItems(groupedItems) {
    const grid = document.getElementById('ordersGrid');

    // Change grid class for items view
    grid.className = 'items-grid';

    if (!groupedItems || groupedItems.length === 0) {
        grid.innerHTML = `
            <div class="empty-state">
                <i class="mdi mdi-food-off" style="font-size: 48px;"></i>
                <p class="mt-3">Không có món nào</p>
            </div>
        `;
        return;
    }

    grid.innerHTML = groupedItems.map(item => createItemCard(item)).join('');
}

// Create single item card HTML (theo từng món)
function createItemCard(item) {
    const imageUrl = item.imageUrl || '';
    const hasImage = imageUrl && imageUrl.trim() !== '';

    return `
        <div class="item-card" data-menu-item-id="${item.menuItemId}">
            <!-- Image -->
            <div class="item-image-container">
                ${hasImage 
                    ? `<img src="${imageUrl}" alt="${item.menuItemName}" class="item-image" onerror="this.parentElement.innerHTML='<div class=\\'item-image-placeholder\\'><i class=\\'mdi mdi-food\\'></i></div>'">`
                    : `<div class="item-image-placeholder"><i class="mdi mdi-food"></i></div>`}
            </div>

            <!-- Header -->
            <div class="item-header">
                <div class="item-name-large">${item.menuItemName}</div>
                <div class="item-quantity-badge">x${item.totalQuantity}</div>
            </div>

            <!-- Details List -->
            <div class="item-details-list">
                ${item.itemDetails.map(detail => createItemDetailRow(detail)).join('')}
            </div>
        </div>
    `;
}

// Create item detail row HTML
function createItemDetailRow(detail) {
    const statusClass = `status-${detail.status.toLowerCase()}`;
    const timerClass = getTimerClass(calculatePriority(detail.waitingMinutes));

    return `
        <div class="item-detail-row" data-ticket-detail-id="${detail.ticketDetailId}">
            <div class="item-detail-order">
                <strong>${detail.orderNumber}</strong> - ${detail.tableNumber}
                ${detail.notes ? `<br><small style="color: #d32f2f;"><i class="mdi mdi-alert"></i> ${detail.notes}</small>` : ''}
            </div>
            <span class="item-detail-quantity">${detail.quantity}x</span>
            <span class="item-status ${statusClass}">${getStatusText(detail.status)}</span>
            <span class="timer-badge ${timerClass}" style="font-size: 14px; padding: 4px 8px; min-width: 50px; margin-left: 8px;">
                ${detail.waitingMinutes}p
            </span>
        </div>
    `;
}

// Filter handlers
function filterByStatus(type) {
    currentViewMode = type;

    // Update active button
    document.querySelectorAll('.filter-tabs .btn').forEach(btn => {
        btn.classList.remove('active');
    });

    const buttonMap = {
        'all': 'filter-all',
        'theo-ban': 'filter-theo-ban',
        'theo-tung-mon': 'filter-theo-tung-mon'
    };

    const activeButton = document.getElementById(buttonMap[type]);
    if (activeButton) {
        activeButton.classList.add('active');
    }

    // Load appropriate data
    const grid = document.getElementById('ordersGrid');
    
    if (type === 'theo-tung-mon') {
        grid.className = 'items-grid';
        loadGroupedItems();
    } else {
        grid.className = 'orders-grid';
        loadActiveOrders();
    }
}

// Toast notifications
function showSuccess(message) {
    if (typeof toastr !== 'undefined') {
        toastr.success(message);
    } else {
        alert(message);
    }
}

function showError(message) {
    if (typeof toastr !== 'undefined') {
        toastr.error(message);
    } else {
        alert(message);
    }
}