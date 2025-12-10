# 📱 SHIFT MANAGEMENT - SCREEN FLOW DIAGRAM

## 🏠 Counter Staff Dashboard (Index.cshtml)

```
┌─────────────────────────────────────────────────────────────┐
│                  COUNTER STAFF DASHBOARD                     │
│                                                               │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐│
│  │ Current Shift   │  │ Today Revenue   │  │ Total Orders ││
│  │ Status: Open    │  │ 5,000,000 ₫     │  │ 25 orders    ││
│  └─────────────────┘  └─────────────────┘  └──────────────┘│
│                                                               │
│  Quick Actions:                                              │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐           │
│  │ 🟢 Open     │ │ 🔴 Close    │ │ 🟠 Handover │           │
│  │ Shift       │ │ Shift       │ │ Shift       │           │
│  └─────────────┘ └─────────────┘ └─────────────┘           │
│                                                               │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐           │
│  │ 📋 Table    │ │ 📅 Reserve  │ │ 🕒 History  │           │
│  │ Status      │ │ Confirm     │ │            │           │
│  └─────────────┘ └─────────────┘ └─────────────┘           │
└─────────────────────────────────────────────────────────────┘
                           │
         ┌─────────────────┼─────────────────┐
         │                 │                 │
         ▼                 ▼                 ▼
    OPEN FLOW         CLOSE FLOW       HANDOVER FLOW
```

---

## 🟢 OPENING SHIFT FLOW (UC125-127)

```
┌─────────────────────────────────────────────────────────────┐
│ STEP 1/3: Declare Opening Balance (Opening.cshtml)          │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  📊 Enter Opening Balance:                                   │
│  ┌────────────────────────────────────┐                     │
│  │  5,000,000 ₫                       │                     │
│  └────────────────────────────────────┘                     │
│                                                               │
│  ⚠️  Rules:                                                  │
│  - Must be > 0                                               │
│  - Staff can't have 2 open shifts                           │
│                                                               │
│  [Cancel] [Next: Count Denominations →]                     │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ STEP 2/3: Count Denominations (OpeningDenominations.cshtml) │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  💵 Enter quantity for each denomination:                   │
│                                                               │
│  500,000 ₫  [10 notes] = 5,000,000 ₫                        │
│  200,000 ₫  [ 0 notes] =         0 ₫                        │
│  100,000 ₫  [ 0 notes] =         0 ₫                        │
│  ...                                                          │
│                                                               │
│  ═══════════════════════════════════                         │
│  Total Counted: 5,000,000 ₫                                 │
│  Expected:      5,000,000 ₫                                 │
│  ✅ Match! Ready to proceed                                 │
│                                                               │
│  [← Back] [Next: Confirm →]                                 │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ STEP 3/3: Confirm Opening (OpeningConfirm.cshtml)           │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ✅ Shift Information Summary:                              │
│                                                               │
│  Staff:         Nguyen Van A                                │
│  Date:          10/12/2025                                  │
│  Start Time:    08:00                                       │
│  Opening Bal:   5,000,000 ₫                                 │
│                                                               │
│  🔐 Denominations verified                                  │
│                                                               │
│  ⚠️  After confirmation:                                    │
│  - Shift will be opened                                     │
│  - You can start processing payments                        │
│                                                               │
│  [← Back] [✅ Confirm & Open Shift]                         │
└─────────────────────────────────────────────────────────────┘
                           ↓
                   🎉 Shift Opened!
                  Return to Dashboard
```

---

## 🔴 CLOSING SHIFT FLOW (UC128-131)

```
┌─────────────────────────────────────────────────────────────┐
│ STEP 1/4: Shift Overview (Closing.cshtml)                   │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  📊 Current Shift Summary:                                  │
│                                                               │
│  Staff:           Nguyen Van A                              │
│  Date:            10/12/2025                                │
│  Start Time:      08:00                                     │
│  Opening Balance: 5,000,000 ₫                               │
│                                                               │
│  📈 Revenue This Shift:                                     │
│  Total Revenue:   3,500,000 ₫                               │
│  Total Orders:    25 orders                                 │
│                                                               │
│  ⚠️  Before closing:                                        │
│  - Ensure all orders are paid                               │
│  - Check cash drawer                                        │
│                                                               │
│  [← Back] [Start Counting →]                                │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ STEP 2/4: Count Closing Cash (ClosingDenominations.cshtml)  │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  💰 Reference:                                              │
│  Opening Balance:  5,000,000 ₫                              │
│  Expected Revenue: 3,500,000 ₫                              │
│  Expected Total:   8,500,000 ₫                              │
│                                                               │
│  💵 Count actual cash in drawer:                            │
│                                                               │
│  500,000 ₫  [17 notes] = 8,500,000 ₫                        │
│  200,000 ₫  [ 0 notes] =         0 ₫                        │
│  ...                                                          │
│                                                               │
│  ═══════════════════════════════════                         │
│  Total Counted: 8,450,000 ₫                                 │
│                                                               │
│  [← Back] [Next: Review →]                                  │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ STEP 3/4: Review Difference (ClosingReview.cshtml)          │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  📊 Comparison:                                             │
│                                                               │
│  Opening Balance:      5,000,000 ₫                          │
│  + Cash Revenue:       3,500,000 ₫                          │
│  ═══════════════════════════════════                         │
│  Expected Closing:     8,500,000 ₫                          │
│  Actual Closing:       8,450,000 ₫                          │
│  ───────────────────────────────────                         │
│  ⚠️  SHORTAGE:         -50,000 ₫                            │
│                                                               │
│  📝 Notes (Required for difference):                        │
│  ┌────────────────────────────────────┐                     │
│  │ Gave change to customer, short     │                     │
│  │ 50k note from personal cash        │                     │
│  └────────────────────────────────────┘                     │
│                                                               │
│  [← Recount] [Next: Confirm →]                              │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ STEP 4/4: Confirm Closing (ClosingConfirm.cshtml)           │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ✅ Shift Summary:                                          │
│                                                               │
│  Staff:           Nguyen Van A                              │
│  Date:            10/12/2025                                │
│  Start Time:      08:00                                     │
│  End Time:        17:00 (9h 0m)                             │
│                                                               │
│  Opening Balance: 5,000,000 ₫                               │
│  Closing Balance: 8,450,000 ₫                               │
│  Shortage:        -50,000 ₫                                 │
│  ───────────────────────────────────                         │
│  Total Revenue:   3,450,000 ₫                               │
│                                                               │
│  📝 Notes: [Shortage explanation saved]                     │
│                                                               │
│  ⚠️  This action cannot be undone!                          │
│                                                               │
│  [← Back] [✅ Confirm & Close Shift]                        │
└─────────────────────────────────────────────────────────────┘
                           ↓
              🎉 Shift Closed Successfully!
                           ↓
           ┌────────────────────────────┐
           │ Want to handover to next   │
           │ staff?                     │
           │ [Yes] [No, Back to Dash]   │
           └────────────────────────────┘
```

---

## 🟠 HANDOVER SHIFT FLOW (UC132-135)

```
┌─────────────────────────────────────────────────────────────┐
│ STEP 1/4: Select Staff (Handover.cshtml)                    │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  Current Shift: Nguyen Van A                                │
│  Closing Balance: 8,450,000 ₫                               │
│                                                               │
│  👥 Select staff to receive shift:                          │
│                                                               │
│  ┌────────────────────────────────────────────┐            │
│  │ 👤 Tran Van B                              │            │
│  │    Counter Staff                           │            │
│  │    Status: ✅ Available                    │ [Select]  │
│  └────────────────────────────────────────────┘            │
│                                                               │
│  ┌────────────────────────────────────────────┐            │
│  │ 👤 Le Thi C                                │            │
│  │    Counter Staff                           │            │
│  │    Status: ❌ Already has open shift      │            │
│  └────────────────────────────────────────────┘            │
│                                                               │
│  [← Back] [Next: Add Notes →]                               │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ STEP 2/4: Handover Notes (HandoverNotes.cshtml)             │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  🔄 Handover to: Tran Van B                                 │
│                                                               │
│  📝 Add notes for next shift (optional):                    │
│  ┌────────────────────────────────────┐                     │
│  │ - Cash counted and verified        │                     │
│  │ - 2 orders waiting for pickup      │                     │
│  │ - POS machine working normally     │                     │
│  │                                    │                     │
│  └────────────────────────────────────┘                     │
│                                                               │
│  💡 Quick suggestions:                                      │
│  [+ Cash verified]                                           │
│  [+ Pending orders]                                          │
│  [+ POS working]                                             │
│  [+ Customer reservation at 18:00]                          │
│                                                               │
│  [← Back] [Next: Verify PIN →]                              │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ STEP 3/4: Verify PIN (HandoverVerifyPin.cshtml)             │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  🔐 Security Verification                                   │
│                                                               │
│  Verify identity of: Nguyen Van A                           │
│  to complete shift handover                                 │
│                                                               │
│  Enter 4-digit PIN:                                         │
│                                                               │
│     ┌───┐ ┌───┐ ┌───┐ ┌───┐                               │
│     │ ● │ │ ● │ │ ● │ │ ● │                               │
│     └───┘ └───┘ └───┘ └───┘                               │
│                                                               │
│  ⚠️  For security, PIN is required                          │
│                                                               │
│  [← Back] [Verify →]                                        │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ STEP 4/4: Confirm Handover (HandoverConfirm.cshtml)         │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  🔄 Handover Flow:                                          │
│                                                               │
│  ┌─────────────────┐        ┌─────────────────┐           │
│  │   👤 Van A      │   →    │   👤 Van B      │           │
│  │   Current       │        │   Next          │           │
│  │   🔴 Closing    │        │   🟢 Opening    │           │
│  └─────────────────┘        └─────────────────┘           │
│                                                               │
│  Handover Details:                                          │
│  Date/Time:          10/12/2025 17:00                       │
│  Closing Balance:    8,450,000 ₫                            │
│  New Opening Bal:    8,450,000 ₫                            │
│                                                               │
│  📝 Notes: [Saved]                                          │
│                                                               │
│  ✅ After confirmation:                                     │
│  - Current shift will be closed                             │
│  - New shift created for Van B (Pending Opening)           │
│  - Van B needs to login and complete opening flow          │
│                                                               │
│  [← Back] [✅ Complete Handover]                            │
└─────────────────────────────────────────────────────────────┘
                           ↓
              🎉 Handover Complete!
        New shift created for Tran Van B
              Return to Dashboard
```

---

## 📊 HISTORY & DETAILS

```
┌─────────────────────────────────────────────────────────────┐
│ SHIFT HISTORY (History.cshtml)                              │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  🔍 Filters:                                                │
│  From: [01/11/2025] To: [10/12/2025] Status: [All]         │
│  [Search] [Reset]                                            │
│                                                               │
│  ┌──────────────────────────────────────────────┐          │
│  │ 📅 10/12/2025            [✅ Closed]         │          │
│  │ Start: 08:00 | End: 17:00                   │          │
│  │ Opening: 5,000,000 ₫ | Closing: 8,450,000 ₫ │          │
│  │                                [View Details]│          │
│  └──────────────────────────────────────────────┘          │
│                                                               │
│  ┌──────────────────────────────────────────────┐          │
│  │ 📅 09/12/2025            [✅ Closed]         │          │
│  │ Start: 08:00 | End: 16:30                   │          │
│  │ Opening: 4,500,000 ₫ | Closing: 7,200,000 ₫ │          │
│  │                                [View Details]│          │
│  └──────────────────────────────────────────────┘          │
│                                                               │
│  [1] [2] [3] ... [10]                                       │
└─────────────────────────────────────────────────────────────┘
                           │
                           ↓ Click "View Details"
┌─────────────────────────────────────────────────────────────┐
│ SHIFT DETAILS (Details.cshtml)                              │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  📄 Shift #12345                                            │
│                                                               │
│  📊 Basic Information:                                      │
│  Staff:          Nguyen Van A                               │
│  Date:           10/12/2025                                 │
│  Start:          08:00:00                                   │
│  End:            17:00:00                                   │
│  Duration:       9h 0m                                      │
│  Status:         ✅ Closed                                  │
│                                                               │
│  💰 Financial Summary:                                      │
│  Opening Bal:    5,000,000 ₫                                │
│  Closing Bal:    8,450,000 ₫                                │
│  Revenue:        3,450,000 ₫                                │
│  Difference:     -50,000 ₫ (Shortage)                       │
│                                                               │
│  ╔════════════════════════════════════╗                     │
│  ║ Total Revenue: 3,450,000 ₫         ║                     │
│  ╚════════════════════════════════════╝                     │
│                                                               │
│  📝 Notes:                                                  │
│  Gave change to customer, short 50k note from personal cash│
│                                                               │
│  🔄 Handover Info:                                          │
│  Handed over to: Tran Van B                                │
│  Notes: Cash verified, 2 pending orders                    │
│                                                               │
│  [← Back to History] [📄 Export PDF Report]                │
└─────────────────────────────────────────────────────────────┘
```

---

## 🎯 Navigation Map

```
                    Counter Staff Dashboard
                             │
        ┌────────────────────┼────────────────────┐
        │                    │                    │
        ▼                    ▼                    ▼
  🟢 OPEN FLOW         🔴 CLOSE FLOW        🟠 HANDOVER
        │                    │                    │
  ┌─────┼─────┐        ┌─────┼─────┐      ┌─────┼─────┐
  ▼     ▼     ▼        ▼     ▼     ▼      ▼     ▼     ▼
 S1    S2    S3       S1    S2    S3     S1    S2    S3
Open  Denom Confirm   View  Count Review  Select Notes PIN
Bal                   Stats Cash  Diff    Staff        
  │     │     │        │     │     │       │     │     │
  └─────┴─────┘        └─────┴─────┘       └─────┴─────┘
        │                    │                    │
        └────────────────────┴────────────────────┘
                             │
                             ▼
                        Dashboard
                             │
                    ┌────────┴────────┐
                    ▼                 ▼
              📊 History         📄 Details
```

---

## 🔄 State Transitions

```
Shift States:
┌──────────┐  Open Flow   ┌──────┐  Close Flow  ┌────────┐
│  None    │ ───────────→ │ Open │ ───────────→ │ Closed │
└──────────┘              └──────┘              └────────┘
                              │                      │
                              │    Handover Flow     │
                              └──────────────────────┘
                                         │
                                         ▼
                          ┌────────────────────────────┐
                          │ Next Staff: Pending Opening│
                          └────────────────────────────┘
```

---

**Legend:**
- 🟢 Green = Opening Flow
- 🔴 Red = Closing Flow
- 🟠 Orange = Handover Flow
- 📊 Blue = Dashboard & Reports
- ✅ Completed/Success
- ❌ Error/Unavailable
- ⚠️ Warning/Important
- 💡 Tip/Suggestion
- 🔐 Security/PIN
- 💰 Financial/Money
- 📝 Notes/Text input
- 👤 User/Staff

---

**Author:** AI Assistant  
**Date:** December 10, 2025

