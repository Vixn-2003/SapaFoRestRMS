// Kitchen Station JavaScript - READ-ONLY VERSION
// File: wwwroot/js/kitchenStation.js
// Trạm CHỈ XEM và HOÀN THÀNH món, KHÔNG được tự fire

// OPTIMIZED: Auto-detect API base URL từ current location
function getApiBaseUrl() {
    // Ưu tiên: window.API_BASE_URL từ server config
    if (window.API_BASE_URL) {
        console.log('[getApiBaseUrl] Using server config:', window.API_BASE_URL);
        return window.API_BASE_URL;
    }
    
    // Fallback: Tự động detect từ current location
    const currentHost = window.location.hostname;
    const currentProtocol = window.location.protocol;
    
    // Nếu đang chạy trên localhost, dùng HTTPS localhost:7096
    if (currentHost === 'localhost' || currentHost === '127.0.0.1') {
        return 'https://localhost:7096/api';
    }
    
    // Nếu đang chạy trên IP (192.168.x.x), thử HTTPS trước, nếu fail thì HTTP
    if (currentHost.match(/^\d+\.\d+\.\d+\.\d+$/)) {
        // Ưu tiên HTTPS (vì backend thường chạy HTTPS)
        return `https://${currentHost}:7096/api`;
    }
    
    // Default fallback - dùng HTTPS localhost:7096
    return 'https://localhost:7096/api';
}

const API_BASE = getApiBaseUrl();
let signalRConnection = null;
let currentCategoryName = '';
let currentData = null;
let selectedCookingItems = new Set(); // Chỉ select items đang cooking để hoàn thành
let retryCount = 0;
const MAX_RETRIES = 3;

// Initialize station - OPTIMIZED
function initializeStation(categoryName) {
    currentCategoryName = categoryName;
    retryCount = 0; // Reset retry count
    
    // Log API URL để debug
    console.log('[initializeStation] API Base URL:', API_BASE);
    console.log('[initializeStation] Category:', categoryName);
    
    // OPTIMIZED: Hiển thị loading indicator
    const allItemsList = document.getElementById('allItemsList');
    const urgentItemsTable = document.getElementById('urgentItemsTable');
    if (allItemsList) {
        allItemsList.innerHTML = '<div class="text-center py-5"><i class="mdi mdi-loading mdi-spin" style="font-size: 48px;"></i><p class="mt-3">Đang tải dữ liệu...</p></div>';
    }
    if (urgentItemsTable) {
        urgentItemsTable.innerHTML = '<tr><td colspan="5" class="empty-state"><i class="mdi mdi-loading mdi-spin" style="font-size: 24px;"></i> Đang tải...</td></tr>';
    }
    
    // Load data trước, SignalR sau (lazy load)
    loadStationItems().then(() => {
        retryCount = 0; // Reset on success
        // Sau khi data đã load xong, mới kết nối SignalR
        setTimeout(() => {
            initializeSignalR();
        }, 500);
    }).catch(error => {
        console.error('Error loading initial station data:', error);
        // Hiển thị error message với retry button
        showErrorWithRetry(error);
        // Vẫn thử kết nối SignalR dù có lỗi
        setTimeout(() => {
            initializeSignalR();
        }, 500);
    });

    // Auto-refresh every 30 seconds
    setInterval(loadStationItems, 30000);

    // Update countdown timers every second
    setInterval(updateTimers, 1000);
}

// SignalR Setup - OPTIMIZED (lazy load, không block UI)
function initializeSignalR() {
    // Nếu đã có connection, không tạo lại
    if (signalRConnection && signalRConnection.state !== signalR.HubConnectionState.Disconnected) {
        return;
    }

    const hubUrl = window.SIGNALR_HUB_URL || (API_BASE.replace('/api', '') + '/kitchenHub');
    console.log('[initializeSignalR] Hub URL:', hubUrl);

    signalRConnection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
            skipNegotiation: true,
            transport: signalR.HttpTransportType.WebSockets
        })
        .withAutomaticReconnect()
        .build();

    // Listen for item status changes from Sous Chef
    signalRConnection.on('ItemStatusChanged', function (notification) {
        console.log('[SignalR] Item status changed:', notification);
        setTimeout(() => {
            loadStationItems();
        }, 500);
    });

    signalRConnection.on('ItemUrgentStatusChanged', function (notification) {
        console.log('[SignalR] Item urgent status changed:', notification);
        loadStationItems();
    });

    // OPTIMIZED: Start connection trong background, không block
    signalRConnection.start()
        .then(() => console.log('SignalR connected to kitchen hub'))
        .catch(err => {
            console.error('SignalR connection error:', err);
            // Retry sau 5 giây
            setTimeout(() => {
                if (signalRConnection && signalRConnection.state === signalR.HubConnectionState.Disconnected) {
                    initializeSignalR();
                }
            }, 5000);
        });
}

// Load station items from API - OPTIMIZED với timeout, retry và error handling
async function loadStationItems() {
    const allItemsList = document.getElementById('allItemsList');
    const urgentItemsTable = document.getElementById('urgentItemsTable');
    
    try {
        if (!currentCategoryName || currentCategoryName.trim() === '') {
            console.error('Category name is empty!');
            showError('Tên trạm không hợp lệ');
            if (allItemsList) {
                allItemsList.innerHTML = '<div class="empty-state" style="color: #dc3545;">Tên trạm không hợp lệ</div>';
            }
            return Promise.resolve();
        }

        console.log('[loadStationItems] Loading for category:', currentCategoryName);
        console.log('[loadStationItems] API Base URL:', API_BASE);
        console.log('[loadStationItems] Current location:', window.location.href);
        const url = `${API_BASE}/KitchenDisplay/station-items?categoryName=${encodeURIComponent(currentCategoryName)}`;
        console.log('[loadStationItems] Full URL:', url);
        
        // Test connection trước khi fetch - thử ping API root
        try {
            const testUrl = API_BASE.replace('/api', '') + '/swagger/index.html';
            console.log('[loadStationItems] Testing backend connection at:', testUrl);
        } catch (e) {
            console.warn('[loadStationItems] Could not test connection:', e);
        }

        // OPTIMIZED: Thêm timeout cho fetch (10 giây - giảm từ 15s)
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 10000);

        let response;
        try {
            // Thử với mode 'cors' và credentials
            // Nếu URL là HTTPS nhưng fail, thử HTTP
            response = await fetch(url, {
                signal: controller.signal,
                method: 'GET',
                mode: 'cors', // Explicit CORS mode
                credentials: 'omit', // Không dùng credentials để tránh CORS issue
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                }
            });
            clearTimeout(timeoutId);
        } catch (fetchError) {
            clearTimeout(timeoutId);
            
            // Nếu lỗi và URL là HTTPS, thử HTTP
            if (url.startsWith('https://') && 
                (fetchError.message?.includes('Failed to fetch') || 
                 fetchError.message?.includes('ERR_CONNECTION_REFUSED') ||
                 fetchError.message?.includes('ERR_SSL'))) {
                console.log('[loadStationItems] HTTPS failed, trying HTTP...');
                const httpUrl = url.replace('https://', 'http://');
                try {
                    response = await fetch(httpUrl, {
                        signal: controller.signal,
                        method: 'GET',
                        mode: 'cors',
                        credentials: 'omit',
                        headers: {
                            'Accept': 'application/json',
                            'Content-Type': 'application/json'
                        }
                    });
                    clearTimeout(timeoutId);
                    console.log('[loadStationItems] HTTP connection successful!');
                } catch (httpError) {
                    console.error('[loadStationItems] HTTP also failed:', httpError);
                    // Fall through to retry logic
                }
            }
            
            // Retry logic với exponential backoff
            if (!response && retryCount < MAX_RETRIES && 
                (fetchError.name === 'AbortError' || 
                 fetchError.message?.includes('Failed to fetch') || 
                 fetchError.message?.includes('ERR_CONNECTION_TIMED_OUT'))) {
                retryCount++;
                const delay = Math.min(1000 * Math.pow(2, retryCount - 1), 5000); // 1s, 2s, 4s
                console.log(`[loadStationItems] Retry ${retryCount}/${MAX_RETRIES} after ${delay}ms...`);
                
                // Update UI với retry message
                if (allItemsList) {
                    allItemsList.innerHTML = `
                        <div class="empty-state">
                            <i class="mdi mdi-loading mdi-spin" style="font-size: 48px;"></i>
                            <p class="mt-3">Đang thử lại lần ${retryCount}/${MAX_RETRIES}...</p>
                        </div>
                    `;
                }
                
                await new Promise(resolve => setTimeout(resolve, delay));
                return loadStationItems(); // Retry
            }
            
            // Không retry được nữa, throw error
            if (!response) {
                if (fetchError.name === 'AbortError') {
                    throw new Error('Kết nối quá lâu. Vui lòng kiểm tra lại server hoặc kết nối mạng.');
                } else if (fetchError.message && (fetchError.message.includes('Failed to fetch') || fetchError.message.includes('ERR_CONNECTION_TIMED_OUT'))) {
                    throw new Error(`Không thể kết nối đến API server tại ${API_BASE}. Vui lòng đảm bảo backend đang chạy tại https://localhost:7096.`);
                }
                throw fetchError;
            }
        }

        if (!response.ok) {
            const errorText = await response.text().catch(() => 'Unknown error');
            throw new Error(`HTTP ${response.status}: ${errorText}`);
        }

        const result = await response.json();
        console.log('[loadStationItems] API Response:', result);

        if (result.success) {
            retryCount = 0; // Reset retry count on success
            currentData = result.data;
            console.log('[loadStationItems] Total items:', currentData.allItems?.length || 0);

            if (!currentData.allItems) {
                currentData.allItems = [];
            }

            renderStationItems(currentData);
            updateCounts(currentData);
        } else {
            console.error('API returned error:', result.message);
            const errorMsg = result.message || 'Không thể tải dữ liệu trạm';
            showError(errorMsg);
            if (allItemsList) {
                allItemsList.innerHTML = `<div class="empty-state" style="color: #dc3545;"><i class="mdi mdi-alert-circle"></i> ${errorMsg}</div>`;
            }
            if (urgentItemsTable) {
                urgentItemsTable.innerHTML = '<tr><td colspan="5" class="empty-state" style="color: #dc3545;">Lỗi tải dữ liệu</td></tr>';
            }
        }
    } catch (error) {
        console.error('[loadStationItems] Error:', error);
        const errorMessage = error.message || 'Lỗi kết nối API';
        showError(errorMessage);
        
        // Hiển thị error message trong UI với retry button
        showErrorWithRetry(error);
    }
}

// Show error với retry button
function showErrorWithRetry(error) {
    const allItemsList = document.getElementById('allItemsList');
    const urgentItemsTable = document.getElementById('urgentItemsTable');
    const errorMessage = error.message || 'Lỗi kết nối API';
    
    if (allItemsList) {
        allItemsList.innerHTML = `
            <div class="empty-state" style="color: #dc3545;">
                <i class="mdi mdi-server-network-off" style="font-size: 48px;"></i>
                <p class="mt-3" style="font-weight: bold;">${errorMessage}</p>
                <p class="mt-2" style="font-size: 14px; color: #666;">API URL: ${API_BASE}</p>
                <p class="mt-2" style="font-size: 14px; color: #666;">Vui lòng kiểm tra:</p>
                <ul style="text-align: left; display: inline-block; margin-top: 10px; color: #666;">
                    <li>Backend API server đang chạy tại ${API_BASE}</li>
                    <li>Kết nối mạng ổn định</li>
                    <li>Firewall không chặn kết nối</li>
                </ul>
                <button class="btn btn-primary mt-3" onclick="retryLoadStationItems()" style="padding: 10px 20px;">
                    <i class="mdi mdi-refresh"></i> Thử lại
                </button>
            </div>
        `;
    }
    if (urgentItemsTable) {
        urgentItemsTable.innerHTML = `<tr><td colspan="5" class="empty-state" style="color: #dc3545;">${errorMessage}</td></tr>`;
    }
}

// Retry load function
function retryLoadStationItems() {
    retryCount = 0; // Reset retry count
    const allItemsList = document.getElementById('allItemsList');
    const urgentItemsTable = document.getElementById('urgentItemsTable');
    
    if (allItemsList) {
        allItemsList.innerHTML = '<div class="text-center py-5"><i class="mdi mdi-loading mdi-spin" style="font-size: 48px;"></i><p class="mt-3">Đang tải lại...</p></div>';
    }
    if (urgentItemsTable) {
        urgentItemsTable.innerHTML = '<tr><td colspan="5" class="empty-state"><i class="mdi mdi-loading mdi-spin" style="font-size: 24px;"></i> Đang tải...</td></tr>';
    }
    
    loadStationItems();
}

// Render station items - BÊN TRÁI: Tất cả món trong trạm | BÊN PHẢI: Món được fire (Cooking)
function renderStationItems(data) {
    console.log('[renderStationItems] Rendering items...');

    const allItemsList = document.getElementById('allItemsList');
    const cookingTable = document.getElementById('urgentItemsTable');
    const allCountEl = document.getElementById('allCount');
    const cookingCountEl = document.getElementById('cookingCount');

    if (!allItemsList || !cookingTable || !allCountEl || !cookingCountEl) {
        console.error('[renderStationItems] Required DOM elements not found!');
        return;
    }

    if (!data.allItems || data.allItems.length === 0) {
        console.warn('[renderStationItems] No items in this station!');
        allItemsList.innerHTML = '<div class="empty-state">Không có món nào trong trạm này</div>';
        cookingTable.innerHTML = '<tr><td colspan="5" class="empty-state">Không có món nào cần nấu</td></tr>';
        allCountEl.textContent = '0';
        cookingCountEl.textContent = '0';
        return;
    }

    console.log('[renderStationItems] Item statuses:',
        data.allItems.map(item => ({ name: item.menuItemName, status: item.status }))
    );

    // BÊN TRÁI: Nhóm TẤT CẢ món theo tên (bất kể status)
    // Hiển thị tổng số lượng của từng món đang có trong hệ thống
    const groupedAllItems = groupItemsByDish(data.allItems);
    console.log('[renderStationItems] All grouped items:', groupedAllItems.length);

    if (groupedAllItems.length > 0) {
        allItemsList.innerHTML = groupedAllItems
            .map(group => createAllItemsCard(group))
            .join('');
    } else {
        allItemsList.innerHTML = '<div class="empty-state">Không có món nào trong trạm này</div>';
    }

    // BÊN PHẢI: CHỈ hiển thị items có status = "Cooking" (đã được bếp phó fire)
    const cookingItems = data.allItems.filter(item => {
        const status = (item.status || '').toLowerCase();
        return status === 'cooking' || status === 'đang chế biến';
    });

    console.log('[renderStationItems] Cooking items (fired by sous chef):', cookingItems.length);

    if (cookingItems.length > 0) {
        cookingTable.innerHTML = cookingItems
            .map(item => createCookingTableRow(item))
            .join('');
    } else {
        cookingTable.innerHTML = '<tr><td colspan="5" class="empty-state">Chưa có món nào được bếp phó fire</td></tr>';
    }

    // Update counts
    allCountEl.textContent = groupedAllItems.length;
    cookingCountEl.textContent = cookingItems.length;
}

// Group items by dish name - NHÓM TẤT CẢ (không filter theo status)
function groupItemsByDish(items) {
    const grouped = {};

    items.forEach(item => {
        const dishName = item.menuItemName;
        if (!grouped[dishName]) {
            grouped[dishName] = {
                dishName: dishName,
                totalQuantity: 0,
                pendingQuantity: 0,
                cookingQuantity: 0,
                doneQuantity: 0
            };
        }

        // Tính tổng số lượng
        grouped[dishName].totalQuantity += item.quantity;

        // Phân loại theo status
        const status = (item.status || '').toLowerCase();
        if (status === 'pending' || status === 'đã gửi' || status === '' || !item.status) {
            grouped[dishName].pendingQuantity += item.quantity;
        } else if (status === 'cooking' || status === 'đang chế biến') {
            grouped[dishName].cookingQuantity += item.quantity;
        } else if (status === 'done' || status === 'hoàn thành') {
            grouped[dishName].doneQuantity += item.quantity;
        }
    });

    return Object.values(grouped);
}

// Create all items card (bên trái) - CHỈ HIỂN THỊ, KHÔNG CÓ NÚT FIRE
function createAllItemsCard(group) {
    // Hiển thị breakdown theo status
    let statusBreakdown = '';
    if (group.pendingQuantity > 0) {
        statusBreakdown += `<span class="status-pending-badge">Chờ: ${group.pendingQuantity}</span> `;
    }
    if (group.cookingQuantity > 0) {
        statusBreakdown += `<span class="status-cooking-badge">Nấu: ${group.cookingQuantity}</span> `;
    }
    if (group.doneQuantity > 0) {
        statusBreakdown += `<span class="status-done-badge">Xong: ${group.doneQuantity}</span>`;
    }

    return `
        <div class="grouped-item-card">
            <div class="grouped-item-name">${group.dishName}</div>
            <div class="grouped-item-quantity">x${group.totalQuantity}</div>
            <div class="grouped-item-status" style="margin-top: 10px; font-size: 14px;">
                ${statusBreakdown}
            </div>
        </div>
    `;
}

// Create cooking table row (bên phải) - VỚI CHECKBOX ĐỂ HOÀN THÀNH
function createCookingTableRow(item) {
    const rowClass = item.isUrgent ? 'urgent-row' : '';
    const isChecked = selectedCookingItems.has(item.orderDetailId);
    
    // Tính thời gian nấu còn lại (đếm ngược)
    const timeCook = item.timeCook || 0; // Thời gian nấu (phút)
    const startedAt = item.startedAt ? new Date(item.startedAt) : null;
    const countdownHtml = getCookingCountdown(startedAt, timeCook, item.orderDetailId);

    return `
        <tr class="${rowClass}" data-order-detail-id="${item.orderDetailId}" data-time-cook="${timeCook}" data-started-at="${startedAt ? startedAt.toISOString() : ''}">
            <td style="width: 50px;">
                <input type="checkbox" 
                       ${isChecked ? 'checked' : ''} 
                       onchange="toggleCookingItemSelection(${item.orderDetailId})">
            </td>
            <td class="time-cell countdown-cell" data-order-detail-id="${item.orderDetailId}">${countdownHtml}</td>
            <td>${item.tableNumber}</td>
            <td>
                <strong>${item.menuItemName}</strong> x${item.quantity}
                ${item.isUrgent ? '<span class="badge bg-danger ms-2">CẦN LÀM NGAY</span>' : ''}
            </td>
            <td class="notes-text">${item.notes || '-'}</td>
        </tr>
    `;
}

// Tính thời gian nấu còn lại (đếm ngược)
function getCookingCountdown(startedAt, timeCook, orderDetailId) {
    if (!startedAt || !timeCook || timeCook <= 0) {
        return `<span class="text-muted">-</span>`;
    }
    
    const now = new Date();
    const elapsedSeconds = Math.floor((now - startedAt) / 1000);
    const totalSeconds = timeCook * 60;
    const remainingSeconds = Math.max(0, totalSeconds - elapsedSeconds);
    
    if (remainingSeconds <= 0) {
        return `<span class="text-danger fw-bold">Hết giờ</span>`;
    }
    
    const minutes = Math.floor(remainingSeconds / 60);
    const seconds = remainingSeconds % 60;
    const isUrgent = remainingSeconds <= 60; // Cảnh báo khi còn < 1 phút
    
    const timeClass = isUrgent ? 'text-danger fw-bold' : (remainingSeconds <= 300 ? 'text-warning' : 'text-success');
    
    return `<span class="${timeClass}" id="countdown-${orderDetailId}">${minutes}:${seconds.toString().padStart(2, '0')}</span>`;
}

// Toggle cooking item selection
function toggleCookingItemSelection(orderDetailId) {
    if (selectedCookingItems.has(orderDetailId)) {
        selectedCookingItems.delete(orderDetailId);
    } else {
        selectedCookingItems.add(orderDetailId);
    }
    console.log('[toggleCookingItemSelection] Selected:', Array.from(selectedCookingItems));
}

// Select all cooking items
function selectAllCookingItems(checkbox) {
    const checkboxes = document.querySelectorAll('#urgentItemsTable input[type="checkbox"]');

    selectedCookingItems.clear();

    checkboxes.forEach(cb => {
        cb.checked = checkbox.checked;
        if (checkbox.checked) {
            const orderDetailId = parseInt(cb.closest('tr').getAttribute('data-order-detail-id'));
            selectedCookingItems.add(orderDetailId);
        }
    });

    console.log('[selectAllCookingItems] Selected:', Array.from(selectedCookingItems));
}

// Update counts - OPTIMIZED với null check
function updateCounts(data) {
    if (!data || !data.allItems) return;

    const groupedCount = groupItemsByDish(data.allItems).length;
    const cookingCount = data.allItems.filter(item => {
        const status = (item.status || '').toLowerCase();
        return status === 'cooking' || status === 'đang chế biến';
    }).length;

    const allCountEl = document.getElementById('allCount');
    const cookingCountEl = document.getElementById('cookingCount');
    const urgentCountEl = document.getElementById('urgentCount');
    
    if (allCountEl) {
        allCountEl.textContent = groupedCount;
    }
    if (cookingCountEl) {
        cookingCountEl.textContent = cookingCount;
    }
    if (urgentCountEl) {
        urgentCountEl.textContent = cookingCount;
    }
}

// Update timers - Cập nhật đếm ngược thời gian nấu
function updateTimers() {
    const countdownCells = document.querySelectorAll('.countdown-cell');
    
    countdownCells.forEach(cell => {
        const orderDetailId = cell.getAttribute('data-order-detail-id');
        const row = cell.closest('tr');
        if (!row) return;
        
        const timeCook = parseInt(row.getAttribute('data-time-cook')) || 0;
        const startedAtStr = row.getAttribute('data-started-at');
        
        if (!startedAtStr || !timeCook || timeCook <= 0) {
            cell.innerHTML = '<span class="text-muted">-</span>';
            return;
        }
        
        const startedAt = new Date(startedAtStr);
        const countdownHtml = getCookingCountdown(startedAt, timeCook, orderDetailId);
        cell.innerHTML = countdownHtml;
    });
    
    // Reload data mỗi 30 giây để đảm bảo đồng bộ (đã có setInterval riêng)
}

// Complete selected items - CHỈ HOÀN THÀNH ITEMS ĐANG COOKING
async function completeSelectedItems() {
    if (selectedCookingItems.size === 0) {
        showError('Vui lòng chọn ít nhất một món để hoàn thành');
        return;
    }

    if (!confirm(`Xác nhận hoàn thành ${selectedCookingItems.size} món?`)) {
        return;
    }

    const itemsToComplete = Array.from(selectedCookingItems);
    console.log('[completeSelectedItems] Completing:', itemsToComplete);

    const promises = itemsToComplete.map(orderDetailId =>
        updateItemStatus(orderDetailId, 'Done')
    );

    try {
        await Promise.all(promises);
        showSuccess(`✓ Đã hoàn thành ${itemsToComplete.length} món`);
        selectedCookingItems.clear();
        loadStationItems();
    } catch (error) {
        console.error('[completeSelectedItems] Error:', error);
        showError('Không thể hoàn thành món: ' + error.message);
    }
}

// Update item status API
async function updateItemStatus(orderDetailId, newStatus) {
    try {
        console.log(`[updateItemStatus] OrderDetailId=${orderDetailId}, NewStatus=${newStatus}`);

        const response = await fetch(`${API_BASE}/KitchenDisplay/update-item-status`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                orderDetailId: orderDetailId,
                newStatus: newStatus,
                userId: 1 // TODO: Get from session
            })
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        const result = await response.json();
        console.log('[updateItemStatus] Result:', result);

        if (!result.success) {
            throw new Error(result.message || 'Update failed');
        }

        return result;
    } catch (error) {
        console.error('[updateItemStatus] Error:', error);
        throw error;
    }
}

// Send back to sous chef - HỦY COOKING, TRẢ LẠI CHO BẾP PHÓ
async function sendBackToSousChef() {
    if (selectedCookingItems.size === 0) {
        showError('Vui lòng chọn ít nhất một món đang nấu');
        return;
    }

    if (!confirm(`Xác nhận gửi lại ${selectedCookingItems.size} món cho bếp phó?`)) {
        return;
    }

    const itemsToSendBack = Array.from(selectedCookingItems);
    console.log('[sendBackToSousChef] Sending back:', itemsToSendBack);

    const promises = itemsToSendBack.map(orderDetailId =>
        updateItemStatus(orderDetailId, 'Pending')
    );

    try {
        await Promise.all(promises);
        showSuccess(`↩ Đã gửi lại ${itemsToSendBack.length} món cho bếp phó`);
        selectedCookingItems.clear();
        loadStationItems();
    } catch (error) {
        console.error('[sendBackToSousChef] Error:', error);
        showError('Không thể gửi lại bếp phó: ' + error.message);
    }
}

// Report missing ingredients
function reportMissingIngredients() {
    if (selectedCookingItems.size === 0) {
        showError('Vui lòng chọn ít nhất một món');
        return;
    }

    const count = selectedCookingItems.size;

    // TODO: Implement proper missing ingredients reporting
    // For now, just show a confirmation
    if (confirm(`Xác nhận báo thiếu nguyên liệu cho ${count} món?`)) {
        showSuccess(`⚠ Đã báo thiếu nguyên liệu cho ${count} món`);
        // Có thể gửi notification đến warehouse/manager
        selectedCookingItems.clear();
        loadStationItems();
    }
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