# Payment Frontend-Backend Integration Summary

## ✅ Integration Complete

The Payment Frontend (UI) and Payment Backend (API) are now fully integrated into a single working flow for the POS system.

---

## 🔄 Complete Payment Flow

### Step-by-Step Process:

1. **Cashier opens order** → Views order details in `OrderDetail.cshtml`
2. **Cashier clicks "Thanh toán QR"** → Triggers `openQrPayment(orderId)`
3. **Frontend calls backend** → `GET /api/payment/qr/{orderId}`
4. **Backend generates QR** → Creates transaction, generates VietQR URL
5. **Frontend displays QR** → Shows QR image, amount, description in modal
6. **Customer scans QR** → Transfers money via banking app
7. **Cashier clicks "Đã nhận tiền"** → Triggers `confirmQRPayment()`
8. **Frontend calls backend** → `POST /api/payment/confirm`
9. **Backend updates status** → Order status = "PAID", Transaction status = "Paid"
10. **Frontend refreshes** → Page reloads, shows updated payment status

---

## 📡 API Endpoints

### 1. Generate QR Code
**Endpoint**: `GET /api/payment/qr/{orderId}`

**Request**:
```http
GET /api/payment/qr/1024
Authorization: Bearer {token}
```

**Response**:
```json
{
  "qrUrl": "https://img.vietqr.io/image/VCB-0123456789-compact2.png?amount=350000&addInfo=RMS%23ORD1024",
  "amount": 350000,
  "description": "RMS#ORD1024",
  "orderId": 1024,
  "orderCode": "RMS0001024",
  "transactionId": 123,
  "transactionCode": "TXN-20250115123456-1024-ABC12345"
}
```

**Backend Processing**:
- Validates order exists
- Creates transaction with status "PaymentProcessing"
- Generates VietQR URL using bank settings
- Returns QR data for frontend

---

### 2. Confirm Payment
**Endpoint**: `POST /api/payment/confirm`

**Request**:
```http
POST /api/payment/confirm
Authorization: Bearer {token}
Content-Type: application/json

{
  "orderId": 1024,
  "transactionId": 123,
  "gatewayReference": null,
  "notes": "Thanh toán qua QR VietQR - Xác nhận thủ công"
}
```

**Response**:
```json
{
  "success": true,
  "message": "Xác nhận thanh toán thành công",
  "status": "PAID",
  "transaction": {
    "transactionId": 123,
    "orderId": 1024,
    "status": "Paid",
    "amount": 350000,
    ...
  }
}
```

**Backend Processing**:
- Validates transaction exists
- Updates transaction status to "Paid"
- Updates order status to "Paid"
- Marks as manually confirmed
- Triggers post-payment actions
- Unlocks order

---

## 🎨 Frontend Implementation

### JavaScript Functions

#### `openQrPayment(orderId)`
- **Location**: `OrderDetail.cshtml`
- **Purpose**: Opens QR modal and fetches QR code from backend
- **Steps**:
  1. Validates order ID
  2. Shows modal with loading state
  3. Fetches QR data from `/api/payment/qr/{orderId}`
  4. Updates UI with QR image, amount, description
  5. Shows confirm button
  6. Handles errors with retry option

#### `confirmQRPayment()`
- **Location**: `OrderDetail.cshtml`
- **Purpose**: Confirms payment manually after customer transfers
- **Steps**:
  1. Validates payment data
  2. Disables button, shows loading
  3. Sends confirmation to `/api/payment/confirm`
  4. Shows success message
  5. Closes modal
  6. Reloads page to show updated status

#### `retryLoadQR()`
- **Purpose**: Retries loading QR if initial fetch failed

---

## 🖼️ UI Components

### QR Payment Modal
**File**: `_QRPaymentModal.cshtml`

**Features**:
- ✅ Loading state with spinner
- ✅ QR image display
- ✅ Payment information (amount, description, transaction code)
- ✅ Instructions for cashier
- ✅ "Đã nhận tiền" confirmation button
- ✅ Error state with retry option
- ✅ "Hủy" button to close modal

**States**:
1. **Loading**: Shows spinner while fetching QR
2. **Content**: Displays QR code and payment info
3. **Error**: Shows error message with retry button

---

## 🔗 Data Flow

### Request/Response Mapping

#### QR Generation Flow:
```
Frontend (openQrPayment)
  ↓
GET /api/payment/qr/{orderId}
  ↓
Backend (GenerateQRForManualConfirmation)
  ↓
PaymentService.StartPaymentAsync()
  ↓
PaymentService.GenerateVietQRAsync()
  ↓
Response: { qrUrl, amount, description, orderId, transactionId, ... }
  ↓
Frontend (Display QR in modal)
```

#### Confirmation Flow:
```
Frontend (confirmQRPayment)
  ↓
POST /api/payment/confirm
  Body: { orderId, transactionId, gatewayReference, notes }
  ↓
Backend (ConfirmPayment)
  ↓
PaymentService.ConfirmManualAsync()
  ↓
Updates: Order.Status = "Paid", Transaction.Status = "Paid"
  ↓
Response: { success: true, message: "...", status: "PAID" }
  ↓
Frontend (Show success, reload page)
```

---

## ✅ Integration Checklist

### Backend ✅
- [x] Endpoint `/api/payment/qr/{orderId}` returns correct JSON structure
- [x] Endpoint `/api/payment/confirm` accepts correct payload
- [x] Backend updates order status to "PAID"
- [x] Backend creates transaction record
- [x] Backend returns success response with status
- [x] Error handling for invalid requests
- [x] Authentication required for both endpoints

### Frontend ✅
- [x] `openQrPayment()` calls correct API endpoint
- [x] QR modal displays QR image correctly
- [x] Payment info (amount, description) displayed
- [x] `confirmQRPayment()` sends correct payload
- [x] Success message shown after confirmation
- [x] Page reloads to show updated status
- [x] Error handling with user-friendly messages
- [x] Loading states during API calls
- [x] Button disabled during confirmation

### UI/UX ✅
- [x] Loading spinner while fetching QR
- [x] Error state with retry option
- [x] Success toast notification
- [x] Modal closes after confirmation
- [x] Page refreshes to show updated status
- [x] Payment status banner updates

---

## 🧪 Testing Guide

### Test Scenario 1: Generate QR
1. Open order detail page
2. Click "Thanh toán QR" button
3. **Expected**: Modal opens, loading spinner shows
4. **Expected**: QR code loads and displays
5. **Expected**: Amount and description shown correctly

### Test Scenario 2: Confirm Payment
1. After QR is displayed, click "Đã nhận tiền"
2. **Expected**: Button shows loading state
3. **Expected**: Success message appears
4. **Expected**: Modal closes
5. **Expected**: Page reloads
6. **Expected**: Order status shows "Paid"

### Test Scenario 3: Error Handling
1. Try with invalid order ID
2. **Expected**: Error message shown
3. **Expected**: Retry button available
4. **Expected**: Modal can be closed

---

## 📝 Code Comments

All major integration steps are clearly commented:

### Frontend Comments:
- Step 1.1-1.7: QR generation flow
- Step 2.1-2.10: Payment confirmation flow

### Backend Comments:
- Step 1-5: QR generation process
- Integration notes in endpoint summaries

---

## 🔧 Configuration

### Bank Settings (`appsettings.json`):
```json
{
  "BankSettings": {
    "BankCode": "VCB",
    "Account": "0123456789"
  }
}
```

### API Base URL:
- Frontend uses `window.API_BASE_URL` or server-provided value
- Default: `https://localhost:7000/api`

---

## 🚀 Usage

### For Cashiers:

1. **Open Order** → Navigate to order detail page
2. **Click "Thanh toán QR"** → Modal opens with QR code
3. **Show QR to Customer** → Customer scans and transfers money
4. **Click "Đã nhận tiền"** → Confirm payment after customer confirms transfer
5. **Verify Status** → Page reloads, order shows as "Paid"

---

## 📊 Integration Status

✅ **Fully Integrated and Working**

- Frontend and backend communicate seamlessly
- Data structures match perfectly
- Error handling implemented
- UI updates correctly
- Payment flow complete end-to-end

---

**Last Updated**: 2025-01-XX  
**Status**: ✅ Complete and Tested

