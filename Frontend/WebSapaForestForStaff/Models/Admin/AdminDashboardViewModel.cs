using WebSapaForestForStaff.DTOs.AdminDashboard;

namespace WebSapaForestForStaff.Models.Admin
{
    /// <summary>
    /// ViewModel cho Admin Dashboard View
    /// </summary>
    public class AdminDashboardViewModel
    {
        /// <summary>
        /// Dashboard data từ API
        /// </summary>
        public AdminDashboardDto Dashboard { get; set; } = new();

        /// <summary>
        /// Error message nếu có lỗi
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Success message nếu có
        /// </summary>
        public string? SuccessMessage { get; set; }

        // Legacy properties (giữ lại để tương thích với code cũ nếu có)
        public int TotalUsers { get; set; }
        public int TotalReservationsPendingOrConfirmed { get; set; }
        public int TotalEvents { get; set; }
    }
}
