using System.ComponentModel.DataAnnotations;

namespace WebSapaForestForStaff.Models
{
    public class BrandBannerViewModel : IValidatableObject
    {
        public int BannerId { get; set; }

        [Required(ErrorMessage = "Tiêu đề là bắt buộc.")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự.")]
        public string Title { get; set; }

        public IFormFile? ImageFile { get; set; }
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc.")]
        [DataType(DataType.Date, ErrorMessage = "Ngày bắt đầu không hợp lệ.")]
        public DateOnly? StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc.")]
        [DataType(DataType.Date, ErrorMessage = "Ngày kết thúc không hợp lệ.")]
        public DateOnly? EndDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
        public string? Status { get; set; }

        public int? CreatedBy { get; set; }
        public string? CreatedByName { get; set; }

       
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (BannerId == 0 && ImageFile == null)
            {
                yield return new ValidationResult("Vui lòng chọn ảnh banner.", new[] { nameof(ImageFile) });
            }

           
            if (StartDate.HasValue && EndDate.HasValue && StartDate > EndDate)
            {
                yield return new ValidationResult("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.",
                    new[] { nameof(EndDate) });
            }
        }
    }
}
