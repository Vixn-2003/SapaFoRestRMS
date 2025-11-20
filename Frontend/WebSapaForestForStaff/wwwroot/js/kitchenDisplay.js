// Kitchen Display System JavaScript - FIXED VERSION
// File: wwwroot/js/kitchenDisplay.js

const API_BASE = window.API_BASE_URL || 'https://localhost:7096/api';
let signalRConnection = null;
let currentOrders = [];
let currentGroupedItems = [];
let currentViewMode = 'theo-ban';

// Main initialization function
(function () {
    function initKDS() {
        try {
            console.log('=== KDS JavaScript Loaded ===');

            // Create modal BEFORE any other initialization
            createModalIfNotExists();

            initializeSignalR();
            loadOrdersByTable(); // Mặc định load theo bàn

            // Auto-refresh every 30 seconds
            setInterval(() => {
                if (currentViewMode === 'theo-tung-mon') {
                    loadGroupedItems();
                } else {
                    loadOrdersByTable();
                }
                
                // Auto-refresh completed orders nếu đang hiển thị
                const completedColumn = document.getElementById('completedOrdersColumn');
                if (completedColumn && !completedColumn.classList.contains('hidden')) {
                    loadRecentlyFulfilledOrders();
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
    try {
        signalRConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${API_BASE.replace('/api', '')}/kitchenHub`, {
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets
            })
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: retryContext => {
                    if (retryContext.elapsedMilliseconds < 60000) {
                        return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
                    }
                    return null; // Stop retrying after 60 seconds
                }
            })
            .build();

        signalRConnection.on('ItemStatusChanged', function (notification) {
            console.log('Item status changed:', notification);
            if (currentViewMode === 'theo-tung-mon') {
                loadGroupedItems();
            } else {
                loadOrdersByTable();
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

        signalRConnection.onreconnecting(() => {
            console.log('SignalR reconnecting...');
        });

        signalRConnection.onreconnected(() => {
            console.log('SignalR reconnected');
        });

        signalRConnection.onclose(() => {
            console.log('SignalR connection closed');
        });

        signalRConnection.start()
            .then(() => {
                console.log('SignalR connected successfully');
            })
            .catch(err => {
                console.error('SignalR connection error:', err);
                // Don't show error to user, just log it - API connection error will be shown separately
            });
    } catch (error) {
        console.error('Error initializing SignalR:', error);
    }
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
                    <h4 class="mb-0"># ${order.orderNumber} - Bàn ${order.tableNumber || 'N/A'}</h4>
                    <small class="text-muted">
                        <i class="mdi mdi-account"></i> ${displayName}
                    </small>
                </div>
                <div class="text-end">
                    <div class="timer-badge ${timerClass}">
                        ${order.waitingMinutes}p
                    </div>
                    <div class="mt-1">
                        <small class="text-muted">
                            <i class="mdi mdi-clock-outline"></i> Đã chờ: ${order.waitingMinutes}p
                        </small>
                    </div>
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
                ${item.menuItemName || ''}
                ${item.Notes ? `<div class="special-instructions"><i class="mdi mdi-alert"></i> ${item.Notes}</div>` : ''}
            </div>
            <span class="item-quantity">${item.quantity || 0}x</span>
            <span class="item-status ${statusClass}">${getStatusText(item.status || 'Pending')}</span>
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
    const confirmed = await showConfirmPopup('Xác nhận hoàn thành đơn hàng này?');
    if (!confirmed) {
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
            
            // Tự động reload đơn vừa hoàn thành nếu đang hiển thị
            const completedColumn = document.getElementById('completedOrdersColumn');
            if (completedColumn && !completedColumn.classList.contains('hidden')) {
                loadRecentlyFulfilledOrders();
            }
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
    // Reload theo view mode hiện tại
    if (currentViewMode === 'theo-tung-mon') {
        loadGroupedItems();
    } else {
        loadOrdersByTable();
    }
    showSuccess(`Đơn mới: ${order.orderNumber}`);
}

// Helper function to reload current view
function reloadCurrentView() {
    if (currentViewMode === 'theo-tung-mon') {
        loadGroupedItems();
    } else {
        loadOrdersByTable();
    }
}

// Remove order
function removeOrder(orderId) {
    const orderCard = document.querySelector(`[data-order-id="${orderId}"]`);
    if (orderCard) {
        orderCard.style.transition = 'all 0.3s';
        orderCard.style.opacity = '0';
        orderCard.style.transform = 'scale(0.9)';

        setTimeout(() => {
            reloadCurrentView();
        }, 300);
    }
}

// Update order count badge
function updateOrderCount(count) {
    const badge = document.getElementById('orderCount');
    if (badge) {
        if (currentViewMode === 'theo-tung-mon') {
            badge.textContent = `${count} món`;
        } else if (currentViewMode === 'theo-ban') {
            badge.textContent = `${count} bàn`;
        } else {
            badge.textContent = `${count} đơn`;
        }
    }
}

// Refresh orders
function refreshOrders() {
    if (currentViewMode === 'theo-tung-mon') {
        loadGroupedItems();
    } else {
        loadOrdersByTable();
    }
    showSuccess('Đã làm mới');
}

// Load orders grouped by table
async function loadOrdersByTable() {
    // Check if we're still in the correct view mode
    if (currentViewMode !== 'theo-ban') {
        return;
    }

    const grid = document.getElementById('ordersGrid');
    if (!grid) return;

    try {
        // Create abort controller for timeout
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 10000);
        
        const response = await fetch(`${API_BASE}/KitchenDisplay/active-orders`, {
            signal: controller.signal
        });
        
        clearTimeout(timeoutId);
        
        // Check if we're still in the correct view mode after fetch
        if (currentViewMode !== 'theo-ban') {
            return;
        }

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const result = await response.json();

        if (result.success) {
            const orders = result.data;
            // Lưu orders vào currentOrders để modal có thể tìm thấy
            currentOrders = orders;
            // Group orders by table number
            const groupedByTable = groupOrdersByTable(orders);
            
            // Double check view mode before rendering
            if (currentViewMode === 'theo-ban') {
                renderOrdersByTable(groupedByTable);
                updateOrderCount(groupedByTable.length);
            }
        } else {
            if (currentViewMode === 'theo-ban') {
                grid.innerHTML = `
                    <div class="empty-state">
                        <i class="mdi mdi-alert-circle" style="font-size: 48px; color: #dc3545;"></i>
                        <p class="mt-3">${result.message || 'Không thể tải đơn hàng'}</p>
                    </div>
                `;
            }
        }
    } catch (error) {
        console.error('Error loading orders by table:', error);
        if (currentViewMode === 'theo-ban') {
            let errorMessage = 'Không thể kết nối đến server';
            if (error.name === 'AbortError' || error.message === 'The operation was aborted.') {
                errorMessage = 'Kết nối quá lâu. Vui lòng kiểm tra lại server.';
            } else if (error.message && (error.message.includes('Failed to fetch') || error.message.includes('ERR_CONNECTION_REFUSED'))) {
                errorMessage = 'Không thể kết nối đến API server. Vui lòng đảm bảo backend đang chạy.';
            }
            
            grid.innerHTML = `
                <div class="empty-state">
                    <i class="mdi mdi-server-network-off" style="font-size: 48px; color: #dc3545;"></i>
                    <p class="mt-3" style="font-weight: bold; color: #dc3545;">${errorMessage}</p>
                    <p class="mt-2" style="font-size: 14px; color: #666;">Vui lòng kiểm tra:</p>
                    <ul style="text-align: left; display: inline-block; margin-top: 10px; color: #666;">
                        <li>Backend API server đang chạy (https://localhost:7096)</li>
                        <li>Kết nối mạng ổn định</li>
                        <li>Firewall không chặn kết nối</li>
                    </ul>
                </div>
            `;
        }
    }
}

// Group orders by table number
function groupOrdersByTable(orders) {
    const grouped = {};
    
    orders.forEach(order => {
        const tableKey = order.tableNumber || 'N/A';
        if (!grouped[tableKey]) {
            grouped[tableKey] = {
                tableNumber: tableKey,
                orders: [],
                totalItems: 0,
                completedItems: 0
            };
        }
        grouped[tableKey].orders.push(order);
        grouped[tableKey].totalItems += order.totalItems || 0;
        grouped[tableKey].completedItems += order.completedItems || 0;
    });
    
    return Object.values(grouped);
}

// Render orders grouped by table
function renderOrdersByTable(groupedByTable) {
    const grid = document.getElementById('ordersGrid');
    
    if (!groupedByTable || groupedByTable.length === 0) {
        grid.innerHTML = `
            <div class="empty-state">
                <i class="mdi mdi-food-off" style="font-size: 48px;"></i>
                <p class="mt-3">Không có đơn hàng nào</p>
            </div>
        `;
        return;
    }
    
    grid.innerHTML = groupedByTable.map(group => createTableGroupCard(group)).join('');
    
    // Attach click handlers
    setTimeout(() => {
        const cards = grid.querySelectorAll('.order-card');
        cards.forEach(card => {
            card.addEventListener('click', function (e) {
                if (e.target.closest('.btn-complete')) {
                    return;
                }
                const orderId = parseInt(this.getAttribute('data-order-id'));
                if (orderId && !isNaN(orderId)) {
                    openOrderModal(orderId);
                }
            });
        });
    }, 50);
}

// Create table group card (shows all orders for a table)
function createTableGroupCard(group) {
    const allOrdersHtml = group.orders.map(order => {
        const timerClass = getTimerClass(order.priorityLevel);
        const canComplete = order.completedItems === order.totalItems;
        
        return `
            <div class="order-card" data-order-id="${order.orderId}" style="margin-bottom: 15px;">
                <div class="d-flex justify-content-between align-items-center mb-3">
                    <div>
                        <h4 class="mb-0"># ${order.orderNumber} - Bàn ${order.tableNumber || 'N/A'}</h4>
                        <small class="text-muted">
                            <i class="mdi mdi-account"></i> ${order.staffName || order.tableNumber}
                        </small>
                    </div>
                    <div class="text-end">
                        <div class="timer-badge ${timerClass}">
                            ${order.waitingMinutes}p
                        </div>
                        <div class="mt-1">
                            <small class="text-muted">
                                <i class="mdi mdi-clock-outline"></i> Đã chờ  
                            </small>
                        </div>
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
    }).join('');
    
    return `
        <div class="table-group-container" style="margin-bottom: 30px;">
            <div class="table-group-header" style="background: #f5f5f5; padding: 15px; border-radius: 8px; margin-bottom: 15px;">
                <h3 style="margin: 0; display: flex; align-items: center; gap: 10px;">
                    <i class="mdi mdi-table"></i> ${group.orders.length} đơn | ${group.completedItems}/${group.totalItems} món đã hoàn thành
                </h3>
            </div>
            <div class="table-orders-list">
                ${allOrdersHtml}
            </div>
        </div>
    `;
}

// Load grouped items
async function loadGroupedItems() {
    // Check if we're still in the correct view mode
    if (currentViewMode !== 'theo-tung-mon') {
        return;
    }

    const grid = document.getElementById('ordersGrid');
    if (!grid) return;

    try {
        console.log('[loadGroupedItems] Fetching from:', `${API_BASE}/KitchenDisplay/grouped-by-item`);
        // Create abort controller for timeout
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 10000);
        
        const response = await fetch(`${API_BASE}/KitchenDisplay/grouped-by-item`, {
            signal: controller.signal
        });
        
        clearTimeout(timeoutId);
        
        // Check if we're still in the correct view mode after fetch
        if (currentViewMode !== 'theo-tung-mon') {
            return;
        }
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        
        const result = await response.json();
        console.log('[loadGroupedItems] API Response:', result);
        
        // Debug: Check if timeCook is in the data
        if (result.success && result.data && result.data.length > 0) {
            console.log('[loadGroupedItems] First item sample:', {
                menuItemName: result.data[0].menuItemName,
                timeCook: result.data[0].timeCook,
                timeCookType: typeof result.data[0].timeCook
            });
        }

        if (result.success && result.data) {
            currentGroupedItems = result.data;
            
            // Double check view mode before rendering
            if (currentViewMode === 'theo-tung-mon') {
                renderGroupedItems(currentGroupedItems);
                updateOrderCount(currentGroupedItems.length);
            }
        } else {
            console.error('[loadGroupedItems] API returned error:', result.message);
            if (currentViewMode === 'theo-tung-mon') {
                grid.innerHTML = `
                    <div class="empty-state">
                        <i class="mdi mdi-alert-circle" style="font-size: 48px; color: #dc3545;"></i>
                        <p class="mt-3">${result.message || 'Không thể tải danh sách món'}</p>
                    </div>
                `;
            }
        }
    } catch (error) {
        console.error('[loadGroupedItems] Error:', error);
        if (currentViewMode === 'theo-tung-mon') {
            let errorMessage = 'Không thể kết nối đến server';
            if (error.name === 'AbortError' || error.message === 'The operation was aborted.') {
                errorMessage = 'Kết nối quá lâu. Vui lòng kiểm tra lại server.';
            } else if (error.message && (error.message.includes('Failed to fetch') || error.message.includes('ERR_CONNECTION_REFUSED'))) {
                errorMessage = 'Không thể kết nối đến API server. Vui lòng đảm bảo backend đang chạy.';
            }
            
            grid.innerHTML = `
                <div class="empty-state">
                    <i class="mdi mdi-server-network-off" style="font-size: 48px; color: #dc3545;"></i>
                    <p class="mt-3" style="font-weight: bold; color: #dc3545;">${errorMessage}</p>
                    <p class="mt-2" style="font-size: 14px; color: #666;">Vui lòng kiểm tra:</p>
                    <ul style="text-align: left; display: inline-block; margin-top: 10px; color: #666;">
                        <li>Backend API server đang chạy (https://localhost:7096)</li>
                        <li>Kết nối mạng ổn định</li>
                        <li>Firewall không chặn kết nối</li>
                    </ul>
                </div>
            `;
        }
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

    const sortedItems = sortGroupedItems(groupedItems);

    grid.innerHTML = sortedItems.map(item => createItemCard(item)).join('');
    
    // Attach click handlers for "Bắt đầu nấu" buttons
    setTimeout(() => {
        const startCookButtons = grid.querySelectorAll('.btn-start-cook');
        startCookButtons.forEach(button => {
            button.addEventListener('click', function() {
                const orderDetailIds = JSON.parse(this.getAttribute('data-order-detail-ids') || '[]');
                if (orderDetailIds.length > 0) {
                    startCookingForItem(orderDetailIds);
                }
            });
        });
    }, 50);
}

function sortGroupedItems(items) {
    if (!Array.isArray(items)) {
        return [];
    }

    const LONG_COOK_THRESHOLD = 15;

    return [...items].sort((a, b) => {
        const timeCookA = Number(a.timeCook) || 0;
        const timeCookB = Number(b.timeCook) || 0;
        const isLongCookA = timeCookA > LONG_COOK_THRESHOLD;
        const isLongCookB = timeCookB > LONG_COOK_THRESHOLD;

        if (isLongCookA && isLongCookB) {
            if (timeCookB !== timeCookA) {
                return timeCookB - timeCookA;
            }
            return compareByWaiting(a, b);
        }

        if (isLongCookA) return -1;
        if (isLongCookB) return 1;

        return compareByWaiting(a, b);
    });
}

function compareByWaiting(a, b) {
    const waitingA = getItemWaitingScore(a);
    const waitingB = getItemWaitingScore(b);

    if (waitingB !== waitingA) {
        // Higher waiting minutes means older order, so show first
        return waitingB - waitingA;
    }

    const nameA = (a.menuItemName || '').toLowerCase();
    const nameB = (b.menuItemName || '').toLowerCase();
    return nameA.localeCompare(nameB);
}

function getItemWaitingScore(item) {
    if (!item) return 0;

    const baseWaiting = Number(item.waitingMinutes) || 0;

    if (!Array.isArray(item.itemDetails) || item.itemDetails.length === 0) {
        return baseWaiting;
    }

    return item.itemDetails.reduce((maxWait, detail) => {
        const waitValue = Number(detail.waitingMinutes);
        if (!isNaN(waitValue) && waitValue > maxWait) {
            return waitValue;
        }
        return maxWait;
    }, baseWaiting);
}

// Create item card
function createItemCard(item) {
    // Get all pending order detail IDs
    const pendingOrderDetailIds = item.itemDetails
        .filter(detail => detail.status === 'Pending' || !detail.status)
        .map(detail => detail.orderDetailId);

    // Format timeCook display
    // Check if timeCook exists and is a valid number
    const hasTimeCook = item.timeCook !== null && 
                        item.timeCook !== undefined && 
                        item.timeCook !== '' && 
                        !isNaN(Number(item.timeCook)) &&
                        Number(item.timeCook) > 0;
    
    const timeCookDisplay = hasTimeCook
        ? `<span style="color: #ff9800; font-weight: 600;">${item.timeCook}p</span>`
        : '<span style="color: #9e9e9e;">Không xác định</span>';

    return `
        <div class="item-card" data-menu-item-id="${item.menuItemId}">
            <div class="item-header" style="display: flex; flex-direction: column; gap: 4px;">
                <div class="item-name-large">
                    ${item.menuItemName} x${item.totalQuantity}
                </div>
                <div class="item-time-cook">
                    Thời gian nấu: ${timeCookDisplay}
                </div>
            </div>

            <div class="item-card-actions" style="padding: 15px; text-align: center;">
                <button class="btn btn-primary btn-start-cook" 
                        data-menu-item-id="${item.menuItemId}"
                        data-order-detail-ids="${JSON.stringify(pendingOrderDetailIds)}"
                        ${pendingOrderDetailIds.length === 0 ? 'disabled' : ''}
                        style="padding: 12px 24px; font-size: 16px; font-weight: 600; border-radius: 8px; width: 100%;">
                    <i class="mdi mdi-chef-hat"></i> Bắt đầu nấu
                </button>
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
        </div>
    `;
}

// Filter by status
function filterByStatus(type) {
    // Prevent multiple rapid clicks
    if (currentViewMode === type) {
        return;
    }

    currentViewMode = type;

    document.querySelectorAll('.filter-tabs .btn').forEach(btn => {
        btn.classList.remove('active');
    });

    const buttonMap = {
        'theo-ban': 'filter-theo-ban',
        'theo-tung-mon': 'filter-theo-tung-mon'
    };

    const activeButton = document.getElementById(buttonMap[type]);
    if (activeButton) {
        activeButton.classList.add('active');
    }

    const grid = document.getElementById('ordersGrid');
    if (!grid) {
        console.error('ordersGrid not found');
        return;
    }

    // Clear grid and show loading
    grid.innerHTML = '<div class="empty-state"><i class="mdi mdi-loading mdi-spin" style="font-size: 48px;"></i><p class="mt-3">Đang tải...</p></div>';

    if (type === 'theo-tung-mon') {
        grid.className = 'items-grid';
        loadGroupedItems();
    } else {
        // Mặc định là 'theo-ban'
        grid.className = 'orders-grid';
        loadOrdersByTable();
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
                            <i class="mdi mdi-clock-fast"></i> CẦN LÀM NGAY
                        </button>
                        <button class="btn-print" onclick="printOrder()">
                            <i class="mdi mdi-printer"></i> IN
                        </button>
                    </div>
                </div>

                <div class="order-modal-body">
                    <ul class="order-modal-items" id="modalOrderItems"></ul>
                </div>

                <div class="order-modal-footer">
                    <button class="btn-modal btn-modal-cancel" onclick="closeOrderModal()">Hủy</button>
                    <button class="btn-modal btn-modal-select-all" onclick="selectAllItems()">Chọn tất cả</button>
                    <button class="btn-modal btn-modal-fire" onclick="fireSelectedItems()">Bắt đầu nấu</button>
                    <button class="btn-modal btn-modal-unfulfill" onclick="unfulfillSelectedItems()">Hủy hoàn thành</button>
                    <button class="btn-modal btn-modal-fulfill" onclick="fulfillSelectedItems()">Hoàn thành</button>
                </div>
            </div>
        `;

        document.body.appendChild(modalOverlay);

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
        showSuccess(newUrgentStatus ? 'Đã đánh dấu cần làm ngay' : 'Đã bỏ đánh dấu cần làm ngay');
        reloadCurrentView();
    } catch (error) {
        showError('Không thể cập nhật trạng thái cần làm ngay');
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
        reloadCurrentView();
        closeOrderModal();
    } catch (error) {
        showError('Không thể bắt đầu nấu');
    }
}

// Start cooking for all items in a grouped item card
async function startCookingForItem(orderDetailIds) {
    if (!orderDetailIds || orderDetailIds.length === 0) {
        showError('Không có món nào để bắt đầu nấu');
        return;
    }

    const confirmed = await showConfirmPopup(`Bắt đầu nấu ${orderDetailIds.length} món này?`);
    if (!confirmed) {
        return;
    }

    try {
        const promises = orderDetailIds.map(orderDetailId =>
            updateItemStatusAPI(orderDetailId, 'Cooking')
        );

        await Promise.all(promises);
        showSuccess(`Đã bắt đầu nấu ${orderDetailIds.length} món`);
        reloadCurrentView();
    } catch (error) {
        console.error('Error starting cooking:', error);
        showError('Không thể bắt đầu nấu: ' + error.message);
    }
}

// Unfulfill selected items
async function unfulfillSelectedItems() {
    if (selectedModalItems.size === 0) {
        showError('Vui lòng chọn ít nhất một món');
        return;
    }

    const confirmed = await showConfirmPopup(`Xác nhận hủy hoàn thành ${selectedModalItems.size} món?`);
    if (!confirmed) {
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
        reloadCurrentView();
        closeOrderModal();

        // Nếu đang hiển thị danh sách đơn vừa hoàn thành thì reload lại
        const completedColumn = document.getElementById('completedOrdersColumn');
        if (completedColumn && !completedColumn.classList.contains('hidden')) {
            loadRecentlyFulfilledOrders();
        }
    } catch (error) {
        showError('Không thể hủy hoàn thành');
    }
}
// ===========================
// FULFILL FUNCTION - FIXED VERSION
// ===========================

async function fulfillSelectedItems() {
    console.log('═══════════════════════════════════════════');
    console.log('🔵 [fulfillSelectedItems] STARTED');
    console.log('═══════════════════════════════════════════');

    console.log('🔵 selectedModalItems:', Array.from(selectedModalItems));

    if (selectedModalItems.size === 0) {
        console.log('🔴 No items selected');
        showError('Vui lòng chọn ít nhất một món');
        return;
    }

    if (!currentModalOrder) {
        console.error('🔴 currentModalOrder is null');
        showError('Không tìm thấy thông tin đơn hàng');
        return;
    }

    console.log('🔵 currentModalOrder.items:', currentModalOrder.items);

    const pendingItems = [];
    const cookingItems = [];
    const doneItems = [];

    selectedModalItems.forEach(orderDetailId => {
        console.log(`\n🔵 Processing orderDetailId: ${orderDetailId}`);

        const item = currentModalOrder.items.find(i => i.orderDetailId === orderDetailId);

        if (!item) {
            console.error(`🔴 Item ${orderDetailId} NOT FOUND`);
            return;
        }

        console.log(`🔵 Found item:`, item);

        const rawStatus = item.status;
        const status = (rawStatus || 'Pending').trim();

        console.log(`🔵 Status: "${status}"`);

        // So sánh chính xác với backend format
        if (status === 'Pending' || status === 'Chờ') {
            console.log(`🔴 Item ${orderDetailId} is PENDING`);
            pendingItems.push({
                orderDetailId: orderDetailId,
                name: item.menuItemName || `Món ${orderDetailId}`,
                status: status
            });
        } else if (status === 'Cooking' || status === 'Đang nấu') {
            console.log(`🟢 Item ${orderDetailId} is COOKING`);
            cookingItems.push(orderDetailId);
        } else if (status === 'Done' || status === 'Hoàn thành' || status === 'Xong') {
            console.log(`🟡 Item ${orderDetailId} is DONE`);
            doneItems.push({
                orderDetailId: orderDetailId,
                name: item.menuItemName || `Món ${orderDetailId}`,
                status: status
            });
        } else {
            console.warn(`⚠️ Unknown status "${status}", treating as Pending`);
            pendingItems.push({
                orderDetailId: orderDetailId,
                name: item.menuItemName || `Món ${orderDetailId}`,
                status: status
            });
        }
    });

    console.log('\n═══════════════════════════════════════════');
    console.log('🔵 SUMMARY:');
    console.log('   - Pending items:', pendingItems.length, pendingItems);
    console.log('   - Cooking items:', cookingItems.length, cookingItems);
    console.log('   - Done items:', doneItems.length, doneItems);
    console.log('═══════════════════════════════════════════\n');

    // CRITICAL CHECK - Phải return nếu có pending
    if (pendingItems.length > 0) {
        console.log('🔴 🔴 🔴 BLOCKED - Found pending items 🔴 🔴 🔴');
        const itemNames = pendingItems.map(i => i.name).join(', ');
        showError(`Các món sau chưa nấu: ${itemNames}. Vui lòng bắt đầu nấu trước!`);
        console.log('🔴 RETURNING - NOT fulfilling anything');
        return; // ❌ DỪNG NGAY
    }

    if (cookingItems.length === 0) {
        console.log('🔴 BLOCKED - No cooking items found');
        if (doneItems.length > 0) {
            showError('Các món đã chọn đã hoàn thành rồi');
        } else {
            showError('Không có món nào đang nấu để hoàn thành');
        }
        return;
    }

    console.log(`🟢 PROCEEDING - Will fulfill ${cookingItems.length} cooking items`);

    const promises = cookingItems.map(orderDetailId => {
        console.log(`🟢 Calling updateItemStatusAPI(${orderDetailId}, "Done")`);
        return updateItemStatusAPI(orderDetailId, 'Done');
    });

    try {
        console.log('🟢 Executing Promise.all...');
        await Promise.all(promises);
        console.log('✅ All promises resolved successfully');

        // Cập nhật trạng thái local cho các món vừa hoàn thành
        cookingItems.forEach(orderDetailId => {
            const item = currentModalOrder.items.find(i =>
                i.orderDetailId === orderDetailId || i.OrderDetailId === orderDetailId
            );
            if (item) {
                item.status = 'Done';
            }
        });

        // Tự động in phiếu cho các món vừa hoàn thành
        printFulfilledItems(currentModalOrder, cookingItems);

        showSuccess(`Đã hoàn thành ${cookingItems.length} món`);
        selectedModalItems.clear();
        reloadCurrentView();
        closeOrderModal();

        const completedColumn = document.getElementById('completedOrdersColumn');
        if (completedColumn && !completedColumn.classList.contains('hidden')) {
            loadRecentlyFulfilledOrders();
        }
    } catch (error) {
        console.error('❌ Promise.all failed:', error);
        showError('Không thể hoàn thành món: ' + error.message);
    }
}

// ===========================
// UPDATE ITEM STATUS API
// ===========================

async function updateItemStatusAPI(orderDetailId, newStatus) {
    try {
        const payload = {
            orderDetailId: parseInt(orderDetailId),
            newStatus: newStatus.trim(),
            userId: 1
        };

        console.log('══════════════════════════════════════');
        console.log('UPDATE ITEM STATUS');
        console.log('══════════════════════════════════════');
        console.log('Payload:', JSON.stringify(payload, null, 2));
        console.log('API:', `${API_BASE}/KitchenDisplay/update-item-status`);

        const response = await fetch(`${API_BASE}/KitchenDisplay/update-item-status`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        console.log('Response Status:', response.status);

        const responseText = await response.text();
        console.log('Response Body:', responseText);

        if (!response.ok) {
            console.error('❌ REQUEST FAILED');

            // Parse error message từ backend
            try {
                const errorResult = JSON.parse(responseText);
                const errorMessage = errorResult.message || errorResult.Message || responseText;
                throw new Error(errorMessage);
            } catch (parseError) {
                throw new Error(`HTTP ${response.status}: ${responseText}`);
            }
        }

        const result = JSON.parse(responseText);
        console.log('✅ SUCCESS:', result);

        if (result.success === false) {
            throw new Error(result.message || 'Update failed');
        }

        return result;
    } catch (error) {
        console.error('❌ EXCEPTION:', error.message);
        throw error;
    }
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

// Print only fulfilled items
function printFulfilledItems(order, fulfilledItemIds) {
    if (!order || !Array.isArray(fulfilledItemIds) || fulfilledItemIds.length === 0) {
        return;
    }

    const normalizedIds = fulfilledItemIds.map(id => Number(id));

    const fulfilledItems = (order.items || order.Items || []).filter(item => {
        const id = item.orderDetailId ?? item.OrderDetailId;
        return normalizedIds.includes(Number(id));
    });

    if (fulfilledItems.length === 0) {
        console.log('[printFulfilledItems] No items found to print.');
        return;
    }

    const printWindow = window.open('', '_blank');
    const printContent = `
        <html>
            <head>
                <title>Phiếu món hoàn thành - ${order.orderNumber}</title>
                <style>
                    body { font-family: Arial, sans-serif; padding: 20px; }
                    h1 { color: #333; margin-bottom: 10px; }
                    .order-info { margin-bottom: 20px; }
                    .items-list { margin-top: 10px; }
                    .item { padding: 10px 0; border-bottom: 1px dashed #bbb; }
                    .item:last-child { border-bottom: none; }
                    .item strong { font-size: 16px; }
                    .notes { font-style: italic; color: #555; }
                </style>
            </head>
            <body>
                <h1>Phiếu món đã hoàn thành</h1>
                <div class="order-info">
                    <p><strong>Đơn:</strong> #${order.orderNumber}</p>
                    <p><strong>Bàn:</strong> ${order.tableNumber || 'N/A'}</p>
                    <p><strong>Nhân viên:</strong> ${order.staffName || 'N/A'}</p>
                    <p><strong>Thời gian in:</strong> ${new Date().toLocaleString('vi-VN')}</p>
                </div>
                <div class="items-list">
                    ${fulfilledItems.map(item => `
                        <div class="item">
                            <strong>${item.quantity ?? item.Quantity ?? 1}x ${item.menuItemName ?? item.MenuItemName ?? 'Món'}</strong>
                            ${item.notes || item.Notes ? `<div class="notes">Ghi chú: ${item.notes ?? item.Notes}</div>` : ''}
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

// Custom Confirm Popup - Thay thế confirm() native
function showConfirmPopup(message, title = 'Xác nhận') {
    return new Promise((resolve) => {
        const overlay = document.createElement('div');
        overlay.className = 'confirm-popup-overlay';
        
        overlay.innerHTML = `
            <div class="confirm-popup">
                <div class="confirm-popup-header">
                    <div class="confirm-popup-icon">
                        <i class="mdi mdi-alert"></i>
                    </div>
                    <h3 class="confirm-popup-title">${title}</h3>
                </div>
                <div class="confirm-popup-body">
                    ${message.replace(/\n/g, '<br>')}
                </div>
                <div class="confirm-popup-footer">
                    <button class="confirm-popup-btn confirm-popup-btn-cancel">Hủy</button>
                    <button class="confirm-popup-btn confirm-popup-btn-confirm">Xác nhận</button>
                </div>
            </div>
        `;

        document.body.appendChild(overlay);

        const confirmBtn = overlay.querySelector('.confirm-popup-btn-confirm');
        const cancelBtn = overlay.querySelector('.confirm-popup-btn-cancel');

        const close = (result) => {
            overlay.style.opacity = '0';
            setTimeout(() => {
                if (document.body.contains(overlay)) {
                    document.body.removeChild(overlay);
                }
                resolve(result);
            }, 200);
        };

        confirmBtn.addEventListener('click', () => close(true));
        cancelBtn.addEventListener('click', () => close(false));
        overlay.addEventListener('click', (e) => {
            if (e.target === overlay) {
                close(false);
            }
        });
    });
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

// ===========================
// RECENTLY FULFILLED ORDERS
// ===========================

// Toggle hiển thị đơn vừa hoàn thành (cột bên trái)
function toggleRecentlyFulfilled() {
    const column = document.getElementById('completedOrdersColumn');
    const btn = document.getElementById('btnShowRecentlyFulfilled');
    
    if (!column) {
        showError('Không tìm thấy cột đơn vừa hoàn thành');
        return;
    }

    // Toggle hiển thị
    if (column.classList.contains('hidden')) {
        // Hiển thị cột
        column.classList.remove('hidden');
        if (btn) {
            btn.classList.remove('btn-outline-info');
            btn.classList.add('btn-info');
            // Thay đổi text và icon
            btn.innerHTML = '<i class="mdi mdi-eye-off"></i> Ẩn đơn vừa hoàn thành';
            // Giữ lại onclick handler
            btn.setAttribute('onclick', 'toggleRecentlyFulfilled()');
        }
        // Load data
        loadRecentlyFulfilledOrders();
    } else {
        // Ẩn cột
        column.classList.add('hidden');
        if (btn) {
            btn.classList.remove('btn-info');
            btn.classList.add('btn-outline-info');
            // Thay đổi text và icon
            btn.innerHTML = '<i class="mdi mdi-history"></i> Hiển thị đơn vừa hoàn thành';
            // Giữ lại onclick handler
            btn.setAttribute('onclick', 'toggleRecentlyFulfilled()');
        }
    }
}

// Load danh sách đơn vừa hoàn thành
async function loadRecentlyFulfilledOrders() {
    const gridContainer = document.getElementById('completedOrdersGrid');
    if (!gridContainer) {
        console.error('[loadRecentlyFulfilledOrders] completedOrdersGrid not found');
        return;
    }

    try {
        console.log('[loadRecentlyFulfilledOrders] Loading recently fulfilled orders...');
        gridContainer.innerHTML = `
            <div class="text-center text-muted py-3">
                <i class="mdi mdi-loading mdi-spin" style="font-size: 24px;"></i>
                <p class="mt-2">Đang tải...</p>
            </div>
        `;

        const url = `${API_BASE}/KitchenDisplay/recently-fulfilled-orders?minutesAgo=10`;
        console.log('[loadRecentlyFulfilledOrders] Fetching:', url);
        
        const response = await fetch(url);
        
        if (!response.ok) {
            const errorText = await response.text();
            console.error('[loadRecentlyFulfilledOrders] HTTP error:', response.status, errorText);
            throw new Error(`HTTP ${response.status}: ${errorText}`);
        }

        const result = await response.json();
        console.log('[loadRecentlyFulfilledOrders] API Response:', result);
        console.log('[loadRecentlyFulfilledOrders] Orders count:', result.data?.length || 0);

        if (result.success && result.data) {
            renderRecentlyFulfilledOrders(result.data);
        } else {
            console.error('[loadRecentlyFulfilledOrders] API returned error:', result.message);
            gridContainer.innerHTML = `
                <div class="alert alert-warning">
                    <i class="mdi mdi-alert"></i> ${result.message || 'Không thể tải danh sách'}
                </div>
            `;
        }
    } catch (error) {
        console.error('[loadRecentlyFulfilledOrders] Error:', error);
        gridContainer.innerHTML = `
            <div class="alert alert-danger">
                <i class="mdi mdi-alert-circle"></i> Lỗi kết nối: ${error.message}
            </div>
        `;
    }
}

// Render danh sách đơn vừa hoàn thành (style giống hình - màu xanh lá, có checkmark)
function renderRecentlyFulfilledOrders(orders) {
    const gridContainer = document.getElementById('completedOrdersGrid');
    const countBadge = document.getElementById('completedOrdersCount');
    
    if (!gridContainer) {
        console.error('[renderRecentlyFulfilledOrders] completedOrdersGrid not found');
        return;
    }

    console.log('[renderRecentlyFulfilledOrders] Rendering orders:', orders?.length || 0);

    if (!orders || orders.length === 0) {
        console.log('[renderRecentlyFulfilledOrders] No orders to display');
        gridContainer.innerHTML = `
            <div class="text-center text-muted py-5">
                <i class="mdi mdi-check-circle" style="font-size: 48px; color: #28a745;"></i>
                <p class="mt-3">Không có đơn nào hoàn thành trong 10 phút gần đây</p>
            </div>
        `;
        if (countBadge) countBadge.textContent = '0';
        return;
    }

    // Update count
    if (countBadge) {
        countBadge.textContent = orders.length.toString();
        console.log('[renderRecentlyFulfilledOrders] Updated count badge:', orders.length);
    }

    let html = '';
    
    orders.forEach(order => {
        // Kiểm tra và lấy items (có thể là Items hoặc items)
        const items = order.Items || order.items || [];
        
        // Bỏ qua order không có items
        if (!items || items.length === 0) {
            console.warn('Order without items:', order);
            return;
        }
        
        const waitingMinutes = order.WaitingMinutes || order.waitingMinutes || 0;
        const minutes = Math.floor(waitingMinutes);
        const seconds = Math.floor((waitingMinutes - minutes) * 60);
        const timeDisplay = `${minutes}:${String(seconds).padStart(2, '0')}`;
        
        const orderNumber = order.OrderNumber || order.orderNumber || `#${order.OrderId || order.orderId || 'N/A'}`;
        
        html += `
            <div class="completed-order-card">
                <div class="order-header">
                    <div class="d-flex justify-content-between align-items-center">
                        <span>#${orderNumber}</span>
                        <span>${timeDisplay}</span>
                    </div>
                </div>
                <div class="mb-2">
                    <strong>Dine In</strong>
                </div>
                <div class="mb-2">
                    <strong>ENTREES</strong>
                </div>
                <div>
        `;

        items.forEach(item => {
            const menuItemName = item.MenuItemName || item.menuItemName || 'N/A';
            const quantity = item.Quantity || item.quantity || 1;
            const orderDetailId = item.OrderDetailId || item.orderDetailId || 0;
            const notes = item.Notes || item.notes || '';
            
            const itemNameEscaped = menuItemName.replace(/'/g, "\\'").replace(/"/g, '&quot;');
            const notesEscaped = notes ? notes.replace(/</g, '&lt;').replace(/>/g, '&gt;') : '';
            const menuItemNameEscaped = menuItemName.replace(/</g, '&lt;').replace(/>/g, '&gt;');
            
            html += `
                    <div class="completed-item">
                        <i class="mdi mdi-check-circle completed-item-check"></i>
                        <div class="flex-grow-1">
                            <span><strong>${quantity}</strong> ${menuItemNameEscaped}</span>
                            ${notes ? `<br><small class="text-muted"><i class="mdi mdi-note-text"></i> ${notesEscaped}</small>` : ''}
                        </div>
                        <button class="btn btn-sm btn-outline-warning ms-2" 
                                onclick="recallOrderDetail(${orderDetailId}, '${itemNameEscaped}')"
                                title="Khôi phục món này">
                            <i class="mdi mdi-restore"></i>
                        </button>
                    </div>
            `;
        });

        html += `
                </div>
            </div>
        `;
    });

    gridContainer.innerHTML = html;
}

// Khôi phục (Recall) một order detail
async function recallOrderDetail(orderDetailId, itemName) {
    const confirmed = await showConfirmPopup(
        `Xác nhận khôi phục món "${itemName}"?<br><br>Món này sẽ quay lại trạng thái đang xử lý.`,
        'Xác nhận khôi phục'
    );
    if (!confirmed) {
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/KitchenDisplay/recall-order-detail`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                orderDetailId: orderDetailId,
                userId: 1 // TODO: Lấy từ session/user context
            })
        });

        const result = await response.json();

        if (result.success) {
            showSuccess(`Đã khôi phục món "${itemName}" thành công`);
            // Reload danh sách đơn vừa hoàn thành
            const column = document.getElementById('completedOrdersColumn');
            if (column && !column.classList.contains('hidden')) {
                loadRecentlyFulfilledOrders();
            }
            // Reload orders chính
            refreshOrders();
        } else {
            showError(result.message || 'Không thể khôi phục món');
        }
    } catch (error) {
        console.error('Error recalling order detail:', error);
        showError('Lỗi kết nối: ' + error.message);
    }
}