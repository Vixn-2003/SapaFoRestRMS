namespace BusinessAccessLayer.DTOs.Waiter
{
    /// <summary>
    /// Request để trả món (đã nấu/đã ra)
    /// </summary>
    public class ReturnOrderDetailDto
    {
        public int OrderDetailId { get; set; }
        public int WaiterUserId { get; set; } // ID của waiter yêu cầu trả
        public string Reason { get; set; } = string.Empty; // Lý do: tanh, nguội, sai yêu cầu...
        public string? ImageUrl { get; set; } // URL ảnh chụp món (nếu có)
        public string ResponsibleParty { get; set; } = string.Empty; // Trách nhiệm: Kitchen, Waiter, Customer
    }

    /// <summary>
    /// Response sau khi yêu cầu trả món (cần manager duyệt)
    /// </summary>
    public class ReturnOrderDetailResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ReturnRequestId { get; set; } // ID của request trả món (để manager duyệt)
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    }
}

