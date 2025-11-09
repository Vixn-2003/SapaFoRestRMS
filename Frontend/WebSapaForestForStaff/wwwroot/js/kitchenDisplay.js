// Kitchen Display System JavaScript - FIXED VERSION
// File: wwwroot/js/kitchenDisplay.js

const API_BASE = 'https://localhost:7096/api';
let signalRConnection = null;
let currentOrders = [];
let currentGroupedItems = [];
let currentViewMode = 'all';

// Main initialization function
(function () {
    function initKDS() {
        try {
            console.log('=== KDS JavaScript Loaded ===');

            // Create modal BEFORE any other initialization
            createModalIfNotExists();

            initializeSignalR();
            loadActiveOrders();

            // Auto-refresh every 30 seconds
            setInterval(() => {
                if (currentViewMode === 'theo-tung-mon') {
                    loadGroupedItems();
                } else {
                    loadActiveOrders();
                }
            }, 30000);

            // Update timers every minute
            setInterval(updateAllTimers, 60000);
        } catch (error) {
            console.error('Error in KDS initialization:', error);
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initKDS);
    } else {
        initKDS();
    }
})();

// SignalR Setup
function initializeSignalR() {
    signalRConnection = new signalR.HubConnectionBuilder()
        .withUrl(`${API_BASE.replace('/api', '')}/kitchenHub`, {
            skipNegotiation: true,
            transport: signalR.HttpTransportType.WebSockets
        })
        .withAutomaticReconnect()
        .build();

    signalRConnection.on('ItemStatusChanged', function (notification) {
        console.log('Item status changed:', notification);
        if (currentViewMode === 'theo-tung-mon') {
            loadGroupedItems();
        } else {
            loadActiveOrders();
        }
    });

    signalRConnection.on('NewOrderReceived', function (order) {
        console.log('New order received:', order);
        addNewOrder(order);
    });

    signalRConnection.on('OrderCompleted', function (orderId) {
        console.log('Order completed:', orderId);
        removeOrder(orderId);
    });

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

// Render orders to grid - FIXED
function renderOrders(orders) {
    const grid = document.getElementById('ordersGrid');

    if (!grid) {
        console.error('Orders grid not found!');
        return;
    }

    if (!orders || orders.length === 0) {
        grid.innerHTML = `
            <div class="empty-state">
                <i class="mdi mdi-food-off" style="font-size: 48px;"></i>
                <p class="mt-3">Không có đơn hàng nào</p>
            </div>
        `;
        return;
    }

    console.log('Rendering', orders.length, 'orders');
    grid.innerHTML = orders.map(order => createOrderCard(order)).join('');

    // Attach click handlers - SIMPLIFIED VERSION
    setTimeout(() => {
        const cards = grid.querySelectorAll('.order-card');
        console.log('Found', cards.length, 'order cards');

        cards.forEach(card => {
            card.addEventListener('click', function (e) {
                // Ignore if clicking on complete button
                if (e.target.closest('.btn-complete')) {
                    return;
                }

                const orderId = parseInt(this.getAttribute('data-order-id'));
                console.log('Card clicked, orderId:', orderId);

                if (orderId && !isNaN(orderId)) {
                    openOrderModal(orderId);
                }
            });
        });
    }, 50);
}

// Create single order card HTML
function createOrderCard(order) {
    const timerClass = getTimerClass(order.priorityLevel);
    const canComplete = order.completedItems === order.totalItems;
    const displayName = order.staffName || order.tableNumber;

    return `
        <div class="order-card" data-order-id="${order.orderId}">
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

            <div class="item-list">
                ${order.items.map(item => createItemRow(item)).join('')}
            </div>

            <button class="btn-complete" 
                    onclick="event.stopPropagation(); completeOrder(${order.orderId})"
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
        <div class="item-row" data-item-id="${item.orderDetailId}">
            <div class="item-name">
                ${item.menuItemName}
                ${item.Notes ? `<div class="special-instructions"><i class="mdi mdi-alert"></i> ${item.Notes}</div>` : ''}
            </div>
            <span class="item-quantity">${item.quantity}x</span>
            <span class="item-status ${statusClass}">${getStatusText(item.status)}</span>
        </div>
    `;
}

// Get timer badge class
function getTimerClass(priority) {
    switch (priority) {
        case 'Critical': return 'timer-critical';
        case 'Warning': return 'timer-warning';
        default: return 'timer-normal';
    }
}

// Calculate priority
function calculatePriority(waitingMinutes) {
    if (waitingMinutes > 15) return 'Critical';
    if (waitingMinutes >= 10) return 'Warning';
    return 'Normal';
}

// Update all timers
function updateAllTimers() {
    currentOrders.forEach(order => {
        const now = new Date();
        const createdAt = new Date(order.createdAt);
        const newWaitingMinutes = Math.floor((now - createdAt) / 60000);

        order.waitingMinutes = newWaitingMinutes;
        order.priorityLevel = calculatePriority(newWaitingMinutes);

        const orderCard = document.querySelector(`[data-order-id="${order.orderId}"]`);
        if (orderCard) {
            const timerBadge = orderCard.querySelector('.timer-badge');
            if (timerBadge) {
                timerBadge.textContent = `${newWaitingMinutes}p`;
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

// Complete order
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
                sousChefUserId: 1
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

// Add new order
function addNewOrder(order) {
    currentOrders.unshift(order);
    renderOrders(currentOrders);
    updateOrderCount(currentOrders.length);
    showSuccess(`Đơn mới: ${order.orderNumber}`);
}

// Remove order
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
        badge.textContent = currentViewMode === 'theo-tung-mon' ? `${count} món` : `${count} đơn`;
    }
}

// Refresh orders
function refreshOrders() {
    if (currentViewMode === 'theo-tung-mon') {
        loadGroupedItems();
    } else {
        loadActiveOrders();
    }
    showSuccess('Đã làm mới');
}

// Load grouped items
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

// Render grouped items
function renderGroupedItems(groupedItems) {
    const grid = document.getElementById('ordersGrid');
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

// Create item card
function createItemCard(item) {
    const imageUrl = item.imageUrl || '';
    const hasImage = imageUrl && imageUrl.trim() !== '';

    return `
        <div class="item-card" data-menu-item-id="${item.menuItemId}">
            <div class="item-image-container">
                ${hasImage
            ? `<img src="${imageUrl}" alt="${item.menuItemName}" class="item-image" onerror="this.parentElement.innerHTML='<div class=\\'item-image-placeholder\\'><i class=\\'mdi mdi-food\\'></i></div>'">`
            : `<div class="item-image-placeholder"><i class="mdi mdi-food"></i></div>`}
            </div>

            <div class="item-header">
                <div class="item-name-large">${item.menuItemName}</div>
                <div class="item-quantity-badge">x${item.totalQuantity}</div>
            </div>

            <div class="item-details-list">
                ${item.itemDetails.map(detail => createItemDetailRow(detail)).join('')}
            </div>
        </div>
    `;
}

// Create item detail row
function createItemDetailRow(detail) {
    const statusClass = `status-${detail.status.toLowerCase()}`;
    const timerClass = getTimerClass(calculatePriority(detail.waitingMinutes));

    return `
        <div class="item-detail-row" data-order-detail-id="${detail.orderDetailId}">
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

// Filter by status
function filterByStatus(type) {
    currentViewMode = type;

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

    const grid = document.getElementById('ordersGrid');

    if (type === 'theo-tung-mon') {
        grid.className = 'items-grid';
        loadGroupedItems();
    } else {
        grid.className = 'orders-grid';
        loadActiveOrders();
    }
}

// ===========================
// MODAL MANAGEMENT - FIXED
// ===========================

let currentModalOrder = null;
let selectedModalItems = new Set();

// Create modal dynamically - FIXED VERSION
function createModalIfNotExists() {
    let modalOverlay = document.getElementById('orderModalOverlay');

    if (!modalOverlay) {
        console.log('Creating modal...');
        modalOverlay = document.createElement('div');
        modalOverlay.id = 'orderModalOverlay';
        modalOverlay.className = 'order-modal-overlay';

        modalOverlay.innerHTML = `
            <div class="order-modal" onclick="event.stopPropagation()">
                <div class="order-modal-header">
                    <div class="order-modal-header-left">
                        <span class="order-modal-number" id="modalOrderNumber">#0</span>
                        <span class="order-modal-time" id="modalOrderTime">00:00</span>
                    </div>
                    <div class="order-modal-header-right">
                        <button class="btn-rush" id="btnRush" onclick="toggleRush()">
                            <i class="mdi mdi-clock-fast"></i> RUSH
                        </button>
                        <button class="btn-print" onclick="printOrder()">
                            <i class="mdi mdi-printer"></i> PRINT
                        </button>
                    </div>
                </div>

                <div class="order-modal-body">
                    <ul class="order-modal-items" id="modalOrderItems"></ul>
                </div>

                <div class="order-modal-footer">
                    <button class="btn-modal btn-modal-cancel" onclick="closeOrderModal()">Cancel</button>
                    <button class="btn-modal btn-modal-select-all" onclick="selectAllItems()">Select All</button>
                    <button class="btn-modal btn-modal-fire" onclick="fireSelectedItems()">Fire</button>
                    <button class="btn-modal btn-modal-unfulfill" onclick="unfulfillSelectedItems()">Unfulfill</button>
                    <button class="btn-modal btn-modal-fulfill" onclick="fulfillSelectedItems()">Fulfill</button>
                </div>
            </div>
        `;

        document.body.appendChild(modalOverlay);

        // Add click listener to close on overlay click
        modalOverlay.addEventListener('click', function (e) {
            if (e.target === modalOverlay) {
                closeOrderModal();
            }
        });

        console.log('✓ Modal created successfully');
    }
}

// Open order modal - FIXED VERSION
function openOrderModal(orderId) {
    console.log('=== Opening modal for order:', orderId, '===');

    orderId = parseInt(orderId);
    if (isNaN(orderId)) {
        console.error('Invalid orderId');
        showError('ID đơn hàng không hợp lệ');
        return;
    }

    const order = currentOrders.find(o => o.orderId === orderId);
    if (!order) {
        console.error('Order not found:', orderId);
        showError('Không tìm thấy đơn hàng');
        return;
    }

    currentModalOrder = order;
    selectedModalItems.clear();

    // Update modal content
    document.getElementById('modalOrderNumber').textContent = `#${order.orderNumber}`;

    const orderTime = new Date(order.createdAt);
    document.getElementById('modalOrderTime').textContent =
        `${String(orderTime.getHours()).padStart(2, '0')}:${String(orderTime.getMinutes()).padStart(2, '0')}`;

    const hasUrgent = order.items && order.items.some(item => item.isUrgent);
    const rushBtn = document.getElementById('btnRush');
    if (rushBtn) {
        rushBtn.classList.toggle('active', hasUrgent);
    }

    renderModalItems(order.items || []);

    // Show modal - SIMPLIFIED
    const modalOverlay = document.getElementById('orderModalOverlay');
    if (modalOverlay) {
        modalOverlay.classList.add('show');
        document.body.style.overflow = 'hidden';
        console.log('✓ Modal shown');
    } else {
        console.error('Modal not found!');
    }
}

// Close order modal - FIXED
function closeOrderModal() {
    const modalOverlay = document.getElementById('orderModalOverlay');
    if (modalOverlay) {
        modalOverlay.classList.remove('show');
        document.body.style.overflow = '';
    }

    currentModalOrder = null;
    selectedModalItems.clear();
}

// Render modal items
function renderModalItems(items) {
    const itemsList = document.getElementById('modalOrderItems');
    if (!itemsList) return;

    itemsList.innerHTML = items.map(item => {
        const itemId = item.orderDetailId;
        const isSelected = selectedModalItems.has(itemId);

        return `
            <li class="order-modal-item ${isSelected ? 'selected' : ''}" 
                data-item-id="${itemId}"
                onclick="toggleModalItemSelection(${itemId}, event)">
                <input type="checkbox" 
                       ${isSelected ? 'checked' : ''} 
                       onchange="toggleModalItemSelection(${itemId}, event)"
                       onclick="event.stopPropagation()">
                <span class="order-modal-item-text">
                    ${item.quantity} ${item.menuItemName}
                    ${item.specialInstructions || item.notes ?
                `<span style="color: #d32f2f; font-size: 14px;"> (${item.specialInstructions || item.notes})</span>` : ''}
                </span>
            </li>
        `;
    }).join('');
}

// Toggle item selection
function toggleModalItemSelection(itemId, event) {
    if (event) {
        event.stopPropagation();
    }

    if (selectedModalItems.has(itemId)) {
        selectedModalItems.delete(itemId);
    } else {
        selectedModalItems.add(itemId);
    }

    if (currentModalOrder) {
        renderModalItems(currentModalOrder.items);
    }
}

// Select all items
function selectAllItems() {
    if (!currentModalOrder) return;

    if (selectedModalItems.size === currentModalOrder.items.length) {
        selectedModalItems.clear();
    } else {
        currentModalOrder.items.forEach(item => {
            selectedModalItems.add(item.orderDetailId);
        });
    }

    renderModalItems(currentModalOrder.items);
}

// Toggle RUSH
async function toggleRush() {
    if (!currentModalOrder) return;

    const rushBtn = document.getElementById('btnRush');
    const isUrgent = rushBtn.classList.contains('active');
    const newUrgentStatus = !isUrgent;

    const promises = currentModalOrder.items.map(item =>
        markAsUrgent(item.orderDetailId, newUrgentStatus)
    );

    try {
        await Promise.all(promises);
        rushBtn.classList.toggle('active');
        showSuccess(newUrgentStatus ? 'Đã đánh dấu RUSH' : 'Đã bỏ đánh dấu RUSH');
        loadActiveOrders();
    } catch (error) {
        showError('Không thể cập nhật trạng thái RUSH');
    }
}

// Mark as urgent
async function markAsUrgent(orderDetailId, isUrgent) {
    const response = await fetch(`${API_BASE}/KitchenDisplay/mark-as-urgent`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ orderDetailId, isUrgent })
    });
    return await response.json();
}

// Fire selected items
async function fireSelectedItems() {
    if (selectedModalItems.size === 0) {
        showError('Vui lòng chọn ít nhất một món');
        return;
    }

    const promises = Array.from(selectedModalItems).map(orderDetailId =>
        updateItemStatusAPI(orderDetailId, 'Cooking')
    );

    try {
        await Promise.all(promises);
        showSuccess(`Đã bắt đầu nấu ${selectedModalItems.size} món`);
        selectedModalItems.clear();
        loadActiveOrders();
        closeOrderModal();
    } catch (error) {
        showError('Không thể bắt đầu nấu');
    }
}

// Unfulfill selected items
async function unfulfillSelectedItems() {
    if (selectedModalItems.size === 0) {
        showError('Vui lòng chọn ít nhất một món');
        return;
    }

    if (!confirm(`Xác nhận hủy hoàn thành ${selectedModalItems.size} món?`)) {
        return;
    }

    const promises = Array.from(selectedModalItems).map(orderDetailId => {
        const item = currentModalOrder.items.find(i => i.orderDetailId === orderDetailId);
        const newStatus = item.status === 'Done' ? 'Cooking' : item.status;
        return updateItemStatusAPI(orderDetailId, newStatus);
    });

    try {
        await Promise.all(promises);
        showSuccess(`Đã hủy hoàn thành ${selectedModalItems.size} món`);
        selectedModalItems.clear();
        loadActiveOrders();
        closeOrderModal();
    } catch (error) {
        showError('Không thể hủy hoàn thành');
    }
}

// Fulfill selected items
async function fulfillSelectedItems() {
    if (selectedModalItems.size === 0) {
        showError('Vui lòng chọn ít nhất một món');
        return;
    }

    const promises = Array.from(selectedModalItems).map(orderDetailId =>
        updateItemStatusAPI(orderDetailId, 'Done')
    );

    try {
        await Promise.all(promises);
        showSuccess(`Đã hoàn thành ${selectedModalItems.size} món`);
        selectedModalItems.clear();
        loadActiveOrders();
        closeOrderModal();
    } catch (error) {
        showError('Không thể hoàn thành món');
    }
}

// Update item status API
async function updateItemStatusAPI(orderDetailId, newStatus) {
    const response = await fetch(`${API_BASE}/KitchenDisplay/update-item-status`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            orderDetailId: orderDetailId,
            newStatus: newStatus,
            userId: 1
        })
    });
    return await response.json();
}

// Print order
function printOrder() {
    if (!currentModalOrder) return;

    const printWindow = window.open('', '_blank');
    const printContent = `
        <html>
            <head>
                <title>Order ${currentModalOrder.orderNumber}</title>
                <style>
                    body { font-family: Arial, sans-serif; padding: 20px; }
                    h1 { color: #333; }
                    .order-info { margin-bottom: 20px; }
                    .items-list { margin-top: 20px; }
                    .item { padding: 10px; border-bottom: 1px solid #ddd; }
                </style>
            </head>
            <body>
                <h1>Order #${currentModalOrder.orderNumber}</h1>
                <div class="order-info">
                    <p><strong>Bàn:</strong> ${currentModalOrder.tableNumber}</p>
                    <p><strong>Nhân viên:</strong> ${currentModalOrder.staffName}</p>
                    <p><strong>Thời gian:</strong> ${new Date(currentModalOrder.createdAt).toLocaleString('vi-VN')}</p>
                </div>
                <div class="items-list">
                    <h2>Danh sách món:</h2>
                    ${currentModalOrder.items.map(item => `
                        <div class="item">
                            <strong>${item.quantity}x ${item.menuItemName}</strong>
                            ${item.notes ? `<br><em>Ghi chú: ${item.notes}</em>` : ''}
                            <br>Trạng thái: ${getStatusText(item.status)}
                        </div>
                    `).join('')}
                </div>
            </body>
        </html>
    `;

    printWindow.document.write(printContent);
    printWindow.document.close();
    printWindow.print();
}

// Toast notifications
function showSuccess(message) {
    if (typeof toastr !== 'undefined') {
        toastr.success(message);
    } else {
        console.log('SUCCESS:', message);
    }
}

function showError(message) {
    if (typeof toastr !== 'undefined') {
        toastr.error(message);
    } else {
        console.error('ERROR:', message);
    }
}