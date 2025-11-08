namespace WebSapaForestForStaff.DTOs
{
    public class ImportSubmitModel
    {
        // Thông tin đơn nhập
        public string ImportCode { get; set; } = null!;
        public DateTime ImportDate { get; set; }

        // Thông tin nhà cung cấp
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = null!;

        // Thông tin người tạo đơn
        public string CreatorName { get; set; } = null!;
        public string CreatorPhone { get; set; } = null!;

        // Thông tin người kiểm hàng
        public string CheckerName { get; set; } = null!;
        public string CheckerPhone { get; set; } = null!;

        // Hình ảnh minh chứng
        public IFormFile? ProofFile { get; set; }

        // Danh sách nguyên liệu (JSON string)
        public string ImportList { get; set; } = null!;
    }
}
