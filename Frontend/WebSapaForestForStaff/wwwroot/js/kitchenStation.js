// Kitchen Station JavaScript - READ-ONLY VERSION
// File: wwwroot/js/kitchenStation.js
// Trạm CHỈ XEM và HOÀN THÀNH món, KHÔNG được tự fire

const API_BASE = window.API_BASE_URL || 'https://localhost:7096/api';
let signalRConnection = null;
let currentCategoryName = '';
let currentData = null;
let selectedCookingItems = new Set(); // Chỉ select items đang cooking để hoàn thành

// Initialize station
function initializeStation(categoryName) {
    currentCategoryName = categoryName;
    initializeSignalR();
    loadStationItems();

    // Auto-refresh every 30 seconds
    setInterval(loadStationItems, 30000);

    // Update timers every minute
    setInterval(updateTimers, 60000);
}

// SignalR Setup
function initializeSignalR() {
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

    signalRConnection.start()
        .then(() => console.log('SignalR connected to kitchen hub'))
        .catch(err => console.error('SignalR connection error:', err));
}

// Load station items from API
async function loadStationItems() {
    try {
        if (!currentCategoryName || currentCategoryName.trim() === '') {
            console.error('Category name is empty!');
            showError('Tên trạm không hợp lệ');
            return;
        }

        console.log('[loadStationItems] Loading for category:', currentCategoryName);
        const url = `${API_BASE}/KitchenDisplay/station-items?categoryName=${encodeURIComponent(currentCategoryName)}`;

        const response = await fetch(url);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        console.log('[loadStationItems] API Response:', result);

        if (result.success) {
            currentData = result.data;
            console.log('[loadStationItems] Total items:', currentData.allItems?.length || 0);

            if (!currentData.allItems) {
                currentData.allItems = [];
            }

            renderStationItems(currentData);
            updateCounts(currentData);
        } else {
            console.error('API returned error:', result.message);
            showError(result.message || 'Không thể tải dữ liệu trạm');
        }
    } catch (error) {
        console.error('[loadStationItems] Error:', error);
        showError('Lỗi kết nối API: ' + error.message);
    }
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
    const fireTime = item.fireTime || item.createdAtTime || '-';
    const rowClass = item.isUrgent ? 'urgent-row' : '';
    const isChecked = selectedCookingItems.has(item.orderDetailId);

    return `
        <tr class="${rowClass}" data-order-detail-id="${item.orderDetailId}">
            <td style="width: 50px;">
                <input type="checkbox" 
                       ${isChecked ? 'checked' : ''} 
                       onchange="toggleCookingItemSelection(${item.orderDetailId})">
            </td>
            <td class="time-cell">${fireTime}</td>
            <td>${item.tableNumber}</td>
            <td>
                <strong>${item.menuItemName}</strong> x${item.quantity}
                ${item.isUrgent ? '<span class="badge bg-danger ms-2">RUSH</span>' : ''}
            </td>
            <td class="notes-text">${item.notes || '-'}</td>
        </tr>
    `;
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

// Update counts
function updateCounts(data) {
    if (!data || !data.allItems) return;

    const groupedCount = groupItemsByDish(data.allItems).length;
    const cookingCount = data.allItems.filter(item => {
        const status = (item.status || '').toLowerCase();
        return status === 'cooking';
    }).length;

    document.getElementById('allCount').textContent = groupedCount;
    document.getElementById('cookingCount').textContent = cookingCount;
}

// Update timers
function updateTimers() {
    loadStationItems();
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