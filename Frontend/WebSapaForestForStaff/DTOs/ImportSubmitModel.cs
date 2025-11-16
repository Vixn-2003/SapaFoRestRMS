namespace WebSapaForestForStaff.DTOs
{
    public class ImportSubmitModel
    {
        // Thông tin đơn nhập
        public string ImportCode { get; set; } = null!;
        public DateTime ImportDate { get; set; }

        // Thông tin nhà cung cấp
        public int SupplierId { get; set; }

        // Thông tin người tạo đơn
        public int CreatorId { get; set; } = 1;

        // Thông tin người kiểm hàng
        //public int? CheckId { get; set; } = 2;

        // Hình ảnh minh chứng
        public IFormFile? ProofFile { get; set; }

        // Danh sách nguyên liệu (JSON string)
        public string ImportList { get; set; } = null!;
    }
}
