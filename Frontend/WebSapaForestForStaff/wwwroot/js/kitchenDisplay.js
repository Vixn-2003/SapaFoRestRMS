// Kitchen Display System JavaScript - FIXED VERSION
// File: wwwroot/js/kitchenDisplay.js

const API_BASE = window.API_BASE_URL || 'https://localhost:7096/api';
let signalRConnection = null;
let currentOrders = [];
let currentGroupedItems = [];
let currentViewMode = 'theo-ban';
let currentStatusFilter = 'all'; // 'all', 'Pending', 'Cooking', 'Late', 'Ready', 'Done'

// Main initialization function - OPTIMIZED
(function () {
    function initKDS() {
        try {
            // Create modal BEFORE any other initialization
            createModalIfNotExists();

            // Initialize status filter button
            const allButton = document.getElementById('filter-status-all');
            if (allButton) {
                allButton.classList.add('active');
            }

            // OPTIMIZED: Load data trước, SignalR sau (lazy load)
            // Hiển thị loading indicator
            const grid = document.getElementById('ordersGrid');
            if (grid) {
                grid.innerHTML = '<div class="empty-state"><i class="mdi mdi-loading mdi-spin" style="font-size: 48px;"></i><p class="mt-3">Đang tải dữ liệu...</p></div>';
            }

            // Load data ngay lập tức
            loadOrdersByTable().then(() => {
                // Sau khi data đã load xong, mới kết nối SignalR (lazy load)
                // Delay nhỏ để đảm bảo UI đã render
                setTimeout(() => {
                    initializeSignalR();
                }, 500);
            }).catch(error => {
                console.error('Error loading initial data:', error);
                // Vẫn thử kết nối SignalR dù có lỗi
                setTimeout(() => {
                    initializeSignalR();
                }, 500);
            });

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

// SignalR Setup - OPTIMIZED (lazy load, không block UI)
function initializeSignalR() {
    // Nếu đã có connection, không tạo lại
    if (signalRConnection && signalRConnection.state !== signalR.HubConnectionState.Disconnected) {
        return;
    }

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
            if (currentViewMode === 'theo-tung-mon') {
                loadGroupedItems();
            } else {
                loadOrdersByTable();
            }
        });

        signalRConnection.on('NewOrderReceived', function (order) {
            addNewOrder(order);
        });

        signalRConnection.on('OrderCompleted', function (orderId) {
            removeOrder(orderId);
        });

        signalRConnection.onreconnecting(() => {
            // Reconnecting...
        });

        signalRConnection.onreconnected(() => {
            // Reconnected
        });

        signalRConnection.onclose(() => {
            // Connection closed
        });

        // OPTIMIZED: Start connection trong background, không block
        signalRConnection.start()
            .then(() => {
                console.log('SignalR connected successfully');
            })
            .catch(err => {
                console.error('SignalR connection error:', err);
                // Don't show error to user, just log it - API connection error will be shown separately
                // Retry sau 5 giây
                setTimeout(() => {
                    if (signalRConnection && signalRConnection.state === signalR.HubConnectionState.Disconnected) {
                        initializeSignalR();
                    }
                }, 5000);
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

    const renderedCards = orders.map(order => createOrderCard(order)).filter(html => html.trim() !== '').join('');
    
    if (renderedCards.trim() === '') {
        grid.innerHTML = `
            <div class="empty-state">
                <i class="mdi mdi-filter-off" style="font-size: 48px;"></i>
                <p class="mt-3">Không có món nào với trạng thái "${getStatusText(currentStatusFilter)}"</p>
            </div>
        `;
        return;
    }
    
    grid.innerHTML = renderedCards;

    // Attach click handlers - SIMPLIFIED VERSION
    setTimeout(() => {
        const cards = grid.querySelectorAll('.order-card');

        cards.forEach(card => {
            card.addEventListener('click', function (e) {
                // Ignore if clicking on complete button
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

// Sort items by course type: Khai vị -> Món chính -> Tráng miệng
function sortItemsByCourseType(items) {
    const courseTypeOrder = {
        'Khai vị': 0,
        'Món chính': 1,
        'Tráng miệng': 2
    };
    
    return [...items].sort((a, b) => {
        const courseTypeA = a.courseType || '';
        const courseTypeB = b.courseType || '';
        
        const orderA = courseTypeOrder[courseTypeA] !== undefined ? courseTypeOrder[courseTypeA] : 999;
        const orderB = courseTypeOrder[courseTypeB] !== undefined ? courseTypeOrder[courseTypeB] : 999;
        
        if (orderA !== orderB) {
            return orderA - orderB;
        }
        
        // Nếu cùng loại, giữ nguyên thứ tự ban đầu
        return 0;
    });
}

// Create single order card HTML
function createOrderCard(order) {
    const timerClass = getTimerClass(order.priorityLevel);
    // ✅ SỬA: Backend đã tính completedItems = Ready + Done
    const completedItems = order.completedItems || 0; // Backend trả về readyCount + doneCount
    const canComplete = completedItems === order.totalItems;
    const numberOfGuests = order.numberOfGuests || 0;
    
    // ✅ Backend đã sort by course type rồi, hiển thị tất cả items (kể cả Ready và Done)
    let sortedItems = order.items || [];
    
    // Kiểm tra xem có món nào làm gấp VÀ đang ở trạng thái Pending (chờ bếp xác nhận) không
    // Chỉ hiển thị "LÀM GẤP" nếu còn món làm gấp đang chờ, không hiển thị nếu đã chuyển sang Cooking/Ready
    const hasUrgentPendingItems = sortedItems.some(item => {
        const isUrgent = item.isUrgent === true || item.IsUrgent === true;
        if (!isUrgent) return false;
        
        // Kiểm tra status là Pending (chờ bếp xác nhận)
        const status = (item.status || '').toLowerCase().trim();
        const isPending = status.includes('pending') || status.includes('chờ') || status.includes('chờ bếp');
        return isPending;
    });
    
    // Nếu không có items sau khi filter, không render order card này
    if (sortedItems.length === 0) {
        return '';
    }

    return `
        <div class="order-card ${hasUrgentPendingItems ? 'has-urgent' : ''}" data-order-id="${order.orderId}">
            <div class="d-flex justify-content-between align-items-center mb-3">
                <div>
                    <h4 class="mb-0"># ${order.orderNumber} - Bàn ${order.tableNumber || 'N/A'}</h4>
                    <small class="text-muted">
                        <i class="mdi mdi-account-group"></i> ${numberOfGuests} người
                    </small>
                    ${hasUrgentPendingItems ? `
                    <div style="margin-top: 8px; color: #ef4444; font-weight: 700; font-size: 14px;">
                        <i class="mdi mdi-fire" style="margin-right: 4px;"></i> LÀM GẤP
                    </div>
                    ` : ''}
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
                    <small>${completedItems}/${order.totalItems} món đã hoàn thành</small>
                </div>
                <div class="progress" style="height: 8px;">
                    <div class="progress-bar ${canComplete ? 'bg-success' : 'bg-warning'}" 
                         style="width: ${((completedItems) / order.totalItems) * 100}%">
                    </div>
                </div>
            </div>

            <div class="item-list">
                ${sortedItems.length > 0 ? sortedItems.map(item => createItemRow(item)).join('') : '<div class="text-muted text-center p-2">Không có món nào</div>'}
            </div>

            <button class="btn-complete" 
                    onclick="event.stopPropagation(); completeOrder(${order.orderId})"
                    ${!canComplete ? 'disabled' : ''}>
                <i class="mdi mdi-check-circle"></i> Sẵn sàng
            </button>
        </div>
    `;
}

// Get status class for CSS
function getStatusClass(status) {
    if (!status) return 'status-pending';
    
    const statusLower = status.toLowerCase().trim();
    
    if (statusLower.includes('pending') || statusLower.includes('chờ') || statusLower.includes('chờ bếp'))
        return 'status-pending';
    if (statusLower.includes('cooking') || statusLower.includes('chế biến') || statusLower.includes('đang nấu'))
        return 'status-cooking';
    if (statusLower.includes('late') || statusLower.includes('trễ'))
        return 'status-late';
    if (statusLower.includes('ready') || statusLower.includes('sẵn sàng'))
        return 'status-ready';
    if (statusLower.includes('done') || statusLower.includes('hoàn thành') || statusLower.includes('xong'))
        return 'status-done';
    
    return 'status-pending';
}

// Create single item row HTML
function createItemRow(item) {
    const statusClass = getStatusClass(item.status);

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

// Get Vietnamese status text - Đồng bộ format
function getStatusText(status) {
    if (!status) return 'CHỜ';
    
    const statusLower = status.toLowerCase().trim();
    
    // Xử lý cả tiếng Anh và tiếng Việt - Format thống nhất
    if (statusLower.includes('pending') || statusLower.includes('chờ') || statusLower.includes('chờ bếp'))
        return 'CHỜ';
    if (statusLower.includes('cooking') || statusLower.includes('chế biến') || statusLower.includes('đang nấu'))
        return 'ĐANG NẤU';
    if (statusLower.includes('late') || statusLower.includes('trễ'))
        return 'TRỄ';
    if (statusLower.includes('ready') || statusLower.includes('sẵn sàng'))
        return 'SẴN SÀNG';
    if (statusLower.includes('done') || statusLower.includes('hoàn thành') || statusLower.includes('xong'))
        return 'HOÀN THÀNH';
    if (statusLower.includes('cancelled') || statusLower.includes('hủy') || statusLower.includes('đã hủy'))
        return 'ĐÃ HỦY';
    if (statusLower.includes('served') || statusLower.includes('đã phục vụ'))
        return 'ĐÃ PHỤC VỤ';
    if (statusLower.includes('returnrequested') || statusLower.includes('yêu cầu trả món'))
        return 'YÊU CẦU TRẢ MÓN';
    if (statusLower.includes('returned') || statusLower.includes('đã trả món'))
        return 'ĐÃ TRẢ MÓN';
    
    return status.toUpperCase();
}

// Complete order - ✅ SỬA: Chuyển tất cả items sang Ready thay vì Done
async function completeOrder(orderId) {
    const confirmed = await showConfirmPopup('Xác nhận đánh dấu tất cả món trong đơn này là sẵn sàng?');
    if (!confirmed) {
        return;
    }

    try {
        // Tìm order trong currentOrders
        const order = currentOrders.find(o => o.orderId === orderId);
        if (!order || !order.items || order.items.length === 0) {
            showError('Không tìm thấy đơn hàng hoặc đơn không có món nào');
            return;
        }

        // Chuyển tất cả items có status Cooking hoặc Late sang Ready
        const itemsToMarkReady = order.items
            .filter(item => {
                const status = (item.status || 'Pending').trim();
                return status === 'Cooking' || status === 'Đang nấu' || status === 'Late' || status === 'Trễ';
            })
            .map(item => item.orderDetailId);

        if (itemsToMarkReady.length === 0) {
            showError('Không có món nào đang nấu hoặc trễ để đánh dấu sẵn sàng');
            return;
        }

        // Gọi API để chuyển từng item sang Ready
        const promises = itemsToMarkReady.map(orderDetailId =>
            updateItemStatusAPI(orderDetailId, 'Ready')
        );

        await Promise.all(promises);
        showSuccess(`Đã đánh dấu ${itemsToMarkReady.length} món sẵn sàng!`);
        reloadCurrentView();
        
        // Tự động reload đơn vừa sẵn sàng nếu đang hiển thị
        const completedColumn = document.getElementById('completedOrdersColumn');
        if (completedColumn && !completedColumn.classList.contains('hidden')) {
            loadRecentlyFulfilledOrders();
        }
    } catch (error) {
        console.error('Error marking order as ready:', error);
        showError('Không thể đánh dấu sẵn sàng: ' + error.message);
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
        return Promise.resolve();
    }

    const grid = document.getElementById('ordersGrid');
    if (!grid) return Promise.resolve();

    try {
        // Create abort controller for timeout
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 10000);
        
        // ✅ THÊM: Gửi statusFilter lên backend thay vì filter ở frontend
        const url = currentStatusFilter !== 'all' 
            ? `${API_BASE}/KitchenDisplay/active-orders?statusFilter=${encodeURIComponent(currentStatusFilter)}`
            : `${API_BASE}/KitchenDisplay/active-orders`;
        
        const response = await fetch(url, {
            signal: controller.signal
        });
        
        clearTimeout(timeoutId);
        
        // Check if we're still in the correct view mode after fetch
        if (currentViewMode !== 'theo-ban') {
            return Promise.resolve();
        }

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const result = await response.json();

        if (result.success) {
            const orders = result.data;
            // ✅ Backend đã filter Done items rồi, không cần filter ở frontend nữa
            // Lưu orders vào currentOrders để modal có thể tìm thấy
            currentOrders = orders;
            
            // ✅ SỬA: Sắp xếp tất cả orders theo thời gian order (CreatedAt), không group theo bàn
            const sortedOrders = orders.sort((a, b) => {
                const timeA = new Date(a.createdAt || 0).getTime();
                const timeB = new Date(b.createdAt || 0).getTime();
                return timeA - timeB; // Sắp xếp từ cũ đến mới
            });
            
            // Double check view mode before rendering
            if (currentViewMode === 'theo-ban') {
                renderOrdersByTable(sortedOrders);
                updateOrderCount(sortedOrders.length);
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
                completedItems: 0,
                lateItems: 0,
                readyItems: 0
            };
        }
        grouped[tableKey].orders.push(order);
        grouped[tableKey].totalItems += order.totalItems || 0;
        grouped[tableKey].completedItems += order.completedItems || 0;
        grouped[tableKey].lateItems += order.lateItems || 0;
        grouped[tableKey].readyItems += order.readyItems || 0;
    });
    
    return Object.values(grouped);
}

// Render orders sorted by time (not grouped by table)
function renderOrdersByTable(orders) {
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
    
    // Tính tổng số đơn và số món đã hoàn thành của TẤT CẢ orders
    const totalOrders = orders.length;
    const totalItems = orders.reduce((sum, order) => sum + (order.totalItems || 0), 0);
    const totalCompletedItems = orders.reduce((sum, order) => sum + (order.completedItems || 0), 0);
    const totalLateItems = orders.reduce((sum, order) => sum + (order.lateItems || 0), 0);
    
    // Render từng order card
    const renderedOrders = orders.map(order => createOrderCard(order)).filter(html => html.trim() !== '').join('');
    
    if (renderedOrders.trim() === '') {
        grid.innerHTML = `
            <div class="empty-state">
                <i class="mdi mdi-filter-off" style="font-size: 48px;"></i>
                <p class="mt-3">Không có món nào với trạng thái "${getStatusText(currentStatusFilter)}"</p>
            </div>
        `;
        return;
    }
    
    // ✅ Tạo header tổng hợp cho TẤT CẢ orders - đặt ở trên cùng, chiếm toàn bộ chiều rộng
    const summaryHeader = `
        <div class="table-group-header" style="background: #f5f5f5; padding: 15px; border-radius: 8px; margin-bottom: 15px; grid-column: 1 / -1;">
            <h3 style="margin: 0; display: flex; align-items: center; gap: 10px; flex-wrap: wrap;">
                <i class="mdi mdi-silverware-fork-knife"></i> 
                <span>${totalOrders} đơn | ${totalCompletedItems}/${totalItems} món đã hoàn thành</span>
                ${totalLateItems > 0 ? `<span style="color: #dc3545; margin-left: 10px;"><i class="mdi mdi-alert-circle"></i> Món đã trễ: ${totalLateItems}</span>` : ''}
                ${totalCompletedItems > 0 ? `<span style="color: #28a745; margin-left: 10px;"><i class="mdi mdi-check-circle"></i> Món đã hoàn thành: ${totalCompletedItems}</span>` : ''}
            </h3>
        </div>
    `;
    
    grid.innerHTML = summaryHeader + renderedOrders;
    
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
        // ✅ SỬA: Backend đã tính completedItems = Ready + Done
        const completedItems = order.completedItems || 0; // Backend trả về readyCount + doneCount
        const canComplete = completedItems === order.totalItems;
        
        // ✅ Backend đã filter Done items và sort by course type rồi, không cần làm ở frontend nữa
        let sortedItems = order.items || [];
        
        // Nếu không có items sau khi filter, không render order card này
        if (sortedItems.length === 0) {
            return '';
        }
        
        const numberOfGuests = order.numberOfGuests || 0;
        
        // Kiểm tra xem có món nào làm gấp VÀ đang ở trạng thái Pending (chờ bếp xác nhận) không
        // Chỉ hiển thị "LÀM GẤP" nếu còn món làm gấp đang chờ, không hiển thị nếu đã chuyển sang Cooking/Ready
        const hasUrgentPendingItems = sortedItems.some(item => {
            const isUrgent = item.isUrgent === true || item.IsUrgent === true;
            if (!isUrgent) return false;
            
            // Kiểm tra status là Pending (chờ bếp xác nhận)
            const status = (item.status || '').toLowerCase().trim();
            const isPending = status.includes('pending') || status.includes('chờ') || status.includes('chờ bếp');
            return isPending;
        });
        
        return `
            <div class="order-card ${hasUrgentPendingItems ? 'has-urgent' : ''}" data-order-id="${order.orderId}" style="margin-bottom: 15px;">
                <div class="d-flex justify-content-between align-items-center mb-3">
                    <div>
                        <h4 class="mb-0"># ${order.orderNumber} - Bàn ${order.tableNumber || 'N/A'}</h4>
                        <small class="text-muted">
                            <i class="mdi mdi-account-group"></i> ${numberOfGuests} người
                        </small>
                        ${hasUrgentPendingItems ? `
                        <div style="margin-top: 8px; color: #ef4444; font-weight: 700; font-size: 14px;">
                            <i class="mdi mdi-fire" style="margin-right: 4px;"></i> LÀM GẤP
                        </div>
                        ` : ''}
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
                        <small>${completedItems}/${order.totalItems} món đã hoàn thành</small>
                    </div>
                    <div class="progress" style="height: 8px;">
                        <div class="progress-bar ${canComplete ? 'bg-success' : 'bg-warning'}" 
                             style="width: ${((completedItems) / order.totalItems) * 100}%">
                        </div>
                    </div>
                </div>
                
                <div class="item-list">
                    ${sortedItems.length > 0 ? sortedItems.map(item => createItemRow(item)).join('') : '<div class="text-muted text-center p-2">Không có món nào</div>'}
                </div>
                
                <button class="btn-complete" 
                        onclick="event.stopPropagation(); completeOrder(${order.orderId})"
                        ${!canComplete ? 'disabled' : ''}>
                    <i class="mdi mdi-check-circle"></i> Sẵn sàng
                </button>
            </div>
        `;
    }).filter(html => html.trim() !== '').join(''); // Remove empty strings
    
    // Nếu không có orders nào có items sau khi filter, không render table group
    if (allOrdersHtml.trim() === '') {
        return '';
    }
    
    // Đếm lại số orders và items sau khi filter
    const filteredOrders = group.orders.filter(order => {
        const sortedItems = sortItemsByCourseType(order.items || []);
        const filteredItems = currentStatusFilter !== 'all' 
            ? sortedItems.filter(item => item.status === currentStatusFilter)
            : sortedItems;
        return filteredItems.length > 0;
    });
    
    return `
        <div class="table-group-container" style="margin-bottom: 30px;">
            <div class="table-group-header" style="background: #f5f5f5; padding: 15px; border-radius: 8px; margin-bottom: 15px;">
                <h3 style="margin: 0; display: flex; align-items: center; gap: 10px; flex-wrap: wrap;">
                    <i class="mdi mdi-table"></i> 
                    <span>${filteredOrders.length} đơn | ${group.completedItems || 0}/${group.totalItems} món đã hoàn thành</span>
                    ${group.lateItems > 0 ? `<span style="color: #dc3545; margin-left: 10px;"><i class="mdi mdi-alert-circle"></i> Món đã trễ: ${group.lateItems}</span>` : ''}
                    ${group.completedItems > 0 ? `<span style="color: #28a745; margin-left: 10px;"><i class="mdi mdi-check-circle"></i> Món đã hoàn thành: ${group.completedItems}</span>` : ''}
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
        return Promise.resolve();
    }

    const grid = document.getElementById('ordersGrid');
    if (!grid) return Promise.resolve();

    try {
        // Create abort controller for timeout
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 10000);
        
        // ✅ THÊM: Gửi statusFilter lên backend thay vì filter ở frontend
        const url = currentStatusFilter !== 'all' 
            ? `${API_BASE}/KitchenDisplay/grouped-by-item?statusFilter=${encodeURIComponent(currentStatusFilter)}`
            : `${API_BASE}/KitchenDisplay/grouped-by-item`;
        
        console.log('[loadGroupedItems] API Base URL:', API_BASE);
        console.log('[loadGroupedItems] Full URL:', url);
        console.log('[loadGroupedItems] Status Filter:', currentStatusFilter);
        
        const response = await fetch(url, {
            signal: controller.signal
        });
        
        clearTimeout(timeoutId);
        
        // Check if we're still in the correct view mode after fetch
        if (currentViewMode !== 'theo-tung-mon') {
            return Promise.resolve();
        }
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        
        const result = await response.json();

        if (result.success && result.data) {
            // ✅ Backend đã filter Done items rồi, không cần filter ở frontend nữa
            currentGroupedItems = result.data;
            console.log('[loadGroupedItems] Received data:', currentGroupedItems.length, 'items');
            
            // Double check view mode before rendering
            if (currentViewMode === 'theo-tung-mon') {
                console.log('[loadGroupedItems] Rendering with currentGroupedItems:', currentGroupedItems.length);
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

    // ✅ Backend đã filter theo status rồi, hiển thị tất cả items (kể cả Ready và Done)
    let filteredItems = groupedItems;

    if (filteredItems.length === 0) {
        grid.innerHTML = `
            <div class="empty-state">
                <i class="mdi mdi-filter-off" style="font-size: 48px;"></i>
                <p class="mt-3">Không có món nào với trạng thái "${getStatusText(currentStatusFilter)}"</p>
            </div>
        `;
        return;
    }

    const sortedItems = sortGroupedItems(filteredItems);

    grid.innerHTML = sortedItems.map(item => createItemCard(item)).join('');
    
    // Attach click handlers for "Bắt đầu nấu" buttons
    setTimeout(() => {
        const startCookButtons = grid.querySelectorAll('.btn-start-cook');
        startCookButtons.forEach((button, index) => {
            button.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                const itemDataJson = this.getAttribute('data-item-data');
                if (itemDataJson) {
                    try {
                        const itemData = JSON.parse(decodeURIComponent(itemDataJson));
                        startCookingForItem(itemData);
                    } catch (e) {
                        console.error('[Button Click] Error parsing item data:', e);
                        // Fallback to old method
                        const orderDetailIds = JSON.parse(this.getAttribute('data-order-detail-ids') || '[]');
                        if (orderDetailIds.length > 0) {
                            startCookingForItem({ itemDetails: orderDetailIds.map(id => ({ orderDetailId: id })) });
                        }
                    }
                } else {
                    // Fallback to old method
                    const orderDetailIds = JSON.parse(this.getAttribute('data-order-detail-ids') || '[]');
                    if (orderDetailIds.length > 0) {
                        startCookingForItem({ itemDetails: orderDetailIds.map(id => ({ orderDetailId: id })) });
                    }
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

    // Get pending item details for batch selection
    const pendingItemDetails = item.itemDetails
        .filter(detail => detail.status === 'Pending' || !detail.status);

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

    // Store item data for batch selection popup
    // Convert batchSize to number, use null if not available
    let batchSizeValue = null;
    if (item.batchSize !== null && item.batchSize !== undefined && item.batchSize !== '') {
        const numValue = Number(item.batchSize);
        if (!isNaN(numValue) && numValue > 0) {
            batchSizeValue = numValue;
        }
    }
    
    const itemData = {
        menuItemId: item.menuItemId,
        menuItemName: item.menuItemName,
        batchSize: batchSizeValue,
        itemDetails: pendingItemDetails
    };

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
                        data-item-data="${encodeURIComponent(JSON.stringify(itemData))}"
                        data-batch-size="${itemData.batchSize || ''}"
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
    const statusClass = getStatusClass(detail.status);
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

// Filter by view mode (theo-ban, theo-tung-mon)
function filterByViewMode(type) {
    // Prevent multiple rapid clicks
    if (currentViewMode === type) {
        return;
    }

    currentViewMode = type;

    // Update view mode buttons
    document.querySelectorAll('.view-mode-filters .btn').forEach(btn => {
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

// Filter by item status (Pending, Cooking, Late, Ready, Done)
function filterByItemStatus(status) {
    
    currentStatusFilter = status;

    // Update status filter buttons
    document.querySelectorAll('.status-filters .btn').forEach(btn => {
        btn.classList.remove('active');
    });

    const buttonMap = {
        'all': 'filter-status-all',
        'Pending': 'filter-status-pending',
        'Cooking': 'filter-status-cooking',
        'Late': 'filter-status-late',
        'Ready': 'filter-status-ready',
        'Done': 'filter-status-done'
    };

    const activeButton = document.getElementById(buttonMap[status]);
    if (activeButton) {
        activeButton.classList.add('active');
    }

    // Reload current view with new filter
    if (currentViewMode === 'theo-tung-mon') {
        loadGroupedItems();
    } else {
        loadOrdersByTable();
    }
}

// ===========================
// MODAL MANAGEMENT - FIXED
// ===========================

let currentModalOrder = null;
// selectedModalItems: Map<orderDetailId, quantity> - lưu số lượng đã chọn cho mỗi món
let selectedModalItems = new Map();

// Create modal dynamically - FIXED VERSION
function createModalIfNotExists() {
    let modalOverlay = document.getElementById('orderModalOverlay');

    if (!modalOverlay) {
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
                    <button class="btn-modal btn-modal-unfulfill" onclick="unfulfillSelectedItems()">Hủy sẵn sàng</button>
                    <button class="btn-modal btn-modal-fulfill" onclick="fulfillSelectedItems()">Sẵn sàng</button>
                </div>
            </div>
        `;

        document.body.appendChild(modalOverlay);

        modalOverlay.addEventListener('click', function (e) {
            if (e.target === modalOverlay) {
                closeOrderModal();
            }
        });
    }
}

// Open order modal - FIXED VERSION
async function openOrderModal(orderId) {
    orderId = parseInt(orderId);
    if (isNaN(orderId)) {
        console.error('Invalid orderId');
        showError('ID đơn hàng không hợp lệ');
        return;
    }

    // Fetch order details with all items (including Done) from API
    try {
        const response = await fetch(`${API_BASE}/KitchenDisplay/order-details/${orderId}`);
        const result = await response.json();
        
        if (!result.success || !result.data) {
            showError('Không tìm thấy đơn hàng');
            return;
        }

        const order = result.data;
        currentModalOrder = order;
        selectedModalItems.clear(); // Clear Map

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
        }
    } catch (error) {
        console.error('Error loading order details:', error);
        showError('Lỗi khi tải chi tiết đơn hàng');
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

    // Hiển thị tất cả items được truyền vào (bao gồm cả Done/Ready nếu có)
    // Sử dụng tham số items được truyền vào, không dùng currentModalOrder.items
    const cookableItems = items || [];

    itemsList.innerHTML = cookableItems.map(item => {
        const itemId = item.orderDetailId;
        const selectedQuantity = selectedModalItems.get(itemId) || 0;
        const isSelected = selectedQuantity > 0;
        const itemQuantity = item.quantity || 1;
        
        // Kiểm tra xem món đã Done chưa
        const status = (item.status || '').toLowerCase().trim();
        const isDone = status.includes('done') || status.includes('hoàn thành') || status.includes('xong');
        const isReady = status.includes('ready') || status.includes('sẵn sàng');
        const isDisabled = isDone; // ✅ Chỉ disable Done, Ready vẫn cho phép chọn (để hủy sẵn sàng)

        return `
            <li class="order-modal-item ${isSelected ? 'selected' : ''} ${isDisabled ? 'disabled' : ''}" 
                data-item-id="${itemId}">
                <div style="display: flex; align-items: center; gap: 12px; width: 100%;">
                    <input type="checkbox" 
                           ${isSelected ? 'checked' : ''} 
                           ${isDisabled ? 'disabled' : ''}
                           onchange="toggleModalItemCheckbox(${itemId}, ${itemQuantity}, event)"
                           onclick="event.stopPropagation()">
                    <span class="order-modal-item-text" style="flex: 1; ${isDisabled ? 'opacity: 0.6;' : ''}">
                        <strong>${item.menuItemName}</strong>
                        ${isDone ? '<span style="color: #1b5e20; font-size: 12px; margin-left: 8px; font-weight: 600;">(Đã hoàn thành)</span>' : ''}
                        ${isReady && !isDone ? '<span style="color: #2e7d32; font-size: 12px; margin-left: 8px;">(Sẵn sàng)</span>' : ''}
                        ${item.specialInstructions || item.notes ?
                        `<span style="color: #d32f2f; font-size: 14px; display: block; margin-top: 4px;"> (${item.specialInstructions || item.notes})</span>` : ''}
                    </span>
                    <div style="display: flex; align-items: center; gap: 4px;">
                        <input type="number" 
                               class="modal-quantity-input" 
                               data-order-detail-id="${itemId}"
                               min="0" 
                               max="${itemQuantity}" 
                               value="${selectedQuantity}"
                               ${isDisabled ? 'disabled' : ''}
                               style="width: 50px; padding: 4px 8px; border: 1px solid #ddd; border-radius: 4px; text-align: center; ${isDisabled ? 'opacity: 0.6; background: #f5f5f5;' : ''}"
                               onchange="updateModalQuantity(${itemId}, this.value, ${itemQuantity}, event)"
                               onclick="event.stopPropagation()">
                        <span style="color: #999; font-size: 14px;">/ ${itemQuantity}</span>
                    </div>
                </div>
            </li>
        `;
    }).join('');
}

// Update modal quantity when user changes input
function updateModalQuantity(orderDetailId, newValue, maxQuantity, event) {
    if (event) {
        event.stopPropagation();
    }
    
    const quantity = parseInt(newValue) || 0;
    const maxQty = parseInt(maxQuantity) || 0;
    
    // Validate quantity
    let validQuantity = quantity;
    if (validQuantity < 0) {
        validQuantity = 0;
    }
    if (validQuantity > maxQty) {
        validQuantity = maxQty;
    }
    
    // Update the input value if it was corrected
    if (event && event.target) {
        event.target.value = validQuantity;
    }
    
    // Update selectedModalItems Map
    if (validQuantity > 0) {
        selectedModalItems.set(orderDetailId, validQuantity);
    } else {
        selectedModalItems.delete(orderDetailId);
    }
    
    // Update checkbox state
    const checkbox = document.querySelector(`input[type="checkbox"][onchange*="${orderDetailId}"]`);
    if (checkbox) {
        checkbox.checked = validQuantity > 0;
    }
    
    // Update item row selected state
    const itemRow = document.querySelector(`.order-modal-item[data-item-id="${orderDetailId}"]`);
    if (itemRow) {
        if (validQuantity > 0) {
            itemRow.classList.add('selected');
        } else {
            itemRow.classList.remove('selected');
        }
    }
}

// Toggle modal item checkbox
function toggleModalItemCheckbox(orderDetailId, maxQuantity, event) {
    if (event) {
        event.stopPropagation();
    }
    
    const checkbox = event?.target;
    const isChecked = checkbox?.checked || false;
    
    // Find the quantity input for this order detail
    const quantityInput = document.querySelector(`.modal-quantity-input[data-order-detail-id="${orderDetailId}"]`);
    const maxQty = parseInt(maxQuantity) || 0;
    
    if (quantityInput) {
        if (isChecked) {
            // If checked, set quantity to max
            const quantity = maxQty > 0 ? maxQty : 1;
            quantityInput.value = quantity;
            updateModalQuantity(orderDetailId, quantity, maxQty, null);
        } else {
            // If unchecked, set quantity to 0
            quantityInput.value = 0;
            updateModalQuantity(orderDetailId, 0, maxQty, null);
        }
    }
}

// Toggle item selection (legacy - kept for compatibility)
function toggleModalItemSelection(itemId, event) {
    if (event) {
        event.stopPropagation();
    }

    const item = currentModalOrder?.items?.find(i => i.orderDetailId === itemId);
    if (!item) return;

    const maxQty = item.quantity || 1;
    const currentQty = selectedModalItems.get(itemId) || 0;
    
    if (currentQty > 0) {
        // Deselect
        updateModalQuantity(itemId, 0, maxQty, null);
    } else {
        // Select with max quantity
        updateModalQuantity(itemId, maxQty, maxQty, null);
    }
}

// Select all items
function selectAllItems() {
    if (!currentModalOrder) return;

    // Hiển thị tất cả items, nhưng chỉ chọn các món có thể nấu (không chọn Done, nhưng cho phép Ready)
    const allItems = currentModalOrder.items || [];
    const cookableItems = allItems.filter(item => {
        const status = (item.status || '').toLowerCase().trim();
        const isDone = status.includes('done') || status.includes('hoàn thành') || status.includes('xong');
        return !isDone; // ✅ Chỉ bỏ Done, Ready vẫn cho phép chọn (để hủy sẵn sàng)
    });

    const allSelected = cookableItems.length > 0 && cookableItems.every(item => {
        const qty = selectedModalItems.get(item.orderDetailId) || 0;
        return qty > 0;
    });

    if (allSelected) {
        // Deselect all
        selectedModalItems.clear();
    } else {
        // Select all cookable items with max quantity
        cookableItems.forEach(item => {
            const maxQty = item.quantity || 1;
            selectedModalItems.set(item.orderDetailId, maxQty);
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
    // Lấy giá trị trực tiếp từ các input để đảm bảo lấy đúng số lượng đã chỉnh sửa
    const selectedItems = [];
    const quantityInputs = document.querySelectorAll('.modal-quantity-input');
    
    quantityInputs.forEach(input => {
        const orderDetailId = parseInt(input.getAttribute('data-order-detail-id'));
        const quantity = parseInt(input.value) || 0;
        const maxQuantity = parseInt(input.getAttribute('max')) || 0;
        
        if (quantity > 0 && quantity <= maxQuantity) {
            selectedItems.push({ orderDetailId, quantity });
        }
    });

    if (selectedItems.length === 0) {
        showError('Vui lòng chọn ít nhất một món');
        return;
    }

    try {
        const promises = [];
        const totalQuantity = selectedItems.reduce((sum, item) => sum + item.quantity, 0);
        
        selectedItems.forEach(({ orderDetailId, quantity }) => {
            // Tìm detail để lấy tổng số lượng
            const detail = currentModalOrder?.items?.find(d => d.orderDetailId === orderDetailId);
            const totalQty = detail?.quantity || quantity;
            
            // Nếu quantity < totalQuantity, gọi API split
            if (quantity < totalQty) {
                promises.push(startCookingWithQuantityAPI(orderDetailId, quantity));
            } else {
                // Nếu quantity = totalQuantity, chỉ cần update status
                promises.push(updateItemStatusAPI(orderDetailId, 'Cooking'));
            }
        });

        await Promise.all(promises);
        showSuccess(`Đã bắt đầu nấu ${totalQuantity} món (${selectedItems.length} đơn)`);
        selectedModalItems.clear();
        reloadCurrentView();
        closeOrderModal();
    } catch (error) {
        console.error('Error starting cooking:', error);
        showError('Không thể bắt đầu nấu: ' + error.message);
    }
}

// Start cooking for all items in a grouped item card
async function startCookingForItem(itemData) {
    if (!itemData || !itemData.itemDetails || itemData.itemDetails.length === 0) {
        showError('Không có món nào để bắt đầu nấu');
        return;
    }

    // Show batch selection popup if batchSize is defined and > 0
    // Check for null, undefined, or 0
    const hasBatchSize = itemData.batchSize !== null && 
                         itemData.batchSize !== undefined && 
                         !isNaN(Number(itemData.batchSize)) && 
                         Number(itemData.batchSize) > 0;

    if (hasBatchSize) {
        const selectedItems = await showBatchSelectionPopup(itemData);
        if (!selectedItems || selectedItems.length === 0) {
            return; // User cancelled or didn't select any items
        }

        try {
            // selectedItems is now array of {orderDetailId, quantity}
            const totalQuantity = selectedItems.reduce((sum, item) => sum + item.quantity, 0);
            const promises = [];
            
            selectedItems.forEach(({ orderDetailId, quantity }) => {
                if (quantity > 0) {
                    // Tìm detail để lấy tổng số lượng
                    const detail = itemData.itemDetails.find(d => d.orderDetailId === orderDetailId);
                    const totalQty = detail?.quantity || quantity;
                    
                    // Nếu quantity < totalQuantity, gọi API split
                    if (quantity < totalQty) {
                        promises.push(startCookingWithQuantityAPI(orderDetailId, quantity));
                    } else {
                        // Nếu quantity = totalQuantity, chỉ cần update status
                        promises.push(updateItemStatusAPI(orderDetailId, 'Cooking'));
                    }
                }
            });

            await Promise.all(promises);
            showSuccess(`Đã bắt đầu nấu ${totalQuantity} món (${selectedItems.length} đơn)`);
            reloadCurrentView();
        } catch (error) {
            console.error('Error starting cooking:', error);
            showError('Không thể bắt đầu nấu: ' + error.message);
        }
    } else {
        // No batch size, use simple confirmation
        const orderDetailIds = itemData.itemDetails.map(d => d.orderDetailId);
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
}

// Show batch selection popup
function showBatchSelectionPopup(itemData) {
    return new Promise((resolve) => {
        const overlay = document.createElement('div');
        overlay.className = 'confirm-popup-overlay';
        
        const batchSize = itemData.batchSize || 1;
        const menuItemName = itemData.menuItemName || 'Món ăn';
        const itemDetails = itemData.itemDetails || [];
        
        // Initialize selected items with quantities - try to fill batch automatically
        // selectedQuantities: Map<orderDetailId, quantity>
        const selectedQuantities = new Map();
        let currentBatchQuantity = 0;
        
        // Auto-select items to fill batch
        for (const detail of itemDetails) {
            if (currentBatchQuantity + detail.quantity <= batchSize) {
                selectedQuantities.set(detail.orderDetailId, detail.quantity);
                currentBatchQuantity += detail.quantity;
            }
            if (currentBatchQuantity >= batchSize) {
                break;
            }
        }
        
        // If batch not filled, select first item at least
        if (selectedQuantities.size === 0 && itemDetails.length > 0) {
            const firstDetail = itemDetails[0];
            const firstQuantity = Math.min(firstDetail.quantity, batchSize);
            selectedQuantities.set(firstDetail.orderDetailId, firstQuantity);
            currentBatchQuantity = firstQuantity;
        }
        
        const updateSelectedQuantity = () => {
            let total = 0;
            selectedQuantities.forEach((quantity, orderDetailId) => {
                total += quantity;
            });
            return total;
        };
        
        const renderPopup = () => {
            const selectedQuantity = updateSelectedQuantity();
            const isBatchFull = selectedQuantity >= batchSize;
            const batchStatusClass = isBatchFull ? 'batch-full' : 'batch-incomplete';
            const batchStatusText = isBatchFull 
                ? `✓ Đủ mẻ (${selectedQuantity}/${batchSize})` 
                : `Chưa đủ mẻ (${selectedQuantity}/${batchSize})`;
            
            overlay.innerHTML = `
                <div class="confirm-popup batch-selection-popup" onclick="event.stopPropagation()">
                    <div class="confirm-popup-header">
                        <div class="confirm-popup-icon">
                            <i class="mdi mdi-chef-hat"></i>
                        </div>
                        <h3 class="confirm-popup-title">Bắt đầu nấu: ${menuItemName}</h3>
                    </div>
                    <div class="confirm-popup-body" style="max-height: 60vh; overflow-y: auto;">
                        <div style="margin-bottom: 20px; padding: 15px; background: #f5f5f5; border-radius: 8px;">
                            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px;">
                                <strong style="font-size: 16px;">Số lượng mỗi mẻ:</strong>
                                <span style="font-size: 18px; font-weight: 600; color: #2196F3;">${batchSize}</span>
                            </div>
                            <div style="display: flex; justify-content: space-between; align-items: center;">
                                <strong>Đã chọn:</strong>
                                <span class="batch-status ${batchStatusClass}" style="font-weight: 600; font-size: 16px;">
                                    ${batchStatusText}
                                </span>
                            </div>
                        </div>
                        
                        <div style="margin-bottom: 15px;">
                            <strong style="display: block; margin-bottom: 10px; color: #333;">Chọn số lượng nấu cho từng đơn:</strong>
                            <div class="batch-items-list" style="border: 1px solid #ddd; border-radius: 8px; overflow: hidden;">
                                ${itemDetails.map((detail, index) => {
                                    const selectedQty = selectedQuantities.get(detail.orderDetailId) || 0;
                                    const isSelected = selectedQty > 0;
                                    return `
                                        <div class="batch-item-row ${isSelected ? 'selected' : ''}" 
                                             style="padding: 12px 15px; border-bottom: 1px solid #eee; transition: background 0.2s;">
                                            <div style="display: flex; align-items: center; gap: 12px;">
                                                <input type="checkbox" 
                                                       class="batch-item-checkbox" 
                                                       data-order-detail-id="${detail.orderDetailId}"
                                                       ${isSelected ? 'checked' : ''}
                                                       onchange="toggleBatchItemCheckbox(${detail.orderDetailId}, event)"
                                                       onclick="event.stopPropagation()">
                                                <div style="flex: 1;">
                                                    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px;">
                                                        <div>
                                                            <strong style="color: #2196F3;">Đơn ${detail.orderNumber || detail.orderId}</strong>
                                                            <span style="color: #666; font-size: 14px; margin-left: 8px;">Bàn ${detail.tableNumber || 'N/A'}</span>
                                                        </div>
                                                    </div>
                                                    <div style="display: flex; justify-content: space-between; align-items: center; gap: 12px;">
                                                        <div style="display: flex; align-items: center; gap: 8px;">
                                                            <span style="color: #666; font-size: 14px;">Số lượng đơn:</span>
                                                            <strong style="color: #333;">${detail.quantity}</strong>
                                                        </div>
                                                        <div style="display: flex; align-items: center; gap: 8px;">
                                                            <span style="color: #666; font-size: 14px;">Số lượng nấu:</span>
                                                            <input type="number" 
                                                                   class="batch-quantity-input" 
                                                                   data-order-detail-id="${detail.orderDetailId}"
                                                                   min="0" 
                                                                   max="${detail.quantity}" 
                                                                   value="${selectedQty}"
                                                                   style="width: 70px; padding: 4px 8px; border: 1px solid #ddd; border-radius: 4px; text-align: center;"
                                                                   onchange="updateBatchQuantity(${detail.orderDetailId}, this.value, ${detail.quantity}, event)"
                                                                   onclick="event.stopPropagation()">
                                                            <span style="color: #999; font-size: 12px;">/ ${detail.quantity}</span>
                                                        </div>
                                                        ${detail.notes ? `<span style="color: #ff9800; font-size: 13px;"><i class="mdi mdi-note-text"></i> ${detail.notes}</span>` : ''}
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    `;
                                }).join('')}
                            </div>
                        </div>
                    </div>
                    <div class="confirm-popup-footer">
                        <button class="confirm-popup-btn confirm-popup-btn-cancel">Hủy</button>
                        <button class="confirm-popup-btn confirm-popup-btn-confirm ${isBatchFull ? '' : 'btn-warning'}" 
                                ${!isBatchFull ? 'title="Mẻ chưa đủ, bạn có muốn tiếp tục?"' : ''}>
                            ${isBatchFull ? 'Xác nhận bắt đầu nấu' : 'Bắt đầu nấu (chưa đủ mẻ)'}
                        </button>
                    </div>
                </div>
            `;
            
            // Attach event listeners
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
            
            confirmBtn.addEventListener('click', () => {
                // Return selected items with quantities: [{orderDetailId, quantity}, ...]
                // Lấy giá trị trực tiếp từ các input để đảm bảo lấy đúng số lượng đã chỉnh sửa
                const selectedItems = [];
                const quantityInputs = overlay.querySelectorAll('.batch-quantity-input');
                
                quantityInputs.forEach(input => {
                    const orderDetailId = parseInt(input.getAttribute('data-order-detail-id'));
                    const quantity = parseInt(input.value) || 0;
                    const maxQuantity = parseInt(input.getAttribute('max')) || 0;
                    
                    if (quantity > 0 && quantity <= maxQuantity) {
                        selectedItems.push({ orderDetailId, quantity });
                    }
                });
                
                close(selectedItems.length > 0 ? selectedItems : null);
            });
            
            cancelBtn.addEventListener('click', () => close(null));
            
            overlay.addEventListener('click', (e) => {
                if (e.target === overlay) {
                    close(null);
                }
            });
            
            // Store overlay reference for toggleBatchItem
            window.currentBatchOverlay = overlay;
            window.currentBatchSelectedQuantities = selectedQuantities;
            window.currentBatchItemDetails = itemDetails;
            window.currentBatchSize = batchSize;
            window.currentBatchRender = renderPopup;
        };
        
        renderPopup();
        document.body.appendChild(overlay);
        
        // Add CSS if not already added
        if (!document.getElementById('batch-selection-styles')) {
            const style = document.createElement('style');
            style.id = 'batch-selection-styles';
            style.textContent = `
                .batch-selection-popup {
                    max-width: 600px;
                    width: 90%;
                }
                .batch-item-row {
                    background: #fff;
                }
                .batch-item-row:hover {
                    background: #f9f9f9;
                }
                .batch-item-row.selected {
                    background: #e3f2fd;
                }
                .batch-status.batch-full {
                    color: #4caf50;
                }
                .batch-status.batch-incomplete {
                    color: #ff9800;
                }
                .batch-items-list {
                    max-height: 400px;
                    overflow-y: auto;
                }
            `;
            document.head.appendChild(style);
        }
    });
}

// Update batch quantity when user changes input
function updateBatchQuantity(orderDetailId, newValue, maxQuantity, event) {
    if (event) {
        event.stopPropagation();
    }
    
    const quantity = parseInt(newValue) || 0;
    const maxQty = parseInt(maxQuantity) || 0;
    
    // Validate quantity
    let validQuantity = quantity;
    if (validQuantity < 0) {
        validQuantity = 0;
    }
    if (validQuantity > maxQty) {
        validQuantity = maxQty;
    }
    
    // Update the input value if it was corrected
    if (event && event.target) {
        event.target.value = validQuantity;
    }
    
    // Update selectedQuantities Map
    if (window.currentBatchSelectedQuantities) {
        if (validQuantity > 0) {
            window.currentBatchSelectedQuantities.set(orderDetailId, validQuantity);
        } else {
            window.currentBatchSelectedQuantities.delete(orderDetailId);
        }
        
        // Update checkbox state
        const checkbox = document.querySelector(`.batch-item-checkbox[data-order-detail-id="${orderDetailId}"]`);
        if (checkbox) {
            checkbox.checked = validQuantity > 0;
        }
        
        // Re-render popup to update totals
        if (window.currentBatchRender) {
            window.currentBatchRender();
        }
    }
}

// Toggle batch item checkbox
function toggleBatchItemCheckbox(orderDetailId, event) {
    if (event) {
        event.stopPropagation();
    }
    
    const checkbox = event?.target;
    const isChecked = checkbox?.checked || false;
    
    // Find the quantity input for this order detail
    const quantityInput = document.querySelector(`.batch-quantity-input[data-order-detail-id="${orderDetailId}"]`);
    const maxQuantity = parseInt(quantityInput?.getAttribute('max')) || 0;
    
    if (quantityInput) {
        if (isChecked) {
            // If checked, set quantity to max (or 1 if max is 0)
            const quantity = maxQuantity > 0 ? maxQuantity : 1;
            quantityInput.value = quantity;
            updateBatchQuantity(orderDetailId, quantity, maxQuantity, null);
        } else {
            // If unchecked, set quantity to 0
            quantityInput.value = 0;
            updateBatchQuantity(orderDetailId, 0, maxQuantity, null);
        }
    }
}

// Toggle batch item selection
function toggleBatchItem(orderDetailId, event) {
    if (event) {
        event.stopPropagation();
    }
    
    if (!window.currentBatchSelectedIds || !window.currentBatchItemDetails) {
        return;
    }
    
    if (window.currentBatchSelectedIds.has(orderDetailId)) {
        window.currentBatchSelectedIds.delete(orderDetailId);
    } else {
        window.currentBatchSelectedIds.add(orderDetailId);
    }
    
    // Re-render popup to update UI
    if (window.currentBatchRender) {
        window.currentBatchRender();
    }
}

// Unfulfill selected items - ✅ SỬA: Hủy Ready thay vì Done
async function unfulfillSelectedItems() {
    // Lấy giá trị trực tiếp từ các input
    const quantityInputs = document.querySelectorAll('.modal-quantity-input');
    const selectedItems = [];
    
    quantityInputs.forEach(input => {
        const orderDetailId = parseInt(input.getAttribute('data-order-detail-id'));
        const quantity = parseInt(input.value) || 0;
        if (quantity > 0) {
            selectedItems.push({ orderDetailId, quantity });
        }
    });

    if (selectedItems.length === 0) {
        showError('Vui lòng chọn ít nhất một món');
        return;
    }

    const confirmed = await showConfirmPopup(`Xác nhận hủy sẵn sàng ${selectedItems.length} món?`);
    if (!confirmed) {
        return;
    }

    const promises = selectedItems.map(({ orderDetailId }) => {
        const item = currentModalOrder.items.find(i => 
            i.orderDetailId === orderDetailId || 
            i.OrderDetailId === orderDetailId ||
            parseInt(i.orderDetailId) === parseInt(orderDetailId) ||
            parseInt(i.OrderDetailId) === parseInt(orderDetailId)
        );
        
        if (!item) {
            console.error('Item not found:', orderDetailId, 'in items:', currentModalOrder.items);
            throw new Error(`Không tìm thấy món với ID ${orderDetailId}`);
        }
        
        // ✅ SỬA: Chuyển từ Ready về Cooking thay vì Done về Cooking
        // Kiểm tra cả tiếng Anh và tiếng Việt, cả uppercase và lowercase
        const status = (item.status || '').trim();
        const statusLower = status.toLowerCase();
        const isReady = statusLower.includes('ready') || statusLower.includes('sẵn sàng') || 
                       status === 'Ready' || status === 'Sẵn sàng';
        
        const newStatus = isReady ? 'Cooking' : item.status;
        console.log(`Hủy sẵn sàng: Item ${orderDetailId}, status: ${status} -> ${newStatus}`);
        
        return updateItemStatusAPI(orderDetailId, newStatus);
    });

    try {
        await Promise.all(promises);
        showSuccess(`Đã hủy sẵn sàng ${selectedItems.length} món`);
        selectedModalItems.clear();
        reloadCurrentView();
        closeOrderModal();

        // Nếu đang hiển thị danh sách đơn vừa sẵn sàng thì reload lại
        const completedColumn = document.getElementById('completedOrdersColumn');
        if (completedColumn && !completedColumn.classList.contains('hidden')) {
            loadRecentlyFulfilledOrders();
        }
    } catch (error) {
        showError('Không thể hủy sẵn sàng');
    }
}
// ===========================
// FULFILL FUNCTION - FIXED VERSION
// ===========================

async function fulfillSelectedItems() {
    // Lấy giá trị trực tiếp từ các input
    const quantityInputs = document.querySelectorAll('.modal-quantity-input');
    const selectedItems = [];
    
    quantityInputs.forEach(input => {
        const orderDetailId = parseInt(input.getAttribute('data-order-detail-id'));
        const quantity = parseInt(input.value) || 0;
        if (quantity > 0) {
            selectedItems.push({ orderDetailId, quantity });
        }
    });

    if (selectedItems.length === 0) {
        showError('Vui lòng chọn ít nhất một món');
        return;
    }

    if (!currentModalOrder) {
        console.error('currentModalOrder is null');
        showError('Không tìm thấy thông tin đơn hàng');
        return;
    }

    const pendingItems = [];
    const cookingItems = []; // Cooking items
    const lateItems = []; // Late items - ✅ THÊM: Cho phép Late chuyển sang Ready
    const readyItems = []; // Đã Ready rồi
    const doneItems = []; // Đã Done rồi

    selectedItems.forEach(({ orderDetailId }) => {
        const item = currentModalOrder.items.find(i => i.orderDetailId === orderDetailId);

        if (!item) {
            return;
        }

        const rawStatus = item.status;
        const status = (rawStatus || 'Pending').trim();

        // So sánh chính xác với backend format
        if (status === 'Pending' || status === 'Chờ') {
            pendingItems.push({
                orderDetailId: orderDetailId,
                name: item.menuItemName || `Món ${orderDetailId}`,
                status: status
            });
        } else if (status === 'Cooking' || status === 'Đang nấu') {
            cookingItems.push(orderDetailId);
        } else if (status === 'Late' || status === 'Trễ') {
            // ✅ THÊM: Late items cũng có thể chuyển sang Ready
            lateItems.push(orderDetailId);
        } else if (status === 'Ready' || status === 'Sẵn sàng') {
            readyItems.push({
                orderDetailId: orderDetailId,
                name: item.menuItemName || `Món ${orderDetailId}`,
                status: status
            });
        } else if (status === 'Ready' || status === 'Sẵn sàng') {
            // ✅ SỬA: Đã Ready rồi, không cần làm gì
            readyItems.push({
                orderDetailId: orderDetailId,
                name: item.menuItemName || `Món ${orderDetailId}`,
                status: status
            });
        } else if (status === 'Done' || status === 'Hoàn thành' || status === 'Xong') {
            doneItems.push({
                orderDetailId: orderDetailId,
                name: item.menuItemName || `Món ${orderDetailId}`,
                status: status
            });
        } else {
            pendingItems.push({
                orderDetailId: orderDetailId,
                name: item.menuItemName || `Món ${orderDetailId}`,
                status: status
            });
        }
    });

    // CRITICAL CHECK - Phải return nếu có pending
    if (pendingItems.length > 0) {
        const itemNames = pendingItems.map(i => i.name).join(', ');
        showError(`Các món sau chưa nấu: ${itemNames}. Vui lòng bắt đầu nấu trước!`);
        return; // ❌ DỪNG NGAY
    }

    // ✅ SỬA: Cho phép cả Cooking và Late chuyển sang Ready
    const itemsToMarkReady = [...cookingItems, ...lateItems];
    
    if (itemsToMarkReady.length === 0) {
        if (readyItems.length > 0) {
            showError('Các món đã chọn đã sẵn sàng rồi');
        } else if (doneItems.length > 0) {
            showError('Các món đã hoàn thành không thể chuyển sang sẵn sàng');
        } else {
            showError('Không có món nào đang nấu hoặc trễ để đánh dấu sẵn sàng');
        }
        return;
    }

    // ✅ SỬA: Chuyển sang Ready thay vì Done
    const promises = itemsToMarkReady.map(orderDetailId => {
        return updateItemStatusAPI(orderDetailId, 'Ready');
    });

    try {
        await Promise.all(promises);

        // Cập nhật trạng thái local cho các món vừa đánh dấu sẵn sàng
        itemsToMarkReady.forEach(orderDetailId => {
            const item = currentModalOrder.items.find(i =>
                i.orderDetailId === orderDetailId || i.OrderDetailId === orderDetailId
            );
            if (item) {
                item.status = 'Ready';
            }
        });

        showSuccess(`Đã đánh dấu ${itemsToMarkReady.length} món sẵn sàng`);
        selectedModalItems.clear();
        reloadCurrentView();
        closeOrderModal();
    } catch (error) {
        console.error('❌ Promise.all failed:', error);
        showError('Không thể đánh dấu sẵn sàng: ' + error.message);
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

        const response = await fetch(`${API_BASE}/KitchenDisplay/update-item-status`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        const responseText = await response.text();

        if (!response.ok) {

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

        if (result.success === false) {
            throw new Error(result.message || 'Update failed');
        }

        return result;
    } catch (error) {
        console.error('❌ EXCEPTION:', error.message);
        throw error;
    }
}

// Start cooking with specific quantity (split order detail if needed)
async function startCookingWithQuantityAPI(orderDetailId, quantity) {
    try {
        const payload = {
            orderDetailId: parseInt(orderDetailId),
            quantity: parseInt(quantity),
            userId: 1
        };

        const response = await fetch(`${API_BASE}/KitchenDisplay/start-cooking-with-quantity`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        const responseText = await response.text();

        if (!response.ok) {
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

        if (result.success === false) {
            throw new Error(result.message || 'Start cooking failed');
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
                    <p><strong>Số lượng người:</strong> ${currentModalOrder.numberOfGuests || 0} người</p>
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
                <h1>Phiếu món sẵn sàng</h1>
                <div class="order-info">
                    <p><strong>Đơn:</strong> #${order.orderNumber}</p>
                    <p><strong>Bàn:</strong> ${order.tableNumber || 'N/A'}</p>
                    <p><strong>Số lượng người:</strong> ${order.numberOfGuests || 0} người</p>
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
        overlay.style.zIndex = '100000'; // Đảm bảo cao hơn modal (99999)
        
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
        
        // Force reflow để đảm bảo z-index được áp dụng
        overlay.offsetHeight;

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
        gridContainer.innerHTML = `
            <div class="text-center text-muted py-3">
                <i class="mdi mdi-loading mdi-spin" style="font-size: 24px;"></i>
                <p class="mt-2">Đang tải...</p>
            </div>
        `;

        const url = `${API_BASE}/KitchenDisplay/recently-fulfilled-orders?minutesAgo=10`;
        
        const response = await fetch(url);
        
        if (!response.ok) {
            const errorText = await response.text();
            console.error('[loadRecentlyFulfilledOrders] HTTP error:', response.status, errorText);
            throw new Error(`HTTP ${response.status}: ${errorText}`);
        }

        const result = await response.json();

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

    if (!orders || orders.length === 0) {
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
    }

    let html = '';
    
    orders.forEach(order => {
        // Kiểm tra và lấy items (có thể là Items hoặc items)
        const items = order.Items || order.items || [];
        
        // Bỏ qua order không có items
        if (!items || items.length === 0) {
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