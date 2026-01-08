// Kitchen Display System JavaScript - FIXED VERSION
// File: wwwroot/js/kitchenDisplay.js

const API_BASE = window.API_BASE_URL || 'https://localhost:7096/api';
let signalRConnection = null;
let currentOrders = [];
let currentGroupedItems = [];
let currentViewMode = 'theo-ban';
let currentStatusFilter = 'all'; // 'all', 'Pending', 'Cooking', 'Late', 'Ready', 'Done'
let fallbackPollingInterval = null; // Fallback polling khi SignalR disconnect

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

            // Load ingredient shortage data
            loadIngredientShortage();

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

                // Auto-refresh ingredient shortage
                loadIngredientShortage();
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
            showSignalRBanner(true);
        });

        signalRConnection.onreconnected(() => {
            // Reconnected
            showSignalRBanner(false);
            stopFallbackPolling();
        });

        signalRConnection.onclose(() => {
            // Connection closed
            showSignalRBanner(true);
            startFallbackPolling();
        });

        // OPTIMIZED: Start connection trong background, không block
        signalRConnection.start()
            .then(() => {
                showSignalRBanner(false);
                stopFallbackPolling();
            })
            .catch(err => {
                console.error('SignalR connection error:', err);
                showSignalRBanner(true);
                startFallbackPolling();
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

// Render orders to grid - FIXED with Masonry Layout
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

    const renderedCards = orders.map(order => createOrderCard(order)).filter(html => html.trim() !== '');
    
    if (renderedCards.length === 0) {
        grid.innerHTML = `
            <div class="empty-state">
                <i class="mdi mdi-filter-off" style="font-size: 48px;"></i>
                <p class="mt-3">Không có món nào với trạng thái "${getStatusText(currentStatusFilter)}"</p>
            </div>
        `;
        return;
    }
    
    // Apply masonry layout
    applyMasonryLayout(grid, renderedCards);

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

// Apply Masonry Layout (Pinterest style)
function applyMasonryLayout(container, cardHtmls) {
    // Preserve header if exists
    const existingHeader = container.querySelector('.table-group-header');
    const headerHtml = existingHeader ? existingHeader.outerHTML : '';
    
    // Clear container (but keep header if exists)
    if (existingHeader) {
        container.innerHTML = headerHtml;
    } else {
        container.innerHTML = '';
    }
    container.classList.add('masonry');
    
    // Calculate number of columns based on container width
    const cardMinWidth = 320; // minmax(320px, 1fr)
    const gap = 20;
    const containerWidth = container.offsetWidth || window.innerWidth - 40;
    const numColumns = Math.max(1, Math.floor((containerWidth + gap) / (cardMinWidth + gap)));
    
    // Create columns
    const columns = [];
    const columnHeights = [];
    
    for (let i = 0; i < numColumns; i++) {
        const column = document.createElement('div');
        column.className = 'masonry-column';
        columns.push(column);
        columnHeights.push(0);
    }
    
    // Create columns wrapper
    const columnsWrapper = document.createElement('div');
    columnsWrapper.className = 'masonry-columns';
    columns.forEach(col => columnsWrapper.appendChild(col));
    container.appendChild(columnsWrapper);
    
    // First, render all cards invisibly to measure their heights
    const tempContainer = document.createElement('div');
    tempContainer.style.position = 'absolute';
    tempContainer.style.visibility = 'hidden';
    tempContainer.style.width = cardMinWidth + 'px';
    document.body.appendChild(tempContainer);
    
    const cardElements = [];
    cardHtmls.forEach(cardHtml => {
        const tempDiv = document.createElement('div');
        tempDiv.innerHTML = cardHtml;
        const cardElement = tempDiv.firstElementChild;
        if (cardElement) {
            tempContainer.appendChild(cardElement);
            cardElements.push(cardElement);
        }
    });
    
    // Wait for layout to calculate heights
    setTimeout(() => {
        // Now distribute cards to columns based on actual heights
        cardElements.forEach(cardElement => {
            const cardHeight = cardElement.offsetHeight;
            
            // Find column with minimum height
            let minHeightIndex = 0;
            let minHeight = columnHeights[0];
            
            for (let i = 1; i < columnHeights.length; i++) {
                if (columnHeights[i] < minHeight) {
                    minHeight = columnHeights[i];
                    minHeightIndex = i;
                }
            }
            
            // Move card to the shortest column
            columns[minHeightIndex].appendChild(cardElement);
            columnHeights[minHeightIndex] += cardHeight + gap;
        });
        
        // Remove temp container
        document.body.removeChild(tempContainer);
    }, 50);
    
    // Recalculate on window resize (debounced)
    if (!container._masonryResizeHandler) {
        let resizeTimeout;
        container._masonryResizeHandler = () => {
            clearTimeout(resizeTimeout);
            resizeTimeout = setTimeout(() => {
                // Re-apply masonry with current card HTMLs
                const currentCards = Array.from(container.querySelectorAll('.order-card'));
                const cardHtmls = currentCards.map(card => card.outerHTML);
                applyMasonryLayout(container, cardHtmls);
            }, 250);
        };
        window.addEventListener('resize', container._masonryResizeHandler);
    }
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

// Helper functions for item status & quantity-based counts
function getItemQuantity(item) {
    const qty = item && typeof item.quantity === 'number' ? item.quantity : parseInt(item?.quantity, 10);
    if (!isNaN(qty) && qty > 0) return qty;
    return 1;
}

function isStatusReady(status) {
    if (!status) return false;
    const s = status.toLowerCase().trim();
    return s.includes('ready') || s.includes('sẵn sàng');
}

function isStatusDone(status) {
    if (!status) return false;
    const s = status.toLowerCase().trim();
    return s.includes('done') || s.includes('hoàn thành') || s.includes('xong');
}

function isStatusCooking(status) {
    if (!status) return false;
    const s = status.toLowerCase().trim();
    return s === 'cooking' || s === 'đang nấu' || s.includes('cooking') || s.includes('đang nấu');
}

function isStatusLate(status) {
    if (!status) return false;
    const s = status.toLowerCase().trim();
    return s.includes('late') || s.includes('trễ');
}

function getOrderQuantityTotals(order) {
    const items = order.items || [];
    let totalQty = 0;
    let completedQty = 0;
    let lateQty = 0;
    let cookingQty = 0;

    items.forEach(item => {
        const qty = getItemQuantity(item);

        // Bỏ qua món đã hủy / trả (không tính vào tổng hoặc completed)
        const status = (item.status || '').toLowerCase().trim();
        const isCancelled = status.includes('cancelled') || status.includes('hủy') ||
            status.includes('đã hủy') || status.includes('returned') || status.includes('trả');
        if (isCancelled) {
            return;
        }

        totalQty += qty;

        if (isStatusReady(item.status) || isStatusDone(item.status)) {
            completedQty += qty;
        }
        if (isStatusLate(item.status)) {
            lateQty += qty;
        }
        if (isStatusCooking(item.status)) {
            cookingQty += qty;
        }
    });

    return {
        totalQty,
        completedQty,
        lateQty,
        cookingQty
    };
}

// Create single order card HTML
function createOrderCard(order) {
    const timerClass = getTimerClass(order.priorityLevel);
    const { totalQty, completedQty } = getOrderQuantityTotals(order);
    const completedItems = completedQty;
    const totalItems = totalQty || order.totalItems || 0;
    const canComplete = totalItems > 0 && completedItems === totalItems;
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
            <div class="d-flex justify-content-between align-items-start mb-3">
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
                    <small>${completedItems}/${totalItems} món đã hoàn thành</small>
                </div>
                <div class="progress" style="height: 8px;">
                    <div class="progress-bar ${canComplete ? 'bg-success' : 'bg-warning'}" 
                         style="width: ${totalItems > 0 ? ((completedItems / totalItems) * 100) : 0}%">
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

// Complete order - ✅ SỬA: Đánh dấu đơn hoàn thành khi bếp phó xác nhận
async function completeOrder(orderId) {
    const confirmed = await showConfirmPopup('Xác nhận đơn này đã hoàn thành? Đơn sẽ bị ẩn khỏi màn hình "Tất cả".', 'Xác nhận hoàn thành đơn');
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

        // Phân loại các món theo trạng thái
        const itemsToMarkReady = []; // Món cần chuyển sang Ready (Cooking, Late)
        const readyItems = []; // Món đã Ready
        const doneItems = []; // Món đã Done
        const pendingItems = []; // Món còn Pending

        order.items.forEach(item => {
            const status = (item.status || 'Pending').trim();
            if (status === 'Cooking' || status === 'Đang nấu' || status === 'Late' || status === 'Trễ') {
                itemsToMarkReady.push(item);
            } else if (status === 'Ready' || status === 'Sẵn sàng') {
                readyItems.push(item);
            } else if (status === 'Done' || status === 'Hoàn thành' || status === 'Xong') {
                doneItems.push(item);
            } else if (status === 'Pending' || status === 'Chờ') {
                pendingItems.push(item);
            }
        });

        // Nếu có món còn Pending, không cho phép
        if (pendingItems.length > 0) {
            showError('Có món chưa bắt đầu nấu. Vui lòng bắt đầu nấu trước!');
            return;
        }

        // Nếu có món cần chuyển sang Ready, thực hiện chuyển trước
        if (itemsToMarkReady.length > 0) {
            const promises = itemsToMarkReady.map(item =>
                updateItemStatusAPI(item.orderDetailId, 'Ready', item.orderComboItemId)
            );
            await Promise.all(promises);
        }

        // Xác nhận toàn bộ đơn đã sẵn sàng (không còn chuyển trạng thái đơn sang Completed)
        const response = await fetch(`${API_BASE}/KitchenDisplay/complete-order`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                orderId: orderId,
                sousChefUserId: 1 // TODO: Get from session
            })
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        const result = await response.json();
        if (!result.success) {
            throw new Error(result.message || 'Không thể xác nhận đơn sẵn sàng');
        }

        showSuccess('Đã xác nhận toàn bộ món trong đơn đều sẵn sàng/hoàn thành.');
        reloadCurrentView();
        
        // Tự động reload đơn vừa sẵn sàng nếu đang hiển thị
        const completedColumn = document.getElementById('completedOrdersColumn');
        if (completedColumn && !completedColumn.classList.contains('hidden')) {
            loadRecentlyFulfilledOrders();
        }
    } catch (error) {
        console.error('Error completing order:', error);
        showError('Không thể đánh dấu đơn hoàn thành: ' + error.message);
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

// Show/hide SignalR connection status banner
function showSignalRBanner(show) {
    const banner = document.getElementById('signalrStatusBanner');
    if (banner) {
        if (show) {
            banner.style.display = 'block';
        } else {
            banner.style.display = 'none';
        }
    }
}

// Start fallback polling when SignalR is disconnected (10-20s interval)
function startFallbackPolling() {
    // Clear existing interval if any
    stopFallbackPolling();
    
    // Random interval between 10-20 seconds
    const interval = 10000 + Math.random() * 10000; // 10-20 seconds
    
    fallbackPollingInterval = setInterval(() => {
        if (currentViewMode === 'theo-tung-mon') {
            loadGroupedItems();
        } else {
            loadOrdersByTable();
        }
        loadIngredientShortage();
    }, interval);
    
}

// Stop fallback polling
function stopFallbackPolling() {
    if (fallbackPollingInterval) {
        clearInterval(fallbackPollingInterval);
        fallbackPollingInterval = null;
    }
}

// Retry SignalR connection manually
function retrySignalRConnection() {
    if (signalRConnection) {
        signalRConnection.stop().then(() => {
            signalRConnection = null;
            initializeSignalR();
        }).catch(err => {
            console.error('Error stopping SignalR:', err);
            signalRConnection = null;
            initializeSignalR();
        });
    } else {
        initializeSignalR();
    }
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
            let errorMessage = 'Không thể tải order. Vui lòng thử lại.';
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
                    <button class="btn btn-primary mt-3" onclick="refreshOrders()">
                        <i class="mdi mdi-refresh"></i> Làm mới
                    </button>
                </div>
            `;
        }
    }
}

// Group orders by table number
function groupOrdersByTable(orders) {
    const grouped = {};
    
    orders.forEach(order => {
        const { totalQty, completedQty, lateQty } = getOrderQuantityTotals(order);
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
        grouped[tableKey].totalItems += totalQty;
        grouped[tableKey].completedItems += completedQty;
        grouped[tableKey].lateItems += lateQty;
        grouped[tableKey].readyItems += completedQty; // Ready + Done đã tính trong completedQty
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
    
    // Tính tổng số đơn và số món theo trạng thái của TẤT CẢ orders (đếm theo quantity)
    const totalOrders = orders.length;
    let totalItems = 0;
    let totalCompletedItems = 0;
    let totalLateItems = 0;
    let totalCookingItems = 0;

    orders.forEach(order => {
        const { totalQty, completedQty, lateQty, cookingQty } = getOrderQuantityTotals(order);
        totalItems += totalQty;
        totalCompletedItems += completedQty;
        totalLateItems += lateQty;
        totalCookingItems += cookingQty;
    });
    
    // Render từng order card
    const cardHtmls = orders.map(order => createOrderCard(order)).filter(html => html.trim() !== '');
    
    if (cardHtmls.length === 0) {
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
        <div class="table-group-header" style="background: #f5f5f5; padding: 15px; border-radius: 8px; margin-bottom: 15px; width: 100%;">
            <h3 style="margin: 0; display: flex; align-items: center; gap: 10px; flex-wrap: wrap;">
                <i class="mdi mdi-silverware-fork-knife"></i> 
                <span>${totalOrders} đơn | ${totalCompletedItems}/${totalItems} món đã hoàn thành</span>
                ${totalLateItems > 0 ? `<span style="color: #dc3545; margin-left: 10px;"><i class="mdi mdi-alert-circle"></i> Món đã trễ: ${totalLateItems}</span>` : ''}
                ${totalCookingItems > 0 ? `<span style="color: #28a745; margin-left: 10px;"><i class="mdi mdi-check-circle"></i> Món đang nấu: ${totalCookingItems}</span>` : ''}
            </h3>
        </div>
    `;
    
    // Clear and add header
    grid.innerHTML = summaryHeader;
    grid.classList.add('masonry');
    
    // Apply masonry to cards
    applyMasonryLayout(grid, cardHtmls);
    
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
                <div class="d-flex justify-content-between align-items-start mb-3">
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
    
    // Đếm lại số orders và items sau khi filter (theo quantity)
    const filteredOrders = group.orders.filter(order => {
        const sortedItems = sortItemsByCourseType(order.items || []);
        const filteredItems = currentStatusFilter !== 'all' 
            ? sortedItems.filter(item => item.status === currentStatusFilter)
            : sortedItems;
        return filteredItems.length > 0;
    });
    
    // Đếm số món đang Cooking cho riêng group này
    const cookingItemsCount = group.orders.reduce((sum, order) => {
        const { cookingQty } = getOrderQuantityTotals(order);
        return sum + cookingQty;
    }, 0);

    return `
        <div class="table-group-container" style="margin-bottom: 30px;">
            <div class="table-group-header" style="background: #f5f5f5; padding: 15px; border-radius: 8px; margin-bottom: 15px;">
                <h3 style="margin: 0; display: flex; align-items: center; gap: 10px; flex-wrap: wrap;">
                    <i class="mdi mdi-table"></i> 
                    <span>${filteredOrders.length} đơn | ${group.completedItems || 0}/${group.totalItems} món đã hoàn thành</span>
                    ${group.lateItems > 0 ? `<span style="color: #dc3545; margin-left: 10px;"><i class="mdi mdi-alert-circle"></i> Món đã trễ: ${group.lateItems}</span>` : ''}
                    ${cookingItemsCount > 0 ? `<span style="color: #28a745; margin-left: 10px;"><i class="mdi mdi-check-circle"></i> Món đang nấu: ${cookingItemsCount}</span>` : ''}
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
            let errorMessage = 'Không thể tải order. Vui lòng thử lại.';
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
                    <button class="btn btn-primary mt-3" onclick="refreshOrders()">
                        <i class="mdi mdi-refresh"></i> Làm mới
                    </button>
                </div>
            `;
        }
    }
}

// Render grouped items
function renderGroupedItems(groupedItems) {
    const grid = document.getElementById('ordersGrid');
    grid.className = ''; // Không dùng items-grid để tránh CSS grid override

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

    // Chia items thành 2 nhóm: Món nấu lâu và Món theo order
    const LONG_COOK_THRESHOLD = 15;
    const longCookItems = [];
    const regularItems = [];

    filteredItems.forEach(item => {
        const timeCook = Number(item.timeCook) || 0;
        if (timeCook > LONG_COOK_THRESHOLD) {
            longCookItems.push(item);
        } else {
            regularItems.push(item);
        }
    });

    // Sắp xếp từng nhóm
    const sortedLongCookItems = sortGroupedItems(longCookItems);
    const sortedRegularItems = sortGroupedItems(regularItems);

    // Render 2 hàng ngang
    let html = `
        <div class="items-horizontal-layout" style="display: flex; flex-direction: column; gap: 30px; width: 100%;">
    `;

    // Hàng 1: Món nấu lâu
    if (sortedLongCookItems.length > 0) {
        html += `
            <div class="items-section" style="width: 100%;">
                <div class="section-header" style="padding: 15px 20px; margin-bottom: 20px;">
                    <h3 style="margin: 0; display: flex; align-items: center; gap: 10px; font-size: 20px; font-weight: 600; color: #333;">
                        <i class="mdi mdi-clock-outline" style="font-size: 24px;"></i>
                        <span>Món nấu lâu (${sortedLongCookItems.length} món)</span>
                    </h3>
                </div>
                <div class="items-grid-section" id="longCookItemsGrid" style="position: relative; width: 100%; display: flex !important; flex-direction: row !important; flex-wrap: wrap !important; gap: 20px;"></div>
            </div>
        `;
    }

    // Hàng 2: Món theo order
    if (sortedRegularItems.length > 0) {
        html += `
            <div class="items-section" style="width: 100%;">
                <div class="section-header" style="padding: 15px 20px; margin-bottom: 20px;">
                    <h3 style="margin: 0; display: flex; align-items: center; gap: 10px; font-size: 20px; font-weight: 600; color: #333;">
                        <i class="mdi mdi-silverware-fork-knife" style="font-size: 24px;"></i>
                        <span>Món theo order (${sortedRegularItems.length} món)</span>
                    </h3>
                </div>
                <div class="items-grid-section" id="regularItemsGrid" style="position: relative; width: 100%; display: flex !important; flex-direction: row !important; flex-wrap: wrap !important; gap: 20px;"></div>
            </div>
        `;
    }

    html += `</div>`;
    grid.innerHTML = html;

    // Render cards theo hàng ngang cho cả 2 phần
    if (sortedLongCookItems.length > 0) {
        const longCookGrid = document.getElementById('longCookItemsGrid');
        if (longCookGrid) {
            const longCookCardHtmls = sortedLongCookItems.map(item => createItemCard(item));
            longCookGrid.innerHTML = longCookCardHtmls.join('');
            // Đảm bảo flexbox row
            longCookGrid.style.display = 'flex';
            longCookGrid.style.flexDirection = 'row';
            longCookGrid.style.flexWrap = 'wrap';
            longCookGrid.style.gap = '20px';
        }
    }

    if (sortedRegularItems.length > 0) {
        const regularGrid = document.getElementById('regularItemsGrid');
        if (regularGrid) {
            const regularCardHtmls = sortedRegularItems.map(item => createItemCard(item));
            regularGrid.innerHTML = regularCardHtmls.join('');
            // Đảm bảo flexbox row
            regularGrid.style.display = 'flex';
            regularGrid.style.flexDirection = 'row';
            regularGrid.style.flexWrap = 'wrap';
            regularGrid.style.gap = '20px';
        }
    }
    
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
    }, 100);
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
    const pendingOrderDetailIds = (item.itemDetails || [])
        .filter(detail => detail.status === 'Pending' || !detail.status)
        .map(detail => detail.orderDetailId);

    // Get pending item details for batch selection
    const pendingItemDetails = (item.itemDetails || [])
        .filter(detail => detail.status === 'Pending' || !detail.status);

    // Calculate quantities by status (only Pending & Cooking; Late counts as Cooking)
    let pendingQty = 0;
    let cookingQty = 0;
    
    if (item.itemDetails && item.itemDetails.length > 0) {
        item.itemDetails.forEach(detail => {
            const status = (detail.status || 'Pending').toLowerCase().trim();
            const qty = detail.quantity || 0;
            
            if (status.includes('pending') || status.includes('chờ')) {
                pendingQty += qty;
            } else if (
                status.includes('cooking') ||
                status.includes('nấu') ||
                status.includes('chế biến') ||
                status.includes('late') ||
                status.includes('trễ')
            ) {
                cookingQty += qty;
            }
        });
    }

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

    // Build status summary HTML (only Pending & Cooking)
    let statusSummaryHtml = '';
    if (pendingQty > 0 || cookingQty > 0) {
        statusSummaryHtml = '<div class="item-status-summary">';
        if (pendingQty > 0) {
            statusSummaryHtml += `<span class="status-badge status-pending">Chờ: ${pendingQty}</span>`;
        }
        if (cookingQty > 0) {
            statusSummaryHtml += `<span class="status-badge status-cooking">Nấu: ${cookingQty}</span>`;
        }
        statusSummaryHtml += '</div>';
    }

    return `
        <div class="item-card" data-menu-item-id="${item.menuItemId}">
            <div class="item-header">
                <div class="item-name-large">
                    <span class="item-name-text">${item.menuItemName}</span>
                    <span class="item-quantity-inline">x${item.totalQuantity}</span>
                </div>
                ${statusSummaryHtml}
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
        'Ready': 'filter-status-ready'
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

// Get status text for modal display (lowercase, in parentheses)
function getModalStatusText(status) {
    if (!status) return '(chờ)';
    
    const statusLower = status.toLowerCase().trim();
    
    // Xử lý cả tiếng Anh và tiếng Việt
    if (statusLower.includes('pending') || statusLower.includes('chờ') || statusLower.includes('chờ bếp'))
        return '(chờ)';
    if (statusLower.includes('cooking') || statusLower.includes('chế biến') || statusLower.includes('đang nấu'))
        return '(đang nấu)';
    if (statusLower.includes('late') || statusLower.includes('trễ'))
        return '(trễ)';
    if (statusLower.includes('ready') || statusLower.includes('sẵn sàng'))
        return '(sẵn sàng)';
    if (statusLower.includes('done') || statusLower.includes('hoàn thành') || statusLower.includes('xong'))
        return '(hoàn thành)';
    if (statusLower.includes('cancelled') || statusLower.includes('hủy') || statusLower.includes('đã hủy'))
        return '(đã hủy)';
    
    return `(${status})`;
}

// Render modal items
// Render danh sách món trong modal
function renderModalItems(items) {
    const itemsList = document.getElementById('modalOrderItems');
    if (!itemsList || !items || items.length === 0) {
        itemsList.innerHTML = '<li class="order-modal-item empty">Không có món nào trong đơn hàng này</li>';
        return;
    }

    // Món có thể thao tác: bỏ Done, Ready và Cancelled
    const cookableItems = items.filter(item => {
        const status = (item.status || '').toLowerCase().trim();
        const isDone = status.includes('done') || status.includes('hoàn thành') || status.includes('xong');
        const isReady = status.includes('ready') || status.includes('sẵn sàng');
        const isCancelled = status.includes('cancelled') || status.includes('hủy') || status.includes('đã hủy');
        return !isDone && !isReady && !isCancelled;
    });

    itemsList.innerHTML = cookableItems.map(item => {
        // Với món trong combo: dùng OrderComboItemId làm khóa duy nhất; món lẻ: dùng OrderDetailId
        const itemKey = item.orderComboItemId || item.orderDetailId;
        const selectedQuantity = selectedModalItems.get(itemKey) || 0;
        const isSelected = selectedQuantity > 0;
        const itemQuantity = item.quantity || 1;

        const status = (item.status || '').toLowerCase().trim();
        const isDone = status.includes('done') || status.includes('hoàn thành') || status.includes('xong');
        const isReady = status.includes('ready') || status.includes('sẵn sàng');
        const isDisabled = isDone || isReady;
        
        // Lấy trạng thái để hiển thị trong tên món
        const statusText = getModalStatusText(item.status || 'Pending');
        const menuItemNameWithStatus = `${item.menuItemName}${statusText}`;

        return `
            <li class="order-modal-item ${isSelected ? 'selected' : ''} ${isDisabled ? 'disabled' : ''}" 
                data-item-id="${itemKey}"
                data-order-detail-id="${item.orderDetailId}"
                data-order-combo-item-id="${item.orderComboItemId ?? ''}">
                <div style="display: flex; align-items: center; gap: 12px; width: 100%;">
                    <input type="checkbox" 
                           ${isSelected ? 'checked' : ''} 
                           ${isDisabled ? 'disabled' : ''}
                           onchange="toggleModalItemCheckbox(${itemKey}, ${itemQuantity}, event)"
                           onclick="event.stopPropagation()">
                    <span class="order-modal-item-text" style="flex: 1; ${isDisabled ? 'opacity: 0.6;' : ''}">
                        <strong>${menuItemNameWithStatus}</strong>
                        ${item.specialInstructions || item.notes ?
                `<span style="color: #d32f2f; font-size: 14px; display: block; margin-top: 4px;"> (${item.specialInstructions || item.notes})</span>` : ''}
                    </span>
                    <div style="display: flex; align-items: center; gap: 4px;">
                        <input type="number" 
                               class="modal-quantity-input" 
                               data-item-id="${itemKey}"
                               data-order-detail-id="${item.orderDetailId}"
                               data-order-combo-item-id="${item.orderComboItemId ?? ''}"
                               min="0" 
                               max="${itemQuantity}" 
                               value="${selectedQuantity}"
                               ${isDisabled ? 'disabled' : ''}
                               style="width: 50px; padding: 4px 8px; border: 1px solid #ddd; border-radius: 4px; text-align: center; ${isDisabled ? 'opacity: 0.6; background: #f5f5f5;' : ''}"
                               onchange="updateModalQuantity(${itemKey}, this.value, ${itemQuantity}, event)"
                               onclick="event.stopPropagation()">
                        <span style="color: #999; font-size: 14px;">/ ${itemQuantity}</span>
                    </div>
                </div>
            </li>
        `;
    }).join('');
}

// Update modal quantity when user changes input
// orderKey = itemId (OrderComboItemId nếu có, ngược lại OrderDetailId)
function updateModalQuantity(orderKey, newValue, maxQuantity, event) {
    if (event) {
        event.stopPropagation();
    }

    const quantity = parseInt(newValue) || 0;
    const maxQty = parseInt(maxQuantity) || 0;

    let validQuantity = quantity;
    if (validQuantity < 0) validQuantity = 0;
    if (validQuantity > maxQty) validQuantity = maxQty;

    // Đồng bộ lại giá trị input nếu bị chỉnh
    if (event && event.target) {
        event.target.value = validQuantity;
    }

    // Lưu vào Map theo itemKey (mỗi món combo riêng)
    if (validQuantity > 0) {
        selectedModalItems.set(orderKey, validQuantity);
    } else {
        selectedModalItems.delete(orderKey);
    }
}

// Toggle modal item checkbox
// itemKey có thể là orderComboItemId (món combo) hoặc orderDetailId (món lẻ)
function toggleModalItemCheckbox(itemKey, maxQuantity, event) {
    if (event) {
        event.stopPropagation();
    }
    
    const checkbox = event?.target;
    const isChecked = checkbox?.checked || false;
    
    // Tìm input bằng data-item-id (vì đó là itemKey - có thể là orderComboItemId hoặc orderDetailId)
    const quantityInput = document.querySelector(`.modal-quantity-input[data-item-id="${itemKey}"]`);
    const maxQty = parseInt(maxQuantity) || 0;
    
    if (quantityInput) {
        if (isChecked) {
            // If checked, set quantity to max
            const quantity = maxQty > 0 ? maxQty : 1;
            quantityInput.value = quantity;
            updateModalQuantity(itemKey, quantity, maxQty, null);
        } else {
            // If unchecked, set quantity to 0
            quantityInput.value = 0;
            updateModalQuantity(itemKey, 0, maxQty, null);
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

    // Hiển thị tất cả items, nhưng chỉ chọn các món có thể nấu (bỏ Ready/Done)
    const allItems = currentModalOrder.items || [];
    const cookableItems = allItems.filter(item => {
        const status = (item.status || '').toLowerCase().trim();
        const isDone = status.includes('done') || status.includes('hoàn thành') || status.includes('xong');
        const isReady = status.includes('ready') || status.includes('sẵn sàng');
        return !isDone && !isReady;
    });

    // Sử dụng itemKey (orderComboItemId hoặc orderDetailId) để kiểm tra và set
    const allSelected = cookableItems.length > 0 && cookableItems.every(item => {
        const itemKey = item.orderComboItemId || item.orderDetailId;
        const qty = selectedModalItems.get(itemKey) || 0;
        return qty > 0;
    });

    if (allSelected) {
        // Deselect all - xóa tất cả và cập nhật input
        selectedModalItems.clear();
        // Cập nhật tất cả input về 0
        document.querySelectorAll('.modal-quantity-input').forEach(input => {
            input.value = 0;
        });
    } else {
        // Select all cookable items with max quantity
        cookableItems.forEach(item => {
            const itemKey = item.orderComboItemId || item.orderDetailId;
            const maxQty = item.quantity || 1;
            selectedModalItems.set(itemKey, maxQty);
            
            // Cập nhật trực tiếp giá trị input để UI hiển thị đúng
            const quantityInput = document.querySelector(`.modal-quantity-input[data-item-id="${itemKey}"]`);
            if (quantityInput) {
                quantityInput.value = maxQty;
            }
        });
    }

    renderModalItems(currentModalOrder.items);
}

// (Removed rush/urgent control from KDS modal; waiter flow still retains its own logic)

// Fire selected items
async function fireSelectedItems() {
    // Lấy giá trị trực tiếp từ các input để đảm bảo lấy đúng số lượng đã chỉnh sửa
    const selectedItems = [];
    const quantityInputs = document.querySelectorAll('.modal-quantity-input');
    
    quantityInputs.forEach(input => {
        const orderDetailId = parseInt(input.getAttribute('data-order-detail-id'));
        const ociAttr = input.getAttribute('data-order-combo-item-id');
        const orderComboItemId = ociAttr ? parseInt(ociAttr) : null;
        const quantity = parseInt(input.value) || 0;
        const maxQuantity = parseInt(input.getAttribute('max')) || 0;
        
        if (quantity > 0 && quantity <= maxQuantity) {
            selectedItems.push({ orderDetailId, orderComboItemId, quantity });
        }
    });

    // Loại bỏ các món đã hủy khỏi danh sách xử lý để không tính thiếu nguyên liệu
    const activeSelectedItems = selectedItems.filter(sel => {
        const detail = currentModalOrder?.items?.find(d =>
            (sel.orderComboItemId && d.orderComboItemId === sel.orderComboItemId) ||
            (!sel.orderComboItemId && d.orderDetailId === sel.orderDetailId)
        );
        if (!detail) return false;
        const status = (detail.status || '').toLowerCase().trim();
        const isCancelled = status.includes('cancelled') || status.includes('hủy') || status.includes('đã hủy');
        return !isCancelled;
    });

    if (activeSelectedItems.length === 0) {
        showError('Vui lòng chọn ít nhất một món');
        return;
    }

    try {
        const totalQuantity = activeSelectedItems.reduce((sum, item) => sum + item.quantity, 0);

        // Gọi batch-cook API để gom tất cả món trong 1 call
        const response = await fetch(`${API_BASE}/KitchenDisplay/batch-cook`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                userId: 1,
                items: activeSelectedItems.map(i => ({
                    orderDetailId: i.orderDetailId,
                    orderComboItemId: i.orderComboItemId || null,
                    quantity: i.quantity
                }))
            })
        });

        const result = await response.json();

        if (!result.success) {
            const itemErrors = (result.items || [])
                .filter(it => !it.success && it.message)
                .map(it => it.message);

            const shortageErrors = itemErrors.filter(e => e.includes('Không đủ nguyên liệu') || e.includes('thiếu'));
            if (shortageErrors.length > 0) {
                const totalShortage = calculateTotalShortage(shortageErrors);
                showIngredientShortagePopupWithTotal(totalShortage, activeSelectedItems, shortageErrors);
                highlightItemsWithShortage(activeSelectedItems);
                return;
            }

            const firstError = itemErrors[0] || result.message || 'Không thể bắt đầu nấu';
            showError(firstError);
            if (firstError.includes('Order đã thay đổi từ hệ thống') || 
                firstError.includes('đã bị hủy') ||
                firstError.includes('đã hoàn thành')) {
                reloadCurrentView();
                closeOrderModal();
            }
            return;
        }

        // Nếu không có errors, thành công
        showSuccess(`Đã bắt đầu nấu ${totalQuantity} món (${activeSelectedItems.length} đơn)`);
        selectedModalItems.clear();
        reloadCurrentView();
        closeOrderModal();
    } catch (error) {
        console.error('Error starting cooking:', error);
        const errorMessage = error.message || 'Không thể bắt đầu nấu';
        
        // A4: Nếu thiếu nguyên liệu, hiển thị popup riêng và highlight món
        if (errorMessage.includes('Không đủ nguyên liệu') || errorMessage.includes('thiếu')) {
            showIngredientShortagePopup(errorMessage, selectedItems);
            highlightItemsWithShortage(selectedItems);
        }
        // A3: Nếu order đã thay đổi từ hệ thống, reload order
        else if (errorMessage.includes('Order đã thay đổi từ hệ thống') || 
            errorMessage.includes('đã bị hủy') ||
            errorMessage.includes('đã hoàn thành')) {
            showError(errorMessage);
            reloadCurrentView();
            closeOrderModal();
        } else {
            showError('Không thể bắt đầu nấu: ' + errorMessage);
        }
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
            // selectedItems is now array of {orderDetailId, orderComboItemId, quantity}
            const totalQuantity = selectedItems.reduce((sum, item) => sum + item.quantity, 0);
            const promises = [];
            
            selectedItems.forEach(({ orderDetailId, orderComboItemId, quantity }) => {
                if (quantity > 0) {
                    // Tìm detail để lấy tổng số lượng
                    const detail = itemData.itemDetails.find(d => d.orderDetailId === orderDetailId);
                    const totalQty = detail?.quantity || quantity;
                    
                    // Nếu là món trong combo → luôn update theo OrderComboItemId để bắt đầu nấu từng món con
                    if (detail && detail.orderComboItemId) {
                        promises.push(updateItemStatusAPI(orderDetailId, 'Cooking', detail.orderComboItemId));
                    }
                    else {
                        // Nếu quantity < totalQuantity, gọi API split
                        if (quantity < totalQty) {
                            promises.push(startCookingWithQuantityAPI(orderDetailId, quantity));
                        } else {
                            // Nếu quantity = totalQuantity, chỉ cần update status
                            promises.push(updateItemStatusAPI(orderDetailId, 'Cooking'));
                        }
                    }
                }
            });

            await Promise.all(promises);
            showSuccess(`Đã bắt đầu nấu ${totalQuantity} món (${selectedItems.length} đơn)`);
            reloadCurrentView();
        } catch (error) {
            console.error('Error starting cooking:', error);
            const errorMessage = error.message || 'Không thể bắt đầu nấu';
            
            // A4: Nếu thiếu nguyên liệu, hiển thị popup riêng và highlight món
            if (errorMessage.includes('Không đủ nguyên liệu') || errorMessage.includes('thiếu')) {
                showIngredientShortagePopup(errorMessage, activeSelectedItems);
                highlightItemsWithShortage(activeSelectedItems);
            }
            // A3: Nếu order đã thay đổi từ hệ thống, reload order
            else if (errorMessage.includes('Order đã thay đổi từ hệ thống') || 
                errorMessage.includes('đã bị hủy') ||
                errorMessage.includes('đã hoàn thành')) {
                showError(errorMessage);
                reloadCurrentView();
            } else {
                showError('Không thể bắt đầu nấu: ' + errorMessage);
            }
        }
    } else {
        // No batch size, use simple confirmation
        const details = itemData.itemDetails || [];
        const confirmed = await showConfirmPopup(`Bắt đầu nấu ${orderDetailIds.length} món này?`);
        if (!confirmed) {
            return;
        }

        try {
            const promises = details.map(detail =>
                updateItemStatusAPI(detail.orderDetailId, 'Cooking', detail.orderComboItemId || null)
            );

            await Promise.all(promises);
            showSuccess(`Đã bắt đầu nấu ${orderDetailIds.length} món`);
            reloadCurrentView();
        } catch (error) {
            console.error('Error starting cooking:', error);
            const errorMessage = error.message || 'Không thể bắt đầu nấu';
            
            // A4: Nếu thiếu nguyên liệu, hiển thị popup riêng và highlight món
            if (errorMessage.includes('Không đủ nguyên liệu') || errorMessage.includes('thiếu')) {
                const selectedItems = details.map(d => ({ 
                    orderDetailId: d.orderDetailId, 
                    orderComboItemId: d.orderComboItemId || null 
                }));
                showIngredientShortagePopup(errorMessage, selectedItems);
                highlightItemsWithShortage(selectedItems);
            }
            // A3: Nếu order đã thay đổi từ hệ thống, reload order
            else if (errorMessage.includes('Order đã thay đổi từ hệ thống') || 
                errorMessage.includes('đã bị hủy') ||
                errorMessage.includes('đã hoàn thành')) {
                showError(errorMessage);
                reloadCurrentView();
            } else {
                showError('Không thể bắt đầu nấu: ' + errorMessage);
            }
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
                        // Tìm lại detail để lấy OrderComboItemId nếu có
                        const detail = itemDetails.find(d => d.orderDetailId === orderDetailId);
                        selectedItems.push({
                            orderDetailId,
                            orderComboItemId: detail && detail.orderComboItemId ? detail.orderComboItemId : null,
                            quantity
                        });
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


// ===========================
// UPDATE ITEM STATUS API
// ===========================

// orderComboItemId: null/undefined = món lẻ (OrderDetail), có giá trị = món trong combo (OrderComboItem)
async function updateItemStatusAPI(orderDetailId, newStatus, orderComboItemId = null) {
    try {
        const payload = {
            orderDetailId: parseInt(orderDetailId),
            newStatus: newStatus.trim(),
            // Nếu là món trong combo sẽ gửi thêm orderComboItemId để backend cập nhật đúng record
            orderComboItemId: orderComboItemId ? parseInt(orderComboItemId) : null,
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

// Toast notifications (force hide after ~5s, no hover extension)
const KDS_TOAST_TIMEOUT = 5000;
const KDS_TOAST_EXT_TIMEOUT = 0;
const KDS_TOAST_CLEAR_BUFFER = 300;
const KDS_TOAST_HIDE_DURATION = 200;

function showSuccess(message) {
    if (typeof toastr === 'undefined') {
        console.log('SUCCESS:', message);
        return;
    }

    // Clear any existing toasts first
    toastr.clear();

    const toast = toastr.success(message, '', {
        closeButton: true,
        progressBar: true,
        positionClass: 'toast-top-right',
        timeOut: KDS_TOAST_TIMEOUT,
        extendedTimeOut: KDS_TOAST_EXT_TIMEOUT,
        showDuration: KDS_TOAST_HIDE_DURATION,
        hideDuration: KDS_TOAST_HIDE_DURATION,
        newestOnTop: true,
        preventDuplicates: true,
        escapeHtml: false,
        tapToDismiss: true
    });

    if (toast && toast[0]) {
        toast[0].dataset.toastTime = Date.now();
        // Hard clear after timeout + buffer even if hover happens
        setTimeout(() => {
            try { toastr.clear(toast); } catch (e) { /* ignore */ }
        }, KDS_TOAST_TIMEOUT + KDS_TOAST_CLEAR_BUFFER);
    }
}

function showError(message) {
    if (typeof toastr === 'undefined') {
        console.error('ERROR:', message);
        return;
    }

    // Clear any existing toasts first
    toastr.clear();

    const toast = toastr.error(message, '', {
        closeButton: true,
        progressBar: true,
        positionClass: 'toast-top-right',
        timeOut: KDS_TOAST_TIMEOUT,
        extendedTimeOut: KDS_TOAST_EXT_TIMEOUT,
        showDuration: KDS_TOAST_HIDE_DURATION,
        hideDuration: KDS_TOAST_HIDE_DURATION,
        newestOnTop: true,
        preventDuplicates: true,
        escapeHtml: false,
        tapToDismiss: true
    });

    if (toast && toast[0]) {
        toast[0].dataset.toastTime = Date.now();
        setTimeout(() => {
            try { toastr.clear(toast); } catch (e) { /* ignore */ }
        }, KDS_TOAST_TIMEOUT + KDS_TOAST_CLEAR_BUFFER);
    }
}
// Cleanup stuck toasts every 10 seconds
setInterval(() => {
    const container = document.getElementById('toast-container');
    if (container) {
        const toasts = container.querySelectorAll('.toast');
        toasts.forEach(toast => {
            // Check if toast has been there for more than 10 seconds
            const ageMs = Date.now() - (parseInt(toast.dataset.toastTime) || 0);
            if (ageMs > 10000) {
                toast.remove();
            }
        });

        // Remove container if empty
        if (container.children.length === 0) {
            container.remove();
        }
    }
}, 10000);

// (Removed: toastr override; we stamp dataset time directly in showSuccess/showError)
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

// Calculate total shortage from error messages
function calculateTotalShortage(errorMessages) {
    const shortageMap = new Map(); // Map<ingredientName, totalShortage>
    
    errorMessages.forEach(errorMsg => {
        // Parse error message: "Không đủ nguyên liệu: {ingredientName}. Thiếu: {quantity} {unit}"
        const match = errorMsg.match(/Không đủ nguyên liệu:\s*(.+?)\.\s*Thiếu:\s*([\d.]+)\s*(.*)/);
        if (match) {
            const ingredientName = match[1].trim();
            const shortage = parseFloat(match[2]) || 0;
            const unit = match[3].trim();
            
            const key = `${ingredientName} (${unit})`;
            if (shortageMap.has(key)) {
                shortageMap.set(key, shortageMap.get(key) + shortage);
            } else {
                shortageMap.set(key, shortage);
            }
        }
    });
    
    return shortageMap;
}

// A4: Show ingredient shortage popup with total shortage
function showIngredientShortagePopupWithTotal(totalShortageMap, selectedItems, errorMessages) {
    const overlay = document.createElement('div');
    overlay.className = 'confirm-popup-overlay';
    overlay.id = 'ingredientShortagePopup';
    
    const popup = document.createElement('div');
    popup.className = 'confirm-popup';
    
    const menuItemNames = selectedItems.map(item => {
        const detail = currentModalOrder?.items?.find(d => 
            (item.orderComboItemId && d.orderComboItemId === item.orderComboItemId) ||
            (!item.orderComboItemId && d.orderDetailId === item.orderDetailId)
        );
        return detail?.menuItemName || 'Món ăn';
    }).join(', ');
    
    // Build shortage list HTML
    let shortageListHTML = '';
    totalShortageMap.forEach((totalShortage, ingredientKey) => {
        shortageListHTML += `<li style="margin-bottom: 8px;"><strong>${ingredientKey}:</strong> <span style="color: #dc3545; font-weight: 600;">Thiếu ${totalShortage}</span></li>`;
    });
    
    popup.innerHTML = `
        <div class="confirm-popup-header">
            <i class="mdi mdi-alert-circle confirm-popup-icon"></i>
            <h3 class="confirm-popup-title">Không đủ nguyên liệu để nấu món</h3>
        </div>
        <div class="confirm-popup-body">
            <p style="font-size: 16px; margin-bottom: 15px;"><strong>Món bị thiếu:</strong> ${menuItemNames}</p>
            <p style="color: #dc3545; font-weight: 600; margin-bottom: 15px; font-size: 18px;">Tổng số nguyên liệu thiếu:</p>
            <ul style="text-align: left; margin-left: 20px; color: #333;">
                ${shortageListHTML}
            </ul>
            <p style="color: #666; font-size: 14px; margin-top: 15px;">Vui lòng xử lý món khác hoặc báo quản lý để bổ sung nguyên liệu.</p>
        </div>
        <div class="confirm-popup-footer">
            <button class="btn btn-primary" onclick="closeIngredientShortagePopup()">Đã hiểu</button>
        </div>
    `;
    
    overlay.appendChild(popup);
    document.body.appendChild(overlay);
    
    // Close on overlay click
    overlay.addEventListener('click', function(e) {
        if (e.target === overlay) {
            closeIngredientShortagePopup();
        }
    });
}

// A4: Show ingredient shortage popup (single error)
function showIngredientShortagePopup(errorMessage, selectedItems) {
    const overlay = document.createElement('div');
    overlay.className = 'confirm-popup-overlay';
    overlay.id = 'ingredientShortagePopup';
    
    const popup = document.createElement('div');
    popup.className = 'confirm-popup';
    
    const menuItemNames = selectedItems.map(item => {
        const detail = currentModalOrder?.items?.find(d => 
            (item.orderComboItemId && d.orderComboItemId === item.orderComboItemId) ||
            (!item.orderComboItemId && d.orderDetailId === item.orderDetailId)
        );
        return detail?.menuItemName || 'Món ăn';
    }).join(', ');
    
    popup.innerHTML = `
        <div class="confirm-popup-header">
            <i class="mdi mdi-alert-circle confirm-popup-icon"></i>
            <h3 class="confirm-popup-title">Không đủ nguyên liệu để nấu món này</h3>
        </div>
        <div class="confirm-popup-body">
            <p style="font-size: 16px; margin-bottom: 15px;"><strong>Món bị thiếu:</strong> ${menuItemNames}</p>
            <p style="color: #dc3545; font-weight: 600; margin-bottom: 15px;">${errorMessage}</p>
            <p style="color: #666; font-size: 14px;">Vui lòng xử lý món khác hoặc báo quản lý để bổ sung nguyên liệu.</p>
        </div>
        <div class="confirm-popup-footer">
            <button class="btn btn-primary" onclick="closeIngredientShortagePopup()">Đã hiểu</button>
        </div>
    `;
    
    overlay.appendChild(popup);
    document.body.appendChild(overlay);
    
    // Close on overlay click
    overlay.addEventListener('click', function(e) {
        if (e.target === overlay) {
            closeIngredientShortagePopup();
        }
    });
}

// Close ingredient shortage popup
function closeIngredientShortagePopup() {
    const popup = document.getElementById('ingredientShortagePopup');
    if (popup) {
        popup.remove();
    }
}

// A4: Highlight items with shortage by adding red badge "Thiếu"
function highlightItemsWithShortage(selectedItems) {
    selectedItems.forEach(({ orderDetailId, orderComboItemId }) => {
        // Find order card
        const orderCard = document.querySelector(`[data-order-id]`);
        if (!orderCard) return;
        
        // Find item in modal or in order card
        let itemElement = null;
        if (orderComboItemId) {
            itemElement = document.querySelector(`[data-order-combo-item-id="${orderComboItemId}"]`);
        } else {
            itemElement = document.querySelector(`[data-order-detail-id="${orderDetailId}"]`);
        }
        
        if (itemElement) {
            // Check if badge already exists
            let badge = itemElement.querySelector('.shortage-badge');
            if (!badge) {
                badge = document.createElement('span');
                badge.className = 'badge bg-danger shortage-badge';
                badge.textContent = 'Thiếu';
                badge.style.marginLeft = '8px';
                badge.style.fontSize = '12px';
                
                // Insert badge after item name or at the end of item element
                const itemName = itemElement.querySelector('.item-name, .menu-item-name, .order-item-name');
                if (itemName) {
                    itemName.appendChild(badge);
                } else {
                    itemElement.appendChild(badge);
                }
            }
            
            // Add visual highlight
            itemElement.style.borderLeft = '3px solid #dc3545';
            itemElement.style.backgroundColor = '#fff5f5';
        }
    });
    
    // Also reload to show updated shortage status
    reloadCurrentView();
}

// Load ingredient shortage list
async function loadIngredientShortage() {
    try {
        const response = await fetch(`${API_BASE}/InventoryIngredient/shortage`);
        if (!response.ok) {
            throw new Error('Failed to load ingredient shortage');
        }

        const result = await response.json();
        if (result.success && result.data && result.data.length > 0) {
            renderIngredientShortage(result.data);
        } else {
            // Hide panel if no shortage
            const panel = document.getElementById('shortageAlertPanel');
            if (panel) {
                panel.style.display = 'none';
            }
        }
    } catch (error) {
        console.error('Error loading ingredient shortage:', error);
        // Hide panel on error
        const panel = document.getElementById('shortageAlertPanel');
        if (panel) {
            panel.style.display = 'none';
        }
    }
}

// Helper functions
function formatNumber(num) {
    if (num == null || isNaN(num)) return '0';
    return parseFloat(num).toLocaleString('vi-VN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Render ingredient shortage list
function renderIngredientShortage(shortageList) {
    const panel = document.getElementById('shortageAlertPanel');
    const body = document.getElementById('shortageAlertBody');
    const countBadge = document.getElementById('shortageCount');

    if (!panel || !body || !countBadge) {
        return;
    }

    // Show panel
    panel.style.display = 'block';

    // ✅ TỔNG HỢP THEO NGUYÊN LIỆU (không theo món) - TỔNG QUAN CHO BẾP PHÓ
    const ingredientSummary = {};
    shortageList.forEach(item => {
        const key = `${item.ingredientId}_${item.ingredientName}_${item.unitName || ''}`;
        if (!ingredientSummary[key]) {
            ingredientSummary[key] = {
                ingredientId: item.ingredientId,
                ingredientName: item.ingredientName,
                unitName: item.unitName || '',
                totalShortage: 0,
                affectedDishes: new Set(), // Set để tránh trùng lặp món
                urgentCount: 0
            };
        }

        // Cộng dồn trực tiếp số lượng thiếu do backend tính sẵn
        ingredientSummary[key].totalShortage += item.shortageQuantity || 0;

        ingredientSummary[key].affectedDishes.add(item.menuItemName);
        if (item.isUrgent) {
            ingredientSummary[key].urgentCount++;
        }
    });

    // Chỉ giữ lại nguyên liệu thực sự đang thiếu (> 0)
    const filteredIngredients = Object.values(ingredientSummary).filter(ing => ing.totalShortage > 0);

    // Nếu không còn nguyên liệu nào thiếu -> ẩn panel
    if (filteredIngredients.length === 0) {
        panel.style.display = 'none';
        body.innerHTML = '<div class="text-center text-muted py-3">Không có nguyên liệu thiếu</div>';
        countBadge.textContent = '0';
        return;
    }

    // Update count - số lượng nguyên liệu thiếu (không phải số món)
    const uniqueIngredientCount = filteredIngredients.length;
    countBadge.textContent = uniqueIngredientCount;

    // Sắp xếp: nguyên liệu có số lượng thiếu nhiều nhất trước, sau đó theo tên
    const sortedIngredients = filteredIngredients.sort((a, b) => {
        if (b.totalShortage !== a.totalShortage) {
            return b.totalShortage - a.totalShortage; // Thiếu nhiều nhất trước
        }
        return a.ingredientName.localeCompare(b.ingredientName);
    });

    // Render tổng quan theo nguyên liệu
    let html = '';
    
    if (sortedIngredients.length === 0) {
        html = '<div class="text-center text-muted py-3">Không có nguyên liệu thiếu</div>';
    } else {
        // Summary header
        html += `
            <div style="background: #fff3cd; padding: 12px; border-radius: 6px; margin-bottom: 15px; border-left: 4px solid #ffc107;">
                <div style="font-weight: 600; color: #856404; margin-bottom: 8px;">
                    <i class="mdi mdi-information" style="margin-right: 6px;"></i>
                    Tổng quan nguyên liệu thiếu
                </div>
                <div style="font-size: 14px; color: #856404;">
                    Có <strong>${uniqueIngredientCount}</strong> nguyên liệu đang thiếu, ảnh hưởng đến <strong>${shortageList.length}</strong> món
                </div>
            </div>
        `;

        // Render từng nguyên liệu với tổng số thiếu
        sortedIngredients.forEach(ing => {
            const affectedDishesList = Array.from(ing.affectedDishes);
            const urgentBadge = ing.urgentCount > 0 ? `<span class="urgent-badge-shortage">${ing.urgentCount} món ưu tiên</span>` : '';
            
            html += `
                <div class="shortage-item ${ing.urgentCount > 0 ? 'urgent' : ''}" style="margin-bottom: 12px;">
                    <div class="shortage-item-info" style="flex: 1;">
                        <div class="shortage-item-name" style="font-size: 16px; margin-bottom: 8px;">
                            <i class="mdi mdi-food-variant" style="margin-right: 6px; color: #dc3545;"></i>
                            <strong>${escapeHtml(ing.ingredientName)}</strong>${urgentBadge}
                        </div>
                        <div class="shortage-item-details" style="font-size: 13px; line-height: 1.6;">
                            <div style="margin-top: 6px;">
                                <span style="color: #6c757d; font-size: 12px;">Ảnh hưởng:</span> 
                                <span style="color: #495057; font-size: 12px;">${affectedDishesList.slice(0, 3).map(d => escapeHtml(d)).join(', ')}${affectedDishesList.length > 3 ? ` và ${affectedDishesList.length - 3} món khác` : ''}</span>
                            </div>
                        </div>
                    </div>
                    <div class="shortage-item-quantity" style="text-align: right; min-width: 150px;">
                        <div class="shortage-quantity-badge" style="font-size: 18px; padding: 10px 16px; margin-bottom: 8px;">
                            <i class="mdi mdi-alert" style="margin-right: 4px;"></i>
                            Thiếu ${formatNumber(ing.totalShortage)} ${ing.unitName}
                        </div>
                        <div style="font-size: 11px; color: #6c757d; margin-top: 4px;">
                            ${affectedDishesList.length} món
                        </div>
                    </div>
                </div>
            `;
        });
    }

    body.innerHTML = html;
}

// Toggle shortage panel
function toggleShortagePanel() {
    const body = document.getElementById('shortageAlertBody');
    const icon = document.getElementById('shortageToggleIcon');
    
    if (!body || !icon) {
        return;
    }

    if (body.classList.contains('collapsed')) {
        body.classList.remove('collapsed');
        icon.className = 'mdi mdi-chevron-up';
    } else {
        body.classList.add('collapsed');
        icon.className = 'mdi mdi-chevron-down';
    }
}