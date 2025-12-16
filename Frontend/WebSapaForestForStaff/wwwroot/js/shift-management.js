/**
 * Shift Management Module
 * Handles all client-side logic for Counter Staff Shift Management
 * Author: AI Assistant
 * Date: 2025
 */

// ===========================================
// CONFIGURATION & CONSTANTS
// ===========================================

const API_BASE_URL = '/shift-management';
const TOAST_DURATION = 3000;

// Vietnamese currency denominations
const DENOMINATIONS = [500000, 200000, 100000, 50000, 20000, 10000, 5000, 2000, 1000];

// ===========================================
// UTILITY FUNCTIONS
// ===========================================

/**
 * Format number as Vietnamese currency
 * @param {number} amount - Amount to format
 * @returns {string} Formatted currency string
 */
function formatCurrency(amount) {
    return amount.toLocaleString('vi-VN') + ' ₫';
}

/**
 * Show toast notification
 * @param {string} message - Message to display
 * @param {string} type - Type of toast (success/error/warning/info)
 */
function showToast(message, type = 'success') {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.style.cssText = 'position: fixed; top: 80px; right: 20px; z-index: 9999;';
        document.body.appendChild(container);
    }

    const bgClass = {
        success: 'bg-success',
        error: 'bg-danger',
        warning: 'bg-warning',
        info: 'bg-info'
    }[type] || 'bg-success';

    const icon = {
        success: 'fa-circle-check',
        error: 'fa-circle-exclamation',
        warning: 'fa-exclamation-triangle',
        info: 'fa-info-circle'
    }[type] || 'fa-circle-check';

    const toastHtml = `
        <div class="toast align-items-center text-white ${bgClass} border-0 mb-2 show" role="alert">
            <div class="d-flex">
                <div class="toast-body"><i class="fa-solid ${icon} me-2"></i> ${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>`;
    
    container.insertAdjacentHTML('beforeend', toastHtml);

    setTimeout(() => {
        const toastElement = container.lastElementChild;
        if (toastElement) {
            toastElement.classList.remove('show');
            setTimeout(() => toastElement.remove(), 300);
        }
    }, TOAST_DURATION);
}

/**
 * Make API request with error handling
 * @param {string} url - API endpoint
 * @param {object} options - Fetch options
 * @returns {Promise} Response data
 */
async function apiRequest(url, options = {}) {
    try {
        const defaultOptions = {
            headers: {
                'Content-Type': 'application/json',
            }
        };

        // Add anti-forgery token if it exists
        const token = document.querySelector('[name="__RequestVerificationToken"]');
        if (token) {
            defaultOptions.headers['RequestVerificationToken'] = token.value;
        }

        const response = await fetch(url, { ...defaultOptions, ...options });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        return await response.json();
    } catch (error) {
        console.error('API Request Error:', error);
        showToast('Lỗi kết nối. Vui lòng thử lại.', 'error');
        throw error;
    }
}

// ===========================================
// DASHBOARD FUNCTIONS
// ===========================================

/**
 * Load shift dashboard data
 */
async function loadDashboard() {
    try {
        const data = await apiRequest(`${API_BASE_URL}/api/dashboard`);
        
        // Update dashboard UI
        if (data.currentShift) {
            updateDashboardShiftInfo(data.currentShift);
        }
        
        if (data.statistics) {
            updateDashboardStatistics(data.statistics);
        }
        
        showToast('Đã tải dữ liệu dashboard', 'success');
    } catch (error) {
        showToast('Không thể tải dữ liệu dashboard', 'error');
    }
}

/**
 * Update dashboard shift info
 * @param {object} shiftData - Shift data
 */
function updateDashboardShiftInfo(shiftData) {
    const statusElement = document.getElementById('shift-status');
    const balanceElement = document.getElementById('opening-balance');
    const startTimeElement = document.getElementById('start-time');
    
    if (statusElement) statusElement.textContent = shiftData.status;
    if (balanceElement) balanceElement.textContent = formatCurrency(shiftData.openingBalance);
    if (startTimeElement) startTimeElement.textContent = shiftData.startTime;
}

/**
 * Update dashboard statistics
 * @param {object} stats - Statistics data
 */
function updateDashboardStatistics(stats) {
    const revenueElement = document.getElementById('total-revenue');
    const ordersElement = document.getElementById('total-orders');
    
    if (revenueElement) revenueElement.textContent = formatCurrency(stats.revenue);
    if (ordersElement) ordersElement.textContent = stats.orderCount;
}

// ===========================================
// OPENING SHIFT FLOW
// ===========================================

/**
 * Start opening shift flow
 */
function startOpening() {
    window.location.href = `${API_BASE_URL}/opening`;
}

/**
 * Submit opening balance
 * @param {number} openingBalance - Opening balance amount
 */
async function submitOpeningBalance(openingBalance) {
    if (!openingBalance || openingBalance <= 0) {
        showToast('Số dư đầu ca phải lớn hơn 0', 'error');
        return false;
    }

    try {
        const result = await apiRequest(`${API_BASE_URL}/opening`, {
            method: 'POST',
            body: JSON.stringify({ openingBalance })
        });

        if (result.success) {
            showToast('Đã lưu số dư đầu ca', 'success');
            window.location.href = `${API_BASE_URL}/opening/denominations/${result.shiftId}`;
            return true;
        } else {
            showToast(result.message || 'Lỗi khi lưu số dư đầu ca', 'error');
            return false;
        }
    } catch (error) {
        return false;
    }
}

/**
 * Submit opening denominations
 * @param {number} shiftId - Shift ID
 * @param {object} denominations - Denomination counts
 */
async function submitOpeningDenominations(shiftId, denominations) {
    try {
        const result = await apiRequest(`${API_BASE_URL}/opening/denominations`, {
            method: 'POST',
            body: JSON.stringify({
                shiftId,
                denominations
            })
        });

        if (result.success) {
            showToast('Đã lưu mệnh giá tiền', 'success');
            window.location.href = `${API_BASE_URL}/opening/confirm/${shiftId}`;
            return true;
        } else {
            showToast(result.message || 'Lỗi khi lưu mệnh giá', 'error');
            return false;
        }
    } catch (error) {
        return false;
    }
}

/**
 * Confirm shift opening
 * @param {number} shiftId - Shift ID
 */
async function confirmOpening(shiftId) {
    if (!confirm('Xác nhận mở ca làm việc?')) {
        return false;
    }

    try {
        const result = await apiRequest(`${API_BASE_URL}/opening/confirm`, {
            method: 'POST',
            body: JSON.stringify({ shiftId })
        });

        if (result.success) {
            showToast('Đã mở ca làm việc thành công!', 'success');
            setTimeout(() => {
                window.location.href = API_BASE_URL;
            }, 1000);
            return true;
        } else {
            showToast(result.message || 'Lỗi khi mở ca', 'error');
            return false;
        }
    } catch (error) {
        return false;
    }
}

// ===========================================
// CLOSING SHIFT FLOW
// ===========================================

/**
 * Start closing shift flow
 */
function startClosingFlow() {
    window.location.href = `${API_BASE_URL}/closing`;
}

/**
 * Submit closing denominations
 * @param {number} shiftId - Shift ID
 * @param {object} denominations - Denomination counts
 */
async function submitClosingDenominations(shiftId, denominations) {
    try {
        const result = await apiRequest(`${API_BASE_URL}/closing/denominations`, {
            method: 'POST',
            body: JSON.stringify({
                shiftId,
                denominations
            })
        });

        if (result.success) {
            showToast('Đã kiểm kê tiền cuối ca', 'success');
            // Store difference data for next page
            sessionStorage.setItem('closingDifference', JSON.stringify(result.difference));
            window.location.href = `${API_BASE_URL}/closing/review`;
            return true;
        } else {
            showToast(result.message || 'Lỗi khi kiểm kê', 'error');
            return false;
        }
    } catch (error) {
        return false;
    }
}

/**
 * Calculate closing difference
 * @param {number} expectedAmount - Expected closing balance
 * @param {number} actualAmount - Actual counted balance
 * @returns {object} Difference details
 */
function calculateDifference(expectedAmount, actualAmount) {
    const difference = actualAmount - expectedAmount;
    
    return {
        difference: difference,
        hasDifference: Math.abs(difference) > 0,
        differenceType: difference > 0 ? 'Surplus' : difference < 0 ? 'Shortage' : 'Balanced',
        expectedAmount: expectedAmount,
        actualAmount: actualAmount
    };
}

/**
 * Submit closing notes
 * @param {number} shiftId - Shift ID
 * @param {string} notes - Closing notes
 */
async function submitClosingNotes(shiftId, notes) {
    try {
        const result = await apiRequest(`${API_BASE_URL}/closing/notes`, {
            method: 'POST',
            body: JSON.stringify({
                shiftId,
                notes
            })
        });

        if (result.success) {
            showToast('Đã lưu ghi chú', 'success');
            return true;
        } else {
            showToast(result.message || 'Lỗi khi lưu ghi chú', 'error');
            return false;
        }
    } catch (error) {
        return false;
    }
}

/**
 * Confirm shift closing
 * @param {number} shiftId - Shift ID
 */
async function confirmClosing(shiftId) {
    if (!confirm('Xác nhận kết ca làm việc? Hành động này không thể hoàn tác.')) {
        return false;
    }

    try {
        const result = await apiRequest(`${API_BASE_URL}/closing/confirm`, {
            method: 'POST',
            body: JSON.stringify({ shiftId })
        });

        if (result.success) {
            showToast('Đã kết ca thành công!', 'success');
            
            // Ask if want to handover
            if (confirm('Bạn có muốn bàn giao ca cho nhân viên khác không?')) {
                window.location.href = `${API_BASE_URL}/handover`;
            } else {
                window.location.href = API_BASE_URL;
            }
            return true;
        } else {
            showToast(result.message || 'Lỗi khi kết ca', 'error');
            return false;
        }
    } catch (error) {
        return false;
    }
}

// ===========================================
// HANDOVER SHIFT FLOW
// ===========================================

/**
 * Load available staff for handover
 */
async function loadAvailableHandoverStaff() {
    try {
        const staffList = await apiRequest(`${API_BASE_URL}/handover/available-staff`);
        return staffList;
    } catch (error) {
        showToast('Không thể tải danh sách nhân viên', 'error');
        return [];
    }
}

/**
 * Select handover staff
 * @param {number} toStaffId - Target staff ID
 * @param {string} toStaffName - Target staff name
 */
function selectHandoverStaff(toStaffId, toStaffName) {
    // Store in session
    sessionStorage.setItem('handoverStaffId', toStaffId);
    sessionStorage.setItem('handoverStaffName', toStaffName);
    
    showToast(`Đã chọn ${toStaffName}`, 'success');
    
    // Update UI
    const selectedSummary = document.getElementById('selectedSummary');
    const selectedStaffName = document.getElementById('selectedStaffName');
    const nextBtn = document.getElementById('nextBtn');
    
    if (selectedSummary) selectedSummary.classList.remove('d-none');
    if (selectedStaffName) selectedStaffName.textContent = toStaffName;
    if (nextBtn) nextBtn.disabled = false;
}

/**
 * Submit handover notes
 * @param {number} shiftId - Shift ID
 * @param {number} toStaffId - Target staff ID
 * @param {string} notes - Handover notes
 */
async function submitHandoverNotes(shiftId, toStaffId, notes) {
    try {
        const result = await apiRequest(`${API_BASE_URL}/handover/notes`, {
            method: 'POST',
            body: JSON.stringify({
                shiftId,
                toStaffId,
                handoverNotes: notes
            })
        });

        if (result.success) {
            showToast('Đã lưu ghi chú bàn giao', 'success');
            window.location.href = `${API_BASE_URL}/handover/verify-pin?shiftId=${shiftId}&toStaffId=${toStaffId}`;
            return true;
        } else {
            showToast(result.message || 'Lỗi khi lưu ghi chú', 'error');
            return false;
        }
    } catch (error) {
        return false;
    }
}

/**
 * Verify PIN code
 * @param {number} shiftId - Shift ID
 * @param {string} pinCode - PIN code to verify
 */
async function verifyPin(shiftId, pinCode) {
    if (!pinCode || pinCode.length !== 4) {
        showToast('Vui lòng nhập đầy đủ mã PIN 4 số', 'error');
        return false;
    }

    try {
        const result = await apiRequest(`${API_BASE_URL}/handover/verify-pin`, {
            method: 'POST',
            body: JSON.stringify({
                shiftId,
                pinCode
            })
        });

        if (result.success) {
            showToast('Xác thực thành công!', 'success');
            return true;
        } else {
            showToast(result.message || 'Mã PIN không chính xác', 'error');
            return false;
        }
    } catch (error) {
        return false;
    }
}

/**
 * Create next shift for handover
 * @param {number} shiftId - Current shift ID
 * @param {number} toStaffId - Target staff ID
 */
async function createNextShift(shiftId, toStaffId) {
    try {
        const result = await apiRequest(`${API_BASE_URL}/handover/complete`, {
            method: 'POST',
            body: JSON.stringify({
                shiftId,
                toStaffId
            })
        });

        if (result.success) {
            showToast('Đã hoàn tất bàn giao ca!', 'success');
            setTimeout(() => {
                window.location.href = API_BASE_URL;
            }, 1500);
            return true;
        } else {
            showToast(result.message || 'Lỗi khi tạo ca mới', 'error');
            return false;
        }
    } catch (error) {
        return false;
    }
}

// ===========================================
// SHIFT HISTORY
// ===========================================

/**
 * Load shift history
 * @param {object} filters - Filter options (fromDate, toDate, status)
 */
async function loadShiftHistory(filters = {}) {
    try {
        const params = new URLSearchParams(filters);
        const history = await apiRequest(`${API_BASE_URL}/api/history?${params.toString()}`);
        
        return history;
    } catch (error) {
        showToast('Không thể tải lịch sử ca', 'error');
        return [];
    }
}

/**
 * View shift details
 * @param {number} shiftId - Shift ID
 */
function viewShiftDetails(shiftId) {
    window.location.href = `${API_BASE_URL}/details/${shiftId}`;
}

/**
 * Export shift report
 * @param {number} shiftId - Shift ID
 */
async function exportShiftReport(shiftId) {
    try {
        showToast('Đang tạo báo cáo...', 'info');
        
        const response = await fetch(`${API_BASE_URL}/export/${shiftId}`);
        
        if (response.ok) {
            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `Shift_Report_${shiftId}_${new Date().getTime()}.pdf`;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);
            
            showToast('Đã tải xuống báo cáo!', 'success');
        } else {
            showToast('Không thể tạo báo cáo', 'error');
        }
    } catch (error) {
        showToast('Lỗi khi xuất báo cáo', 'error');
    }
}

// ===========================================
// DENOMINATION CALCULATOR
// ===========================================

/**
 * Calculate total from denominations
 * @param {object} denominations - Denomination counts { value: count }
 * @returns {number} Total amount
 */
function calculateTotalFromDenominations(denominations) {
    let total = 0;
    for (const [value, count] of Object.entries(denominations)) {
        total += parseInt(value) * parseInt(count);
    }
    return total;
}

/**
 * Validate denominations match expected amount
 * @param {object} denominations - Denomination counts
 * @param {number} expectedAmount - Expected total
 * @returns {boolean} True if match
 */
function validateDenominations(denominations, expectedAmount) {
    const total = calculateTotalFromDenominations(denominations);
    return Math.abs(total - expectedAmount) < 1; // Allow small floating point difference
}

// ===========================================
// REAL-TIME UPDATES (Optional)
// ===========================================

/**
 * Setup real-time shift updates via SignalR (if needed)
 */
function setupRealtimeUpdates() {
    // This can be implemented if real-time updates are needed
    // using SignalR or WebSockets
    console.log('Real-time updates not implemented yet');
}

// ===========================================
// INITIALIZATION
// ===========================================

// Export functions for global use
window.ShiftManagement = {
    // Utility
    formatCurrency,
    showToast,
    
    // Dashboard
    loadDashboard,
    
    // Opening
    startOpening,
    submitOpeningBalance,
    submitOpeningDenominations,
    confirmOpening,
    
    // Closing
    startClosingFlow,
    submitClosingDenominations,
    calculateDifference,
    submitClosingNotes,
    confirmClosing,
    
    // Handover
    loadAvailableHandoverStaff,
    selectHandoverStaff,
    submitHandoverNotes,
    verifyPin,
    createNextShift,
    
    // History
    loadShiftHistory,
    viewShiftDetails,
    exportShiftReport,
    
    // Calculator
    calculateTotalFromDenominations,
    validateDenominations
};

// Initialize on DOM ready
document.addEventListener('DOMContentLoaded', function() {
    console.log('Shift Management Module Loaded');
    
    // Setup any global event listeners here
    // setupRealtimeUpdates();
});

