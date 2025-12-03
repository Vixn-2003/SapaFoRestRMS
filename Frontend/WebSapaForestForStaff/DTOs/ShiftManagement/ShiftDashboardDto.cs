using System;
using System.Collections.Generic;

namespace WebSapaForestForStaff.DTOs.ShiftManagement
{
    public class ShiftDashboardDto
    {
        public string Id { get; set; }
        public string Cashier { get; set; }
        public string StartTime { get; set; }
        public string CurrentTime { get; set; }
        public string StartDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal SystemCash { get; set; }
        public decimal SystemCard { get; set; }
        public decimal SystemQR { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal Discount { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal Vat { get; set; }
        public decimal Debt { get; set; }
        public decimal TotalItems { get; set; }
        public string Status { get; set; } // "open", "closed", "handover"
    }

    public class OpenShiftRequestDto
    {
        public decimal OpeningBalance { get; set; }
        public Dictionary<int, int> Denominations { get; set; }
    }

    public class CloseShiftRequestDto
    {
        public decimal ClosingBalance { get; set; }
        public Dictionary<int, int> Denominations { get; set; }
        public decimal Difference { get; set; }
        public string Notes { get; set; }
    }

    public class HandoverShiftRequestDto
    {
        public string NextCashierId { get; set; }
        public string Note { get; set; }
        public string PinCode { get; set; }
    }

    public class ShiftResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public ShiftDashboardDto Data { get; set; }
    }
}

