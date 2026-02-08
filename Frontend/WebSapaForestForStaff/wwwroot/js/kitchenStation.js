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
// Lưu cả orderDetailId và orderComboItemId để xử lý combo items
// Format: "orderDetailId|orderComboItemId" hoặc "orderDetailId|" (nếu không có orderComboItemId)
let selectedCookingItems = new Set(); // Set<string> - format: "orderDetailId|orderComboItemId"
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

        // Gắn click để mở popup công thức cho từng món
        const nameCells = cookingTable.querySelectorAll('.station-menu-item-name[data-menu-item-id]');
        nameCells.forEach(el => {
            el.addEventListener('click', function (e) {
                e.stopPropagation();
                const menuItemIdAttr = this.getAttribute('data-menu-item-id');
                const menuItemId = menuItemIdAttr ? parseInt(menuItemIdAttr) : NaN;
                const menuItemName = this.textContent.trim();
                if (!isNaN(menuItemId) && menuItemId > 0) {
                    openRecipePopup(menuItemId, menuItemName);
                } else {
                    showError('Không tìm được thông tin công thức cho món này');
                }
            });
        });
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
    // Tạo key: "orderDetailId|orderComboItemId" hoặc "orderDetailId|" nếu không có orderComboItemId
    const itemKey = `${item.orderDetailId}|${item.orderComboItemId || ''}`;
    const isChecked = selectedCookingItems.has(itemKey);
    
    // Tính thời gian nấu còn lại (đếm ngược)
    // Dùng orderComboItemId nếu có, ngược lại dùng orderDetailId
    const itemId = item.orderComboItemId || item.orderDetailId;
    const timeCook = item.timeCook || 0; // Thời gian nấu (phút)
    const startedAt = item.startedAt ? new Date(item.startedAt) : null;
    const countdownHtml = getCookingCountdown(startedAt, timeCook, itemId);

    return `
        <tr class="${rowClass}" 
            data-order-detail-id="${item.orderDetailId}" 
            data-order-combo-item-id="${item.orderComboItemId || ''}"
            data-menu-item-id="${item.menuItemId || ''}"
            data-item-key="${itemKey}"
            data-time-cook="${timeCook}" 
            data-started-at="${startedAt ? startedAt.toISOString() : ''}">
            <td style="width: 60px;">
                <input type="checkbox" 
                       style="width: 20px; height: 20px; cursor: pointer;"
                       ${isChecked ? 'checked' : ''} 
                       onchange="toggleCookingItemSelection('${itemKey}')">
            </td>
            <td class="time-cell countdown-cell" data-item-id="${itemId}" style="font-size: 22px;">${countdownHtml}</td>
            <td style="font-size: 18px; font-weight: 600;">${item.tableNumber}</td>
            <td style="font-size: 18px;">
                <strong class="station-menu-item-name"
                        data-menu-item-id="${item.menuItemId || ''}"
                        style="font-size: 20px; cursor: pointer; text-decoration: underline dotted;">
                    ${item.menuItemName}
                </strong>
                <span style="font-size: 18px; color: #ff9800; font-weight: 600;">x${item.quantity}</span>
                ${item.isUrgent ? '<span class="badge bg-danger ms-2" style="font-size: 14px; padding: 4px 8px;">CẦN LÀM NGAY</span>' : ''}
            </td>
            <td class="notes-text" style="font-size: 16px;">${item.notes || '-'}</td>
        </tr>
    `;
}

// Tính thời gian nấu còn lại (đếm ngược)
function getCookingCountdown(startedAt, timeCook, itemId) {
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
    
    return `<span class="${timeClass}" id="countdown-${itemId}">${minutes}:${seconds.toString().padStart(2, '0')}</span>`;
}

// Toggle cooking item selection
// itemKey format: "orderDetailId|orderComboItemId" hoặc "orderDetailId|"
function toggleCookingItemSelection(itemKey) {
    if (selectedCookingItems.has(itemKey)) {
        selectedCookingItems.delete(itemKey);
    } else {
        selectedCookingItems.add(itemKey);
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
            const row = cb.closest('tr');
            const itemKey = row.getAttribute('data-item-key');
            if (itemKey) {
                selectedCookingItems.add(itemKey);
            }
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
        const itemId = cell.getAttribute('data-item-id');
        const row = cell.closest('tr');
        if (!row) return;
        
        const timeCook = parseInt(row.getAttribute('data-time-cook')) || 0;
        const startedAtStr = row.getAttribute('data-started-at');
        
        if (!startedAtStr || !timeCook || timeCook <= 0) {
            cell.innerHTML = '<span class="text-muted">-</span>';
            return;
        }
        
        const startedAt = new Date(startedAtStr);
        const countdownHtml = getCookingCountdown(startedAt, timeCook, itemId);
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

    const confirmed = await showConfirmPopup(`Xác nhận hoàn thành ${selectedCookingItems.size} món?`, 'Xác nhận hoàn thành');
    if (!confirmed) {
        return;
    }

    const itemsToComplete = Array.from(selectedCookingItems);
    console.log('[completeSelectedItems] Completing:', itemsToComplete);

    // Parse itemKey: "orderDetailId|orderComboItemId" hoặc "orderDetailId|"
    const promises = itemsToComplete.map(itemKey => {
        const [orderDetailId, orderComboItemId] = itemKey.split('|');
        return updateItemStatus(parseInt(orderDetailId), 'Ready', orderComboItemId ? parseInt(orderComboItemId) : null);
    });

    try {
        await Promise.all(promises);
        showSuccess(`✓ Đã hoàn thành ${itemsToComplete.length} món`);
        
        // In ticket cho từng món đã hoàn thành (song song với delay nhỏ)
        const printPromises = itemsToComplete.map((itemKey, index) => {
            const [orderDetailId, orderComboItemId] = itemKey.split('|');
            // Delay nhỏ giữa các lần in để tránh mở quá nhiều cửa sổ cùng lúc
            return new Promise(resolve => {
                setTimeout(async () => {
                    try {
                        await printItemTicket(parseInt(orderDetailId), orderComboItemId ? parseInt(orderComboItemId) : null);
                    } catch (printError) {
                        console.error('[completeSelectedItems] Print error:', printError);
                        // Không throw để không ảnh hưởng đến việc hoàn thành món
                    }
                    resolve();
                }, index * 500); // Delay 500ms giữa mỗi lần in
            });
        });
        
        // Đợi tất cả các ticket được in (không block UI)
        Promise.all(printPromises).catch(err => {
            console.error('[completeSelectedItems] Print promises error:', err);
        });
        
        selectedCookingItems.clear();
        loadStationItems();
    } catch (error) {
        console.error('[completeSelectedItems] Error:', error);
        showError('Không thể hoàn thành món: ' + error.message);
    }
}

// Update item status API
// orderComboItemId: null/undefined = món lẻ (OrderDetail), có giá trị = món trong combo (OrderComboItem)
async function updateItemStatus(orderDetailId, newStatus, orderComboItemId = null) {
    try {
        console.log(`[updateItemStatus] OrderDetailId=${orderDetailId}, OrderComboItemId=${orderComboItemId}, NewStatus=${newStatus}`);

        const response = await fetch(`${API_BASE}/KitchenDisplay/update-item-status`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                orderDetailId: orderDetailId,
                orderComboItemId: orderComboItemId,
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

// Print item ticket khi hoàn thành món
async function printItemTicket(orderDetailId, orderComboItemId = null) {
    try {
        console.log(`[printItemTicket] OrderDetailId=${orderDetailId}, OrderComboItemId=${orderComboItemId}`);

        const response = await fetch(`${API_BASE}/KitchenDisplay/print-item-ticket`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                orderDetailId: orderDetailId,
                orderComboItemId: orderComboItemId
            })
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        const result = await response.json();
        console.log('[printItemTicket] Result:', result);

        if (!result.success || !result.data) {
            console.warn('[printItemTicket] No data to print');
            return;
        }

        const ticket = result.data;
        
        // Tạo nội dung ticket để in
        const printContent = `
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="UTF-8">
                <title>Ticket - ${ticket.menuItemName}</title>
                <style>
                    @media print {
                        @page { size: 80mm auto; margin: 0; }
                        body { margin: 5mm; font-size: 12px; }
                    }
                    body {
                        font-family: Arial, sans-serif;
                        margin: 0;
                        padding: 10px;
                        font-size: 12px;
                    }
                    .ticket-header {
                        text-align: center;
                        border-bottom: 2px dashed #000;
                        padding-bottom: 10px;
                        margin-bottom: 10px;
                    }
                    .ticket-title {
                        font-size: 16px;
                        font-weight: bold;
                        margin-bottom: 5px;
                    }
                    .ticket-info {
                        margin: 5px 0;
                    }
                    .ticket-item {
                        margin: 10px 0;
                        padding: 10px 0;
                        border-bottom: 1px dashed #ccc;
                    }
                    .item-name {
                        font-size: 14px;
                        font-weight: bold;
                        margin-bottom: 5px;
                    }
                    .item-details {
                        font-size: 11px;
                        color: #666;
                    }
                    .ticket-footer {
                        text-align: center;
                        margin-top: 15px;
                        padding-top: 10px;
                        border-top: 2px dashed #000;
                        font-size: 10px;
                    }
                </style>
            </head>
            <body>
                <div class="ticket-header">
                    <div class="ticket-title">PHIẾU HOÀN THÀNH MÓN</div>
                    <div class="ticket-info">Đơn: ${ticket.orderNumber}</div>
                    <div class="ticket-info">Bàn: ${ticket.tableNumber}</div>
                    <div class="ticket-info">Trạm: ${ticket.stationName}</div>
                </div>
                <div class="ticket-item">
                    <div class="item-name">${ticket.menuItemName} x${ticket.quantity}</div>
                    ${ticket.notes ? `<div class="item-details">Ghi chú: ${ticket.notes}</div>` : ''}
                    <div class="item-details">Hoàn thành: ${new Date(ticket.completedAt).toLocaleString('vi-VN')}</div>
                </div>
                <div class="ticket-footer">
                    <div>Cảm ơn bạn!</div>
                </div>
            </body>
            </html>
        `;

        // Mở cửa sổ in
        const printWindow = window.open('', '_blank');
        printWindow.document.write(printContent);
        printWindow.document.close();
        
        // Đợi một chút để nội dung load xong, sau đó in
        setTimeout(() => {
            printWindow.print();
            // Đóng cửa sổ sau khi in (tùy chọn)
            // printWindow.close();
        }, 250);
    } catch (error) {
        console.error('[printItemTicket] Error:', error);
        // Không throw để không ảnh hưởng đến việc hoàn thành món
    }
}

// Send back to sous chef - HỦY COOKING, TRẢ LẠI CHO BẾP PHÓ
async function sendBackToSousChef() {
    if (selectedCookingItems.size === 0) {
        showError('Vui lòng chọn ít nhất một món đang nấu');
        return;
    }

    const confirmed = await showConfirmPopup(`Xác nhận gửi lại ${selectedCookingItems.size} món cho bếp phó?`, 'Gửi lại bếp phó');
    if (!confirmed) {
        return;
    }

    const itemsToSendBack = Array.from(selectedCookingItems);
    console.log('[sendBackToSousChef] Sending back:', itemsToSendBack);

    // Parse itemKey: "orderDetailId|orderComboItemId" hoặc "orderDetailId|"
    const promises = itemsToSendBack.map(itemKey => {
        const [orderDetailId, orderComboItemId] = itemKey.split('|');
        return updateItemStatus(parseInt(orderDetailId), 'Pending', orderComboItemId ? parseInt(orderComboItemId) : null);
    });

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
async function reportMissingIngredients() {
    if (selectedCookingItems.size === 0) {
        showError('Vui lòng chọn ít nhất một món');
        return;
    }

    const count = selectedCookingItems.size;

    // TODO: Implement proper missing ingredients reporting
    // For now, just show a confirmation
    const confirmed = await showConfirmPopup(`Xác nhận báo thiếu nguyên liệu cho ${count} món?`, 'Báo thiếu nguyên liệu');
    if (confirmed) {
        showSuccess(`⚠ Đã báo thiếu nguyên liệu cho ${count} món`);
        // Có thể gửi notification đến warehouse/manager
        selectedCookingItems.clear();
        loadStationItems();
    }
}

// Toast notifications
function showSuccess(message) {
    if (typeof toastr !== 'undefined') {
        toastr.success(message, '', {
            closeButton: true,
            progressBar: true,
            timeOut: 5000,
            extendedTimeOut: 0,
            positionClass: 'toast-top-right',
            escapeHtml: false
        });
    } else {
        console.log('SUCCESS:', message);
    }
}

function showError(message) {
    if (typeof toastr !== 'undefined') {
        toastr.error(message, '', {
            closeButton: true,
            progressBar: true,
            timeOut: 5000,
            extendedTimeOut: 0,
            positionClass: 'toast-top-right',
            escapeHtml: false
        });
    } else {
        console.error('ERROR:', message);
    }
}

// Popup hiển thị công thức món ăn
async function openRecipePopup(menuItemId, menuItemName) {
    try {
        console.log('[openRecipePopup] menuItemId =', menuItemId);

        const response = await fetch(`${API_BASE}/ManagerMenu/recipes/${menuItemId}`);
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        const recipes = await response.json();

        if (!recipes || recipes.length === 0) {
            showError(`Món "${menuItemName}" chưa được cấu hình công thức`);
            return;
        }

        const rowsHtml = recipes.map((r, index) => {
            const ingredientName = r.ingredient?.name || r.ingredient?.IngredientName || 'Nguyên liệu';
            const unitName = r.ingredient?.unit?.unitName || r.ingredient?.unit?.UnitName || '';
            const qty = r.quantityNeeded ?? r.quantity ?? 0;

            return `
                <tr>
                    <td style="padding: 6px 8px; text-align: center;">${index + 1}</td>
                    <td style="padding: 6px 8px;">${ingredientName}</td>
                    <td style="padding: 6px 8px; text-align: right;">${qty}</td>
                    <td style="padding: 6px 8px;">${unitName}</td>
                </tr>
            `;
        }).join('');

        const overlay = document.createElement('div');
        overlay.className = 'recipe-popup-overlay';

        overlay.innerHTML = `
            <div class="recipe-popup">
                <div class="recipe-popup-header">
                    <div class="recipe-popup-icon">
                        <i class="mdi mdi-book-open-page-variant"></i>
                    </div>
                    <div class="recipe-popup-title">
                        Công thức: ${menuItemName}
                    </div>
                    <button type="button" class="recipe-popup-close">&times;</button>
                </div>
                <div class="recipe-popup-body">
                    <table class="recipe-table">
                        <thead>
                            <tr>
                                <th>#</th>
                                <th>Nguyên liệu</th>
                                <th>Số lượng</th>
                                <th>Đơn vị</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${rowsHtml}
                        </tbody>
                    </table>
                </div>
                <div class="recipe-popup-footer">
                    <button type="button" class="recipe-popup-btn recipe-popup-btn-close">Đóng</button>
                </div>
            </div>
        `;

        document.body.appendChild(overlay);

        const closePopup = () => {
            overlay.style.opacity = '0';
            setTimeout(() => {
                if (document.body.contains(overlay)) {
                    document.body.removeChild(overlay);
                }
            }, 200);
        };

        overlay.addEventListener('click', e => {
            if (e.target === overlay) {
                closePopup();
            }
        });

        overlay.querySelector('.recipe-popup-close')
            .addEventListener('click', closePopup);
        overlay.querySelector('.recipe-popup-btn-close')
            .addEventListener('click', closePopup);
    } catch (error) {
        console.error('[openRecipePopup] Error:', error);
        showError('Không tải được công thức món ăn');
    }
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